namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Provides the underlying <see cref="CosmosDbUnitOfWork"/> <see cref="IUnitOfWork"/> invoker functionality.
/// </summary>
/// <remarks>Implements transaction handling for a <see cref="CosmosDbUnitOfWork"/>, including nested-transaction flow-through (no Cosmos DB equivalent of a relational save point - see
/// <see cref="CosmosDbUnitOfWork"/>'s own remarks) and outbox/event publishing where supported, by delegating to <see cref="CosmosDbInvoker.OrchestrateUnitOfWorkTransactionAsync{TResult}(InvokerTracer, CosmosDbUnitOfWork, Func{Task{TResult}}, Action{int}?, CancellationToken)"/> -
/// mirrors <c>SqlServerUnitOfWorkInvoker</c>'s shape exactly.
/// <para>Note that the underlying <see cref="CosmosDbUnitOfWork"/> implementation is not thread-safe.</para></remarks>
[InvokerName("CoreEx.Cosmos.CosmosDbUnitOfWork")]
public class CosmosDbUnitOfWorkInvoker : InvokerBase<CosmosDbUnitOfWork, CosmosDbArgs>
{
    private static CosmosDbUnitOfWorkInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="CosmosDbUnitOfWorkInvoker"/> instance.
    /// </summary>
    public static CosmosDbUnitOfWorkInvoker Default => ExecutionContext.GetService<CosmosDbUnitOfWorkInvoker>() ?? (_default ??= new CosmosDbUnitOfWorkInvoker());

    /// <inheritdoc/>
    public override bool IsTracingDisabled => true;

    /// <inheritdoc/>
    protected async override Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, CosmosDbUnitOfWork unitOfWork, CosmosDbArgs args, Func<InvokerTracer, CosmosDbArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        => await CosmosDbInvoker.OrchestrateUnitOfWorkTransactionAsync(tracer, unitOfWork,
            () => base.OnInvokeAsync(tracer, unitOfWork, args, func, cancellationToken),
            outboxEnqueued => CosmosMetrics.OutboxEnqueued.Add(outboxEnqueued),
            cancellationToken).ConfigureAwait(false);
}
