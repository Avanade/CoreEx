namespace CoreEx.Redis;

/// <summary>
/// Coordinates bounded leases and retained idempotency outcomes directly on the Redis primary.
/// </summary>
/// <param name="connection">The host-owned connection.</param>
/// <param name="settings">The host settings that provide the <see cref="IHostSettings.DomainName"/> used to qualify keys.</param>
/// <param name="logger">The logger.</param>
/// <param name="options">The ownership options.</param>
/// <param name="maxPayloadSize">The maximum serialized idempotency response payload size, or <see langword="null"/> when unlimited.</param>
/// <remarks>Redis failover, eviction or loss of persisted data can erase ownership. This is not resource fencing or exactly-once business execution.</remarks>
public sealed class RedisCoordination(IConnectionMultiplexer connection, IHostSettings settings, ILogger<RedisCoordination> logger, CoordinationOptions options, int? maxPayloadSize = 1024 * 1024) : ISynchronizer, IIdempotencyStore
{
    private const string LeasePurpose = "lease";
    private const string IdempotencyPurpose = "idempotency";

    private static readonly string _begin = LoadScript("IdempotencyBegin.lua");
    private static readonly string _transition = LoadScript("IdempotencyTransition.lua");
    private static readonly string _resolve = LoadScript("IdempotencyResolve.lua");
    private static readonly string _renewLease = LoadScript("LeaseRenew.lua");
    private static readonly string _releaseLease = LoadScript("LeaseRelease.lua");
    private readonly IHostSettings _settings = settings.ThrowIfNull();
    private readonly int? _maxPayloadSize = maxPayloadSize is <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxPayloadSize)) : maxPayloadSize;

    /// <inheritdoc/>
    public int? MaxPayloadSize => _maxPayloadSize;

    /// <summary>
    /// Loads an embedded Lua script from the <c>Scripts</c> folder.
    /// </summary>
    private static string LoadScript(string name)
    {
        using var reader = new StreamReader(Resource.GetStream<RedisCoordination>($"Scripts.{name}"));
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Namespaces the identity qualified by the host domain and ambient tenant.
    /// </summary>
    private RedisKey Key(string purpose, string name, CompositeKey key) => RedisKeyFormatter.Create($"CoreEx:coordination:v1:{purpose}:", _settings.DomainName, RedisKeyFormatter.GetTenantId(), name, key.ToCanonicalString().ThrowIfNullOrWhiteSpace(nameof(key)));

    /// <summary>
    /// Executes one primary-side operation with a bounded acknowledgement wait.
    /// </summary>
    private async Task<RedisResult> ExecuteAsync(string script, RedisKey key, RedisValue[] args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        operation.CancelAfter(options.OperationTimeout);
        return await connection.GetDatabase().ScriptEvaluateAsync(script, [key], args, CommandFlags.DemandMaster).WaitAsync(operation.Token).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ISynchronizationLease?> TryAcquireAsync(string name, CompositeKey key, CancellationToken cancellationToken = default)
    {
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        var physical = Key(LeasePurpose, name, key);
        var owner = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var started = Stopwatch.GetTimestamp();
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        operation.CancelAfter(options.OperationTimeout);

        if (!await connection.GetDatabase().StringSetAsync(physical, owner, options.LeaseDuration, When.NotExists, CommandFlags.DemandMaster).WaitAsync(operation.Token).ConfigureAwait(false))
            return null;

        var lease = new CoordinationLease(options, started,
            async ct => (int)await ExecuteAsync(_renewLease, physical, [owner, (long)options.LeaseDuration.TotalMilliseconds], ct).ConfigureAwait(false) == 1,
            async ct => { await ExecuteAsync(_releaseLease, physical, [owner], ct).ConfigureAwait(false); }, logger);

        if (lease.OwnershipLost.IsCancellationRequested)
        {
            await lease.DisposeAsync().ConfigureAwait(false);
            throw new TimeoutException("The acquired Redis lease has no remaining conservative validity.");
        }

        return lease;
    }

    /// <inheritdoc/>
    public async Task<IdempotencyBeginResult> TryBeginAsync(string name, CompositeKey key, string fingerprint, CancellationToken cancellationToken = default)
    {
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        var physical = Key(IdempotencyPurpose, name, key);
        var owner = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var started = Stopwatch.GetTimestamp();
        var result = (RedisResult[]?)await ExecuteAsync(_begin, physical, [owner, fingerprint.ThrowIfNullOrEmpty(), (long)options.LeaseDuration.TotalMilliseconds, (long)options.Retention.TotalMilliseconds], cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Redis returned no idempotency decision.");
        var outcome = (IdempotencyOutcome)(int)result[0];
        var revision = (string?)result[1];

        if (outcome != IdempotencyOutcome.Acquired)
            return new(outcome, Response: outcome == IdempotencyOutcome.Completed ? new BinaryData((byte[]?)result[2] ?? throw new InvalidOperationException("Missing terminal response.")) : null, Revision: revision);

        var attempt = new IdempotencyAttempt(options, started,
            ct => TransitionAsync(physical, owner, "renew", null, ct),
            async ct => { await TransitionAsync(physical, owner, "retain", null, ct).ConfigureAwait(false); },
            (response, ct) => TransitionAsync(physical, owner, "complete", response, ct),
            ct => TransitionAsync(physical, owner, "release", null, ct), logger);

        if (attempt.OwnershipLost.IsCancellationRequested)
        {
            await attempt.DisposeAsync().ConfigureAwait(false);
            throw new TimeoutException("The acquired idempotency attempt has no remaining conservative validity.");
        }

        return new(outcome, attempt, Revision: owner);
    }

    /// <summary>
    /// Applies a transition only to the same active owner.
    /// </summary>
    private async Task<bool> TransitionAsync(RedisKey key, string owner, string operation, BinaryData? response, CancellationToken cancellationToken)
    {
        ValidateResponseSize(response);
        return (int)await ExecuteAsync(_transition, key, [owner, operation, (long)options.LeaseDuration.TotalMilliseconds, (long)options.Retention.TotalMilliseconds, response is null ? "U" : "C", response?.ToArray() ?? []], cancellationToken).ConfigureAwait(false) == 1;
    }

    /// <inheritdoc/>
    public async Task<bool> TryResolveAsync(string name, CompositeKey key, string revision, BinaryData? response, bool allowRetry = false, CancellationToken cancellationToken = default)
    {
        options.Validate();
        if (!allowRetry)
        {
            IdempotencyAttempt.ThrowIfEmptyResponse(response);
            ValidateResponseSize(response);
        }

        return (int)await ExecuteAsync(_resolve, Key(IdempotencyPurpose, name, key), [revision.ThrowIfNullOrEmpty(), allowRetry ? "retry" : response is null ? "U" : "C", response?.ToArray() ?? [], (long)options.Retention.TotalMilliseconds], cancellationToken).ConfigureAwait(false) == 1;
    }

    /// <summary>
    /// Rejects terminal payloads that exceed the configured store ceiling.
    /// </summary>
    private void ValidateResponseSize(BinaryData? response)
    {
        if (response is not null && _maxPayloadSize is int maxPayloadSize && response.ToMemory().Length > maxPayloadSize)
            throw new ArgumentOutOfRangeException(nameof(response), $"The serialized idempotency response exceeds the configured store limit of {maxPayloadSize} bytes.");
    }
}
