namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides the <see cref="IDatabaseOutboxRelay.RelayAsync(CoreEx.Database.Outbox.DatabaseOutboxRelayArgs, CancellationToken)"/> execution leveraging a <see cref="TimerHostedServiceBase"/>.
/// </summary>
/// <typeparam name="TOutboxRelay">The <see cref="IDatabaseOutboxRelay"/> <see cref="Type"/>.</typeparam>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public abstract class DatabaseOutboxRelayHostedServiceBase<TOutboxRelay>(IServiceProvider serviceProvider, ILogger logger) : DatabaseOutboxRelayHostedServiceBase(serviceProvider, logger) where TOutboxRelay : IDatabaseOutboxRelay
{
    /// <summary>
    /// Gets or sets the factory method to create the <typeparamref name="TOutboxRelay"/>.
    /// </summary>
    public Func<IServiceProvider, TOutboxRelay>? RelayFactory { get => field; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <inheritdoc/>
    protected override async Task<bool> OnExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken)
    {
        // Instantiate the relay via the factory where specified.
        var relay = RelayFactory is null
            ? ExecutionContext.GetRequiredService<TOutboxRelay>()
            : RelayFactory(executionContext.ServiceProvider.ThrowIfNull()) ?? throw new InvalidOperationException($"The {typeof(TOutboxRelay).Name} was not be created using the specified {nameof(RelayFactory)}.");

        // Create the arguments.
        var args = new DatabaseOutboxRelayArgs
        {
            PartitionPicker = PartitionPicker,
            BatchSize = BatchSize,
            LeaseDuration = LeaseDuration,
            BackOffDuration = BackOffDuration
        };

        // Execute the relay.
        var relayed = await relay.RelayAsync(args, cancellationToken).ConfigureAwait(false);

        // Immediately re-execute where work was done (doesn't matter how much); otherwise, sleep.
        return relayed;   
    }
}