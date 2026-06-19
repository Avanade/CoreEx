namespace CoreEx.Database.Abstractions;

/// <summary>
/// Provides the standard <see cref="IDatabase"/> invoker functionality.
/// </summary>
/// <remarks>Catches any unhandled <see cref="DbException"/> and invokes <see cref="IDatabase.HandleDbException(DbException)"/> to handle (where <see cref="DatabaseArgsBase.TransformException"/>) is <see langword="true"/>
/// before bubbling up.</remarks>
public abstract class DatabaseInvoker : InvokerBase<IDatabase, DatabaseArgs>
{
    /// <inheritdoc/>
    protected override async Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, IDatabase database, DatabaseArgs dbArgs, Func<InvokerTracer, DatabaseArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        try
        {
            return await base.OnInvokeAsync(tracer, database, dbArgs, func, cancellationToken).ConfigureAwait(false);
        }
        catch (DbException dbex) when (dbArgs.TransformException)
        {
            var hex = database.HandleDbException(dbex);
            if (hex is not null)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug(dbex, "Database exception converted to '{ExceptionType}': {Message} [DatabaseId: {DatabaseId}]", hex.GetType().Name, hex.Message, database.DatabaseId);

                // Where the result is an IResult (ROP) and the exception is considered an error then return as an IResult _failure_.
                if (ExtendedException.TryConvertExceptionToResult<TResult>(hex, out var res))
                    return res;

                throw hex;
            }

            throw;
        }
    }

    /// <summary>
    /// Provides standardized database transaction handling for a unit-of-work, including support for nested transactions via save-points, and outbox/event publishing where supported.
    /// </summary>
    /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
    /// <param name="tracer">The <see cref="InvokerTracer"/>.</param>
    /// <param name="unitOfWork">The <see cref="IDatabaseUnitOfWork"/>.</param>
    /// <param name="work">The work to be performed within the unit-of-work.</param>
    /// <param name="emitOutboxMetrics">The action to emit outbox metrics (where applicable).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The result of the <paramref name="work"/>.</returns>
    /// <remarks>This is intended to be used by the <see cref="IUnitOfWork"/> invoker to provide database-agnostic transaction handling (where applicable).</remarks>
    public static async Task<TResult> OrchestrateUnitOfWorkTransactionAsync<TResult>(InvokerTracer tracer, IDatabaseUnitOfWork unitOfWork, Func<Task<TResult>> work, Action<int>? emitOutboxMetrics, CancellationToken cancellationToken)
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

                    // Where a known/expected error, then log as debug; otherwise, log as a genuine error (exception).
                    if (exception is IExtendedException iex && iex.IsError)
                    {
                        if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                            tracer.Logger.LogDebug("Unit-of-work transaction rolled back due to error: {Error} [DatabaseId: {DatabaseId}]", exception.Message, unitOfWork.Database.DatabaseId);
                    }
                    else
                    {
                        if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Error))
                            tracer.Logger.LogError(exception, "Unit-of-work transaction rolled back due to an unexpected error: {Error} [DatabaseId: {DatabaseId}]", exception.Message, unitOfWork.Database.DatabaseId);
                    }
                }
                else
                {
                    await txn.RollbackAsync(savePoint, cancellationToken).ConfigureAwait(false);

                    // Where a known/expected error, then log as debug; otherwise, log as a genuine error (exception).
                    if (exception is IExtendedException iex && iex.IsError)
                    {
                        if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                            tracer.Logger.LogDebug("Unit-of-work transaction save-point '{SavePoint}' rolled back due to error: {Error} [DatabaseId: {DatabaseId}]", savePoint, exception.Message, unitOfWork.Database.DatabaseId);
                    }
                    else
                    {
                        if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Error))
                            tracer.Logger.LogError(exception, "Unit-of-work transaction save-point '{SavePoint}' rolled back due to an unexpected error: {Error} [DatabaseId: {DatabaseId}]", savePoint, exception.Message, unitOfWork.Database.DatabaseId);
                    }
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
            var result = await work().ConfigureAwait(false);

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
                    emitOutboxMetrics?.Invoke(outboxEnqueued);
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