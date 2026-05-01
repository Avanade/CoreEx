namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides the base <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see> <i>relay</i> using the destination <see cref="IEventPublisher"/>.
/// </summary>
/// <typeparam name="TDatabase">The <see cref="IDatabase"/> <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The <see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}"/> instance (self) <see cref="Type"/>.</typeparam>
/// <remarks>The <see cref="SetStatementsByConvention(string?)"/> is used by the constructor to default where possible.</remarks>
public abstract class DatabaseOutboxRelayBase<TDatabase, TSelf> : IDatabaseOutboxRelay where TDatabase : IDatabase where TSelf : DatabaseOutboxRelayBase<TDatabase, TSelf>
{
    private readonly Lazy<DatabaseOutboxRelayInvoker<TDatabase, TSelf>> _invoker = new(() => new DatabaseOutboxRelayInvoker<TDatabase, TSelf>());

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}"/> class.
    /// </summary>
    /// <param name="database">The <see cref="IDatabase"/>.</param>
    /// <param name="eventPublisher">The destination <see cref="IEventPublisher"/>.</param>
    /// <param name="logger">The optional <see cref="ILogger"/>.</param>
    public DatabaseOutboxRelayBase(TDatabase database, IEventPublisher eventPublisher, ILogger<DatabaseOutboxRelayBase<TDatabase, TSelf>>? logger = null)
    {
        Database = database.ThrowIfNull();
        EventPublisher = eventPublisher.ThrowIfNull();
        Logger = logger;

        // Default the statements by convention.
        SetStatementsByConvention();
    }

    /// <summary>
    /// Gets the underlying <typeparamref name="TDatabase"/>.
    /// </summary>
    protected TDatabase Database { get; }

    /// <summary>
    /// Gets the destination <see cref="IEventPublisher"/>.
    /// </summary>
    protected IEventPublisher EventPublisher { get; }

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger? Logger { get; }

    /// <summary>
    /// Gets the <see cref="DatabaseOutboxRelayInvoker{TDatabase, TSelf}"/>.
    /// </summary>
    protected DatabaseOutboxRelayInvoker<TDatabase, TSelf> Invoker => _invoker.Value;

    /// <summary>
    /// Gets or sets the <see cref="SqlStatement"/> used to <i>claim</i> the next batch of events from the <see cref="Database"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="SqlStatement.None"/>.</remarks>
    public virtual SqlStatement ClaimBatchStatement { get; set; } = SqlStatement.None;

    /// <summary>
    /// Gets or sets the <see cref="SqlStatement"/> used to <i>complete</i> the batch of events from the <see cref="Database"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="SqlStatement.None"/>.</remarks>
    public virtual SqlStatement CompleteBatchStatement { get; set; } = SqlStatement.None;

    /// <summary>
    /// Gets or sets the <see cref="SqlStatement"/> used to <i>cancel</i> the batch of events from the <see cref="Database"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="SqlStatement.None"/>.</remarks>
    public virtual SqlStatement CancelBatchStatement { get; set; } = SqlStatement.None;

    /// <summary>
    /// Indicates whether instrumentation is enabled for the polling.
    /// </summary>
    /// <remarks><para>Default is <see langword="false"/>.</para>
    /// The underlying polling is likely to occur frequently and as such causes instrumentation noise; therefore, is disabled by default.</remarks>
    public bool IsInstrumentationEnabledForPolling { get; set; } = false;

    /// <summary>
    /// Indicates whether instrumentation is enabled for the publishing.
    /// </summary>
    /// <remarks><para>Default is <see langword="true"/>.</para>
    /// The underlying publishing is considered more interesting; therefore, is enabled by default. However, may create noise and as such is configurable.</remarks>
    public bool IsInstrumentationEnabledForPublishing { get; set; } = true;

    /// <summary>
    /// Sets the <see cref="ClaimBatchStatement"/>, <see cref="CompleteBatchStatement"/>, and <see cref="CancelBatchStatement"/> by convention using the optional <paramref name="schema"/>.
    /// </summary>
    /// <param name="schema">The optional database schema name.</param>
    /// <remarks>Where the schema is not specified, and the database supports schema, then the <see cref="IHostSettings.DomainName"/> will be used by default.</remarks>
    public abstract void SetStatementsByConvention(string? schema = null);

    /// <inheritdoc/>
    public async Task<bool> RelayAsync(DatabaseOutboxRelayArgs args, CancellationToken cancellationToken = default)
    {
        // Guard against being in a transaction already as this is not allowed!
        if (Database.CurrentTransaction is not null)
            throw new InvalidOperationException($"The {typeof(TSelf).Name} cannot be executed within an existing database transaction.");

        bool relayed = false;

        // Iterate through the allocated partitions and relay accordingly.
        var partitions = args.PartitionPicker.GetNextPartitions(DateTimeOffset.UtcNow);
        foreach (var partitionId in partitions)
        {
            // To minimize the risk of the relay being inadvertently disrupted during execution the cancellation token is not passed to the RelayAsync method;
            // the cancellation token is only used here to determine whether to continue with the next partition or not.
            if (cancellationToken.IsCancellationRequested)
                break;

            // Perform the relay for the partition using a new timer-based cancellation token that is based on the lease duration to ensure it completes within the lease window to minimize the risk of the batch
            // being cancelled due to exceeding the lease duration before the relay operation has had a chance to complete.
            var leaseCancellationTokenSource = new CancellationTokenSource(args.LeaseDuration);
            try
            {
                var relay = await RelayAsync(args, partitionId, leaseCancellationTokenSource.Token).ConfigureAwait(false);
                if (relay)
                    relayed = true;
            }
            catch (Exception ex) when (ex.IsCanceled())
            {
                if (Logger?.IsEnabled(LogLevel.Warning) is true)
                    Logger.LogWarning("The relay operation for partition '{PartitionId}' was cancelled due to exceeding lease-duration timeout; cancellation exception will continue to throw.", partitionId);

                // Keep throwing as the cancellation is likely to be due to exceeding the lease duration which is a serious failure that should be surfaced and not treated as a transient exception.
                throw;
            }
        }

        return relayed;
    }

    /// <summary>
    /// Performs the relay for the specified <paramref name="partitionId"/>.
    /// </summary>
    private async Task<bool> RelayAsync(DatabaseOutboxRelayArgs args, int partitionId, CancellationToken cancellationToken)
    {
        // New lease identifier per relay invocation.
        var leaseId = Guid.NewGuid();

        // Grab the next batch of events.
        using (SuppressInstrumentationScope.Begin(!IsInstrumentationEnabledForPolling))
        {
            // Claim a batch.
            var events = await ClaimNextBatchAsync(args, leaseId, partitionId, cancellationToken).ConfigureAwait(false);
            if (events.Count == 0)
            {
                if (Logger?.IsEnabled(LogLevel.Debug) is true)
                    Logger.LogDebug("No events were found to relay from the Outbox.");

                return false;
            }

            // Need a try/catch so we can rollback the claim where necessary.
            try
            {
                // Add the events.
                EventPublisher.Add(events);

                // Publish the events; i.e. the actual "relay".
                using (SuppressInstrumentationScope.Begin(!IsInstrumentationEnabledForPublishing))
                {
                    await Invoker.InvokeAsync(this, async (tracer, cancellationToken) =>
                    { 
                        if (tracer.Activity is not null)
                        {
                            tracer.Activity.AddTag("outbox.partition", partitionId);
                            tracer.Activity.AddTag("outbox.events.count", events.Count);

                            foreach (var e in events)
                            {
                                if (!e.Event.TryGetExtensionAttribute<string>("traceparent", out var traceParent) || string.IsNullOrEmpty(traceParent))
                                    continue;

                                e.Event.TryGetExtensionAttribute<string>("tracestate", out var traceState);
                                if (ActivityContext.TryParse(traceParent, traceState, out var ac))
                                    tracer.Activity.AddLink(new ActivityLink(ac));

                                if (e.Event.TryGetExtensionAttribute<string>("baggage", out var baggageHeader) && !string.IsNullOrEmpty(baggageHeader))
                                {
                                    // Parse W3C Baggage format: "key1=value1,key2=value2;property1;property2"
                                    // Note: OpenTelemetry doesn't expose a public baggage parser, so we implement per W3C spec.
                                    foreach (var member in baggageHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                    {
                                        // Take only the key-value part (before any optional properties after semicolon).
                                        var keyValue = member.Split(';', 2)[0].Trim();
                                        var parts = keyValue.Split('=', 2);
                                        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                                        {
                                            // Decode URL-encoded values per W3C Baggage spec.
                                            var key = Uri.UnescapeDataString(parts[0].Trim());
                                            var value = Uri.UnescapeDataString(parts[1].Trim());
                                            tracer.Activity.AddBaggage(key, value);
                                        }
                                    }
                                }
                            }
                        }

                        await EventPublisher.PublishAsync(cancellationToken).ConfigureAwait(false);
                    }, cancellationToken).ConfigureAwait(false);
                }

                // Complete the batch.
                await CompleteBatchAsync(args, leaseId, cancellationToken).ConfigureAwait(false);

                // Report success to encourage consideration in the next round pick where a full batch was claimed.
                if (events.Count == args.BatchSize)
                    args.PartitionPicker.PrioritizePartition(partitionId);

                return true;
            }
            catch (Exception ex)
            {
                // Cancel the batch.
                await CancelBatchAsync(args, leaseId, cancellationToken).ConfigureAwait(false);

                if (Logger?.IsEnabled(LogLevel.Debug) is true)
                    Logger.LogDebug("Outbox batch was cancelled due to error: {Error}", ex.Message);

                // Keep bubbling the exception.
                throw;
            }
            finally
            {
                // Reset the event publisher events to ensure no events accidently bleed out of this operation.
                EventPublisher.Reset();
            }
        }
    }

    /// <summary>
    /// Claims the next batch of events for the specified <paramref name="partitionId"/>.
    /// </summary>
    /// <param name="args">The <see cref="DatabaseOutboxRelayArgs"/>.</param>
    /// <param name="leaseId">The lease <see cref="Guid"/>.</param>
    /// <param name="partitionId">The assigned partition identifier/number.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Zero or more <see cref="DestinationEvent"/> entries within a list.</returns>
    protected virtual async Task<List<DestinationEvent>> ClaimNextBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, int partitionId, CancellationToken cancellationToken)
    {
        var ec = ExecutionContext.HasCurrent ? ExecutionContext.Current : null;
        var events = new List<DestinationEvent>();

        // Add 10% more to ensure the lease duration is slightly longer than the cancellation token used for the relay operation to minimize the risk of the batch being cancelled due to exceeding the lease duration
        // before the relay operation has had a chance to complete.
        var leaseDurationSeconds = ConvertDurationToSeconds(args.LeaseDuration);
        leaseDurationSeconds += Math.Min(1, (int)Math.Round(leaseDurationSeconds * 0.1, MidpointRounding.AwayFromZero));

        try
        {
            await Database.Statement(ClaimBatchStatement)
                .ParamWhen(!string.IsNullOrEmpty(ec?.TenantId), Database.NamedColumns.TenantIdName, () => ec!.TenantId)
                .Param(Database.NamedColumns.PartitionIdName, partitionId)
                .Param(Database.NamedColumns.OutboxBatchSizeName, args.BatchSize)
                .Param(Database.NamedColumns.OutboxLeaseIdName, leaseId)
                .Param(Database.NamedColumns.OutboxLeaseDurationName, leaseDurationSeconds)
                .ReturnValue(out var returnValueParameter)
                .SelectQueryAsync(events, (dr, _) =>
                {
                    return new DestinationEvent
                    (
                        dr.GetValue<string>(Database.NamedColumns.OutboxDestinationName),
                        dr.GetValue<JsonElement>(Database.NamedColumns.OutboxEventName).DecodeToCloudEvent(Database.JsonSerializerOptions)
                    );
                }, cancellationToken);

            if (Logger?.IsEnabled(LogLevel.Debug) is true)
                Logger.LogDebug("The return value from the Outbox claim batch database statement is {ReturnValue}.", returnValueParameter.Value);
        }
        catch (Exception ex)
        {
            if (!IsTransientException(ex))
                throw;
        }

        return events;
    }

    /// <summary>
    /// Completes the batch.
    /// </summary>
    /// <param name="args">The <see cref="DatabaseOutboxRelayArgs"/>.</param>
    /// <param name="leaseId">The lease <see cref="Guid"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected virtual Task CompleteBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, CancellationToken cancellationToken)
        => Database.Statement(CompleteBatchStatement)
            .Param(Database.NamedColumns.OutboxLeaseIdName, leaseId)
            .Param(Database.NamedColumns.OutboxDequeuedUtcName, DateTime.UtcNow)
            .NonQueryAsync(cancellationToken);

    /// <summary>
    /// Cancels the batch.
    /// </summary>
    /// <param name="args">The <see cref="DatabaseOutboxRelayArgs"/>.</param>
    /// <param name="leaseId">The lease <see cref="Guid"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected virtual Task CancelBatchAsync(DatabaseOutboxRelayArgs args, Guid leaseId, CancellationToken cancellationToken)
        => Database.Statement(CancelBatchStatement)
            .Param(Database.NamedColumns.OutboxLeaseIdName, leaseId)
            .Param(Database.NamedColumns.OutboxBackoffDurationName, ConvertDurationToSeconds(args.BackOffDuration))
            .NonQueryAsync(cancellationToken);

    /// <summary>
    /// Indicates whether the specified <paramref name="exception"/> is an expected exception that should not be logged as an error.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/>.</param>
    /// <returns><see langword="true"/> where the exception is considered transient; otherwise, <see langword="false"/>.</returns>
    /// <remarks>For example, a timeout or deadlock exception that may occur during the claim of the batch and is expected to be transient in nature.</remarks>
    protected virtual bool IsTransientException(Exception exception) => false;

    /// <summary>
    /// Converts a duration time-span into a rounded number of seconds where the minimum allowed is one second.
    /// </summary>
    private static int ConvertDurationToSeconds(TimeSpan duration) => Math.Min((int)Math.Round(duration.TotalSeconds, MidpointRounding.AwayFromZero), 1);
}