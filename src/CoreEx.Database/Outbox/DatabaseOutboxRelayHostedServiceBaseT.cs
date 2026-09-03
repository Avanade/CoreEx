namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides the <see cref="IDatabaseOutboxRelay.RelayAsync(CoreEx.Database.Outbox.DatabaseOutboxRelayArgs, CancellationToken)"/> execution leveraging a <see cref="TimerHostedServiceBase"/>.
/// </summary>
/// <typeparam name="TOutboxRelay">The <see cref="IDatabaseOutboxRelay"/> <see cref="Type"/>.</typeparam>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public abstract class DatabaseOutboxRelayHostedServiceBase<TOutboxRelay>(IServiceProvider serviceProvider, ILogger logger) : DatabaseOutboxRelayHostedServiceBase(serviceProvider, logger) where TOutboxRelay : IDatabaseOutboxRelay
{
    private DatabaseOutboxRelayArgs? _args;

    /// <summary>
    /// Gets or sets the factory method to create the <typeparamref name="TOutboxRelay"/>.
    /// </summary>
    public Func<IServiceProvider, TOutboxRelay>? RelayFactory { get; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <inheritdoc/>
    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializeAsync(cancellationToken).ConfigureAwait(false);

        // Built once and reused for every tick, rather than per tick - every field here is immutable once initialization completes (PartitionPicker is itself explicitly designed to be reused
        // for the worker's whole lifetime; see its own remarks).
        _args = new DatabaseOutboxRelayArgs
        {
            PartitionPicker = PartitionPicker,
            BatchSize = BatchSize,
            LeaseDuration = LeaseDuration,
            BackOffDuration = BackOffDuration,
            // Resiliency may be explicitly set to null by a consumer that wants to fully opt out (falling back to TimerHostedServiceBase.PauseOnUnhandledException instead) - degrade to an
            // unprotected pass-through in that case, rather than failing, while still always supplying a ResiliencyExecutor.
            ResiliencyExecutor = Resiliency is null
                ? (work, ct) => work(ct)
                : async (work, ct) =>
                {
                    var context = ResilienceContextPool.Shared.Get(ct);
                    try
                    {
                        // Keyed by TimerHostedServiceBase (not DatabaseOutboxRelayHostedServiceBase) to match the TOwner CreateDefaultResiliency builds the pipeline with - the same convention
                        // TimerHostedServiceBase's own generic per-tick wrap uses; a custom Resiliency pipeline must be built with the same TOwner to resolve correctly here.
                        context.Properties.Set(ResilienceOwner<TimerHostedServiceBase>.PropertyKey, this);
                        return await Resiliency.ExecuteAsync(static async (rc, w) => await w(rc.CancellationToken).ConfigureAwait(false), context, work).ConfigureAwait(false);
                    }
                    finally
                    {
                        ResilienceContextPool.Shared.Return(context);
                    }
                }
        };
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken)
    {
        // Instantiate the relay via the factory where specified.
        var relay = RelayFactory is null
            ? ExecutionContext.GetRequiredService<TOutboxRelay>()
            : RelayFactory(executionContext.ServiceProvider.ThrowIfNull()) ?? throw new InvalidOperationException($"The {typeof(TOutboxRelay).Name} was not be created using the specified {nameof(RelayFactory)}.");

        var args = _args ?? throw new InvalidOperationException($"{nameof(_args)} has not yet been initialized; this should not be accessed before {nameof(OnInitializeAsync)}.");

        // Execute the relay.
        var relayed = await relay.RelayAsync(args, cancellationToken).ConfigureAwait(false);

        // Immediately re-execute where work was done (doesn't matter how much); otherwise, sleep.
        return relayed;
    }
}