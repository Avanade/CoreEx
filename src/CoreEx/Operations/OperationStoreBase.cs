namespace CoreEx.Operations;

/// <summary>
/// Provides the shared argument validation for an <see cref="IOperationStore"/> implementation.
/// </summary>
/// <remarks>Public members validate their arguments and then invoke the corresponding protected <c>On…Async</c> member, so every provider enforces identical limits.</remarks>
/// <param name="logger">The <see cref="ILogger"/>; required so that every operation expired by <see cref="CleanupAsync"/> is reported.</param>
/// <param name="settings">The <see cref="IHostSettings"/> providing the domain name used to qualify the operation identity.</param>
/// <param name="options">The optional <see cref="OperationStoreOptions"/>.</param>
public abstract class OperationStoreBase(ILogger logger, IHostSettings settings, OperationStoreOptions? options = null) : IOperationStore
{
    /// <summary>
    /// Gets the <see cref="OperationStoreOptions"/>.
    /// </summary>
    protected OperationStoreOptions Options { get; } = options ?? new OperationStoreOptions();

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; } = logger.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="IHostSettings"/>.
    /// </summary>
    protected IHostSettings Settings { get; } = settings.ThrowIfNull();

    /// <inheritdoc/>
    public Task<OperationState?> GetByIdAsync(Guid operationId, CancellationToken cancellationToken = default) => OnGetByIdAsync(RequiredId(operationId), cancellationToken);

    /// <inheritdoc/>
    public Task<OperationState?> GetByKeyAsync(string operationName, CompositeKey key, CancellationToken cancellationToken = default)
        => OnGetByKeyAsync(DomainName, TenantId, operationName.ThrowIfNullOrWhiteSpace(), key.ToCanonicalString().ThrowIfNullOrWhiteSpace(nameof(key)), cancellationToken);

    /// <inheritdoc/>
    public Task<OperationCreateResult> CreateAsync(string operationName, CompositeKey key, string? initiatorId = null, string? data = null, CancellationToken cancellationToken = default)
        => OnCreateAsync(DomainName, TenantId, operationName.ThrowIfNullOrWhiteSpace(), key.ToCanonicalString().ThrowIfNullOrWhiteSpace(nameof(key)), Bounded(initiatorId, OperationLimits.MaxInitiatorIdLength), data, cancellationToken);

    /// <inheritdoc/>
    public Task<OperationClaimResult> ClaimAsync(Guid operationId, string owner, TimeSpan duration, CancellationToken cancellationToken = default)
        => OnClaimAsync(RequiredId(operationId), Bounded(owner.ThrowIfNullOrWhiteSpace(), OperationLimits.MaxClaimOwnerLength, nameof(owner))!, Positive(duration), cancellationToken);

    /// <inheritdoc/>
    public Task<OperationClaim?> RenewAsync(OperationClaim claim, TimeSpan duration, string? progress = null, CancellationToken cancellationToken = default)
        => OnRenewAsync(claim.ThrowIfNull(), Positive(duration), Bounded(progress, OperationLimits.MaxProgressLength), cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ReleaseAsync(OperationClaim claim, string? errorCode = null, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        if (errorMessage is not null && errorCode is null)
            throw new ArgumentException("An error message requires an error code.", nameof(errorMessage));

        if (errorCode is not null && string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentException("The error code must not be empty or whitespace.", nameof(errorCode));

        return OnReleaseAsync(claim.ThrowIfNull(), Bounded(errorCode, OperationLimits.MaxErrorCodeLength), Bounded(errorMessage, OperationLimits.MaxErrorMessageLength), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<OperationState?> CompleteAsync(OperationClaim claim, OperationCompletion completion, CancellationToken cancellationToken = default) => OnCompleteAsync(claim.ThrowIfNull(), completion.ThrowIfNull(), cancellationToken);

    /// <inheritdoc/>
    public Task<OperationState?> RequestCancelAsync(Guid operationId, CancellationToken cancellationToken = default) => OnRequestCancelAsync(RequiredId(operationId), cancellationToken);

    /// <inheritdoc/>
    public Task<OperationCleanupResult> CleanupAsync(int maxCount, CancellationToken cancellationToken = default) => OnCleanupAsync(maxCount.ThrowIfLessThanOrEqualToZero("The count must be greater than zero."), cancellationToken);

    /// <summary>
    /// Gets the operation by its validated <paramref name="operationId"/>.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationState"/> where found; otherwise, <see langword="null"/>.</returns>
    protected abstract Task<OperationState?> OnGetByIdAsync(Guid operationId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the operation by its validated complete identity.
    /// </summary>
    /// <param name="domainName">The domain name.</param>
    /// <param name="tenantId">The tenant identifier; <see langword="null"/> where not tenant-scoped.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="instanceId">The canonical instance identifier; compared ordinally, including case and trailing spaces.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationState"/> where found; otherwise, <see langword="null"/>.</returns>
    protected abstract Task<OperationState?> OnGetByKeyAsync(string domainName, string? tenantId, string operationName, string instanceId, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the validated <see cref="CreateAsync"/>.
    /// </summary>
    /// <param name="domainName">The domain name.</param>
    /// <param name="tenantId">The tenant identifier; <see langword="null"/> where not tenant-scoped.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="instanceId">The canonical instance identifier; compared ordinally, including case and trailing spaces.</param>
    /// <param name="initiatorId">The initiator identifier.</param>
    /// <param name="data">The opaque JSON extension data.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationCreateResult"/>.</returns>
    protected abstract Task<OperationCreateResult> OnCreateAsync(string domainName, string? tenantId, string operationName, string instanceId, string? initiatorId, string? data, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the validated <see cref="ClaimAsync"/>.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="owner">The owner token.</param>
    /// <param name="duration">The claim duration.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationClaimResult"/>.</returns>
    protected abstract Task<OperationClaimResult> OnClaimAsync(Guid operationId, string owner, TimeSpan duration, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the validated <see cref="RenewAsync"/>.
    /// </summary>
    /// <param name="claim">The held <see cref="OperationClaim"/>.</param>
    /// <param name="duration">The claim duration.</param>
    /// <param name="progress">The optional progress text.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The renewed <see cref="OperationClaim"/>; otherwise, <see langword="null"/>.</returns>
    protected abstract Task<OperationClaim?> OnRenewAsync(OperationClaim claim, TimeSpan duration, string? progress, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the validated <see cref="ReleaseAsync"/>.
    /// </summary>
    /// <param name="claim">The held <see cref="OperationClaim"/>.</param>
    /// <param name="errorCode">The optional retryable error code.</param>
    /// <param name="errorMessage">The optional retryable error message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where released; otherwise, <see langword="false"/>.</returns>
    protected abstract Task<bool> OnReleaseAsync(OperationClaim claim, string? errorCode, string? errorMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the validated <see cref="CompleteAsync"/>.
    /// </summary>
    /// <param name="claim">The held <see cref="OperationClaim"/>.</param>
    /// <param name="completion">The <see cref="OperationCompletion"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The finished <see cref="OperationState"/>; otherwise, <see langword="null"/>.</returns>
    protected abstract Task<OperationState?> OnCompleteAsync(OperationClaim claim, OperationCompletion completion, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the validated <see cref="RequestCancelAsync"/>.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="OperationState"/>; otherwise, <see langword="null"/>.</returns>
    protected abstract Task<OperationState?> OnRequestCancelAsync(Guid operationId, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the validated <see cref="CleanupAsync"/>.
    /// </summary>
    /// <param name="maxCount">The maximum number of operations to expire, and separately the maximum number to purge.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationCleanupResult"/>.</returns>
    /// <remarks>Implementations must invoke <see cref="LogExpired"/> for each operation they expire.</remarks>
    protected abstract Task<OperationCleanupResult> OnCleanupAsync(int maxCount, CancellationToken cancellationToken);

    /// <summary>
    /// Logs a warning that an abandoned operation was expired.
    /// </summary>
    /// <param name="state">The expired <see cref="OperationState"/>.</param>
    /// <param name="previousStatus">The <see cref="OperationStatus"/> before expiry.</param>
    /// <remarks>The operation key and initiator are deliberately not logged as they may contain tenant or caller-supplied values.</remarks>
    protected void LogExpired(OperationState state, OperationStatus previousStatus)
    {
        state.ThrowIfNull();
        Logger.LogWarning("Operation {OperationId} ({OperationName}) expired after inactivity; previous status {PreviousStatus}, attempt {Attempt}, last error {ErrorCode}.",
            state.OperationId, state.OperationName, previousStatus, state.Attempt, state.ErrorCode);
    }

    /// <summary>
    /// Gets the validated host domain name.
    /// </summary>
    private string DomainName => Settings.DomainName.ThrowIfNullOrWhiteSpace(nameof(IHostSettings.DomainName));

    /// <summary>
    /// Gets the ambient tenant identifier, treating an empty value as absent.
    /// </summary>
    private static string? TenantId => ExecutionContext.TryGetCurrent(out var ec) && !string.IsNullOrEmpty(ec.TenantId) ? ec.TenantId : null;

    /// <summary>
    /// Validates that the identifier is not empty.
    /// </summary>
    private static Guid RequiredId(Guid operationId, [CallerArgumentExpression(nameof(operationId))] string? paramName = null)
        => operationId != Guid.Empty ? operationId : throw new ArgumentException("The operation identifier must not be empty.", paramName);

    /// <summary>
    /// Validates that the duration is greater than zero.
    /// </summary>
    private static TimeSpan Positive(TimeSpan duration, [CallerArgumentExpression(nameof(duration))] string? paramName = null)
        => duration > TimeSpan.Zero ? duration : throw new ArgumentOutOfRangeException(paramName, "The duration must be greater than zero.");

    /// <summary>
    /// Validates the maximum length of an optional value.
    /// </summary>
    private static string? Bounded(string? value, int maxLength, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value is not null && value.Length > maxLength ? throw new ArgumentException($"The value must not exceed {maxLength} characters.", paramName) : value;
}
