namespace CoreEx.Database.Postgres.Extended;

/// <summary>
/// Provides the underlying <see cref="PostgresDatabase"/> <see cref="IUnitOfWork"/> invoker functionality.
/// </summary>
/// <remarks>Implements transaction handling including automatic save-point support for nested unit-of-work invocations. Also, where the underlying work returns an <see cref="IResult"/>, 
/// then an <see cref="IResult.IsFailure"/> will trigger a rollback similar to an unhandled exception.
/// <para>Where a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see> is supported (<see cref="IUnitOfWork.AreEventsSupported"/>) then the <see cref="IUnitOfWork.Events"/> will
/// automatically be included within the root (top-most) transaction. This is achieved by executing the <see cref="IEventPublisher.PublishAsync(CancellationToken)"/>. Nested (child) transactional rollbacks are also supported by the <see cref="IEventPublisher.Rollback(int)"/>.</para>
/// <para>Note that the underlying <see cref="IUnitOfWork"/> implementation is not thread-safe.</para></remarks>
[InvokerName("CoreEx.Database.Postgres.PostgresUnitOfWork")]
public class PostgresUnitOfWorkInvoker : InvokerBase<PostgresUnitOfWork, PostgresDatabaseArgs>
{
    private static PostgresUnitOfWorkInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="PostgresUnitOfWorkInvoker"/> instance.
    /// </summary>
    public static PostgresUnitOfWorkInvoker Default => ExecutionContext.GetService<PostgresUnitOfWorkInvoker>() ?? (_default ??= new PostgresUnitOfWorkInvoker());

    /// <inheritdoc/>
    protected async override Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, PostgresUnitOfWork unitOfWork, PostgresDatabaseArgs args, Func<InvokerTracer, PostgresDatabaseArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        => await DatabaseInvoker.OrchestrateUnitOfWorkTransactionAsync(tracer, unitOfWork,
            () => base.OnInvokeAsync(tracer, unitOfWork, args, func, cancellationToken),
            outboxEnqueued => PostgresMetrics.OutboxEnqueued.Add(outboxEnqueued),
            cancellationToken).ConfigureAwait(false);
}