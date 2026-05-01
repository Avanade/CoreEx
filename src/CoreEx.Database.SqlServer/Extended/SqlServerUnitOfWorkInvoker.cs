namespace CoreEx.Database.SqlServer.Extended;

/// <summary>
/// Provides the underlying <see cref="SqlServerDatabase"/> <see cref="IUnitOfWork"/> invoker functionality.
/// </summary>
/// <remarks>Implements transaction handling including automatic save-point support for nested unit-of-work invocations. Also, where the underlying work returns an <see cref="IResult"/>, 
/// then an <see cref="IResult.IsFailure"/> will trigger a rollback similar to an unhandled exception.
/// <para>Where a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see> is supported (<see cref="IUnitOfWork.AreEventsSupported"/>) then the <see cref="IUnitOfWork.Events"/> will
/// automatically be included within the root (top-most) transaction. This is achieved by executing the <see cref="IEventPublisher.PublishAsync(CancellationToken)"/>. Nested (child) transactional rollbacks are also supported by the <see cref="IEventPublisher.Rollback(int)"/>.</para>
/// <para>Note that the underlying <see cref="IUnitOfWork"/> implementation is not thread-safe.</para></remarks>
[InvokerName("CoreEx.Database.SqlServer.SqlServerUnitOfWork")]
public class SqlServerUnitOfWorkInvoker : InvokerBase<SqlServerUnitOfWork, SqlServerDatabaseArgs>
{
    private static SqlServerUnitOfWorkInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="SqlServerUnitOfWorkInvoker"/> instance.
    /// </summary>
    public static SqlServerUnitOfWorkInvoker Default => ExecutionContext.GetService<SqlServerUnitOfWorkInvoker>() ?? (_default ??= new SqlServerUnitOfWorkInvoker());

    /// <inheritdoc/>
    protected async override Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, SqlServerUnitOfWork unitOfWork, SqlServerDatabaseArgs args, Func<InvokerTracer, SqlServerDatabaseArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        var txn = unitOfWork.Database.CurrentTransaction;
        var isRootTxn = txn is null;
        var savePoint = isRootTxn ? string.Empty : unitOfWork.Database.GetNextSavePointName();
        var eventStartCount = unitOfWork.Outbox?.Count ?? 0;

        tracer.Activity?.AddTag("database.id", unitOfWork.Database.DatabaseId);

        // Reusable rollback logic.
        async Task RollbackAsync(Exception exception)
        {
            if (txn is not null)
            {
                if (isRootTxn)
                {
                    await txn.RollbackAsync(cancellationToken).ConfigureAwait(false);

                    if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Information))
                        tracer.Logger.LogInformation("Unit-of-work transaction rolled back due to error: {Error} [DatabaseId: {DatabaseId}]", exception.Message, unitOfWork.Database.DatabaseId);
                }
                else
                {
                    await txn.RollbackAsync(savePoint, cancellationToken).ConfigureAwait(false);

                    if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Information))
                        tracer.Logger.LogInformation("Unit-of-work transaction save-point '{SavePoint}' rolled back due to error: {Error} [DatabaseId: {DatabaseId}]", savePoint, exception.Message, unitOfWork.Database.DatabaseId);
                }
            }

            // Where outbox/events are supported then also rollback any added events.
            unitOfWork.Outbox?.Rollback(Math.Max(0, unitOfWork.Outbox.Count - eventStartCount));
        }

        // Perform the unit-of-work within a transaction or save-point as appropriate.
        try
        {
            // Where root, begin new transaction; otherwise, create save-point.
            if (isRootTxn)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug("Unit-of-work transaction; creating (root) transaction. [DatabaseId: {DatabaseId}]", unitOfWork.Database.DatabaseId);

                var conn = await unitOfWork.Database.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
                txn = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                unitOfWork.Database.UseTransaction(txn);
            }
            else
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug("Unit-of-work transaction; creating save-point '{SavePoint}'. [DatabaseId: {DatabaseId}]", savePoint, unitOfWork.Database.DatabaseId);

                await txn!.SaveAsync(savePoint, cancellationToken).ConfigureAwait(false);
            }

            // Invoke the "work".
            var result = await base.OnInvokeAsync(tracer, unitOfWork, args, func, cancellationToken).ConfigureAwait(false);

            // Where a failure, rollback transaction or save-point as appropriate, and return.
            if (result is IResult ir && ir.IsFailure)
            {
                // Rollback transaction or save-point as appropriate; then return the failure result.
                await RollbackAsync(ir.Error!).ConfigureAwait(false);
                return result;
            }

            // Commit transaction or complete save-point as appropriate.
            if (isRootTxn)
            {
                // Where outbox/events are supported then publish.
                var outboxEnqueued = 0;
                if (unitOfWork.AreEventsSupported && !unitOfWork.Events.IsEmpty)
                {
                    await unitOfWork.Outbox!.PublishAsync(cancellationToken).ConfigureAwait(false);
                    outboxEnqueued = unitOfWork.Outbox.Count;
                }

                // Commit the work and outbox.
                await txn!.CommitAsync(cancellationToken).ConfigureAwait(false);

                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug("Unit-of-work transaction committed successfully. [DatabaseId: {DatabaseId}]", unitOfWork.Database.DatabaseId);

                // Record metrics for enqueued outbox messages.
                if (outboxEnqueued > 0)
                    SqlServerMetrics.OutboxEnqueued.Add(outboxEnqueued);
            }
            else if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                tracer.Logger.LogDebug("Unit-of-work transaction save-point '{SavePoint}' completed successfully. [DatabaseId: {DatabaseId}]", savePoint, unitOfWork.Database.DatabaseId);

            // Sweet, we made it. Happy times!
            return result;
        }
        catch (Exception ex)
        {
            // Rollback transaction or save-point as appropriate.
            await RollbackAsync(ex).ConfigureAwait(false);

            // Where extended exception, and the result is an IResult then convert to a failure result.
            if (ExtendedException.TryConvertExceptionToResult<TResult>(ex, out var result))
                return result;

            // Keep on bubbling.
            throw;
        }
        finally
        {
            // Dispose and reset transaction where root.
            if (isRootTxn)
            {
                if (txn is not null)
                    await txn.DisposeAsync().ConfigureAwait(false);

                unitOfWork.Database.UseTransaction(null);
            }
        }
    }
}