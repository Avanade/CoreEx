namespace CoreEx.Database.Abstractions;

/// <summary>
/// Provides the standard <see cref="IDatabase"/> invoker functionality.
/// </summary>
/// <remarks>Catches any unhandled <see cref="DbException"/> and invokes <see cref="IDatabase.HandleDbException(DbException)"/> to handle (where <see cref="DatabaseArgsBase.TransformException"/>) is <see langword="true"/>
/// before bubbling up.
/// <para>Where <see cref="DatabaseArgsBase.RetryOnTransient"/> is <see langword="true"/>, also retries the invocation - via <see cref="DatabaseArgsBase.RetryResiliencePipeline"/> where supplied, otherwise a
/// default <see cref="DatabaseInvokerResiliency"/> pipeline - for any exception <see cref="IDatabase.IsTransientException(Exception)"/> classifies as transient. Where retries are exhausted and the
/// invocation's result type is itself an <see cref="IResult"/> (ROP), the conversion is applied directly and returned as a failure with <b>no exception thrown at all</b> - avoiding both the cost of an
/// unnecessary throw/catch round-trip and any need to synthesize a wrapping exception purely to carry the converted (e.g. <see cref="NotFoundException"/>/<see cref="ConcurrencyException"/>) error back out;
/// only a non-ROP result type, or one where no conversion applies, results in a single unwrapped throw (never an <see cref="AggregateException"/>) of the final (possibly retried)
/// exception.</para></remarks>
public abstract class DatabaseInvoker : InvokerBase<IDatabase, DatabaseArgs>
{
    /// <inheritdoc/>
    protected override async Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, IDatabase database, DatabaseArgs dbArgs, Func<InvokerTracer, DatabaseArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        try
        {
            return dbArgs.RetryOnTransient
                ? await InvokeWithRetryAsync(tracer, database, dbArgs, func, cancellationToken).ConfigureAwait(false)
                : await base.OnInvokeAsync(tracer, database, dbArgs, func, cancellationToken).ConfigureAwait(false);
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
    /// Executes <paramref name="func"/> under the <see cref="DatabaseArgsBase.RetryOnTransient"/> retry pipeline - the caller-supplied <see cref="DatabaseArgsBase.RetryResiliencePipeline"/> where
    /// specified; otherwise a cached-per-<typeparamref name="TResult"/> <see cref="DatabaseInvokerResiliency.CreateDefaultRetry{TResult}(TimeSpan?, int, DelayBackoffType)"/> default.
    /// </summary>
    /// <remarks>Only a <see cref="DbException"/> that <see cref="IDatabase.IsTransientException(Exception)"/> classifies as transient is ever retried; any other exception is rethrown immediately,
    /// unretried, and handled by the existing <see cref="DatabaseArgsBase.TransformException"/> logic in <see cref="OnInvokeAsync{TResult}"/> as before.</remarks>
    private static async Task<TResult> InvokeWithRetryAsync<TResult>(InvokerTracer tracer, IDatabase database, DatabaseArgs dbArgs, Func<InvokerTracer, DatabaseArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        var context = ResilienceContextPool.Shared.Get(cancellationToken);

        try
        {
            context.Properties.Set(ResilienceOwner<IDatabase>.PropertyKey, database);

            // Where an explicit (non-generic, payload-less) pipeline has been supplied, carry the successful value out via a captured local as Result itself carries no payload.
            if (dbArgs.RetryResiliencePipeline is not null)
            {
                TResult? value = default;

                var result = await dbArgs.RetryResiliencePipeline.ExecuteAsync(async rc =>
                {
                    try
                    {
                        value = await func(tracer, dbArgs, rc.CancellationToken).ConfigureAwait(false);
                        return Result.Success;
                    }
                    catch (DbException dbex) when (database.IsTransientException(dbex))
                    {
                        return Result.Fail(dbex);
                    }
                }, context).ConfigureAwait(false);

                if (result.IsFailure)
                {
                    if (TryConvertExhaustedRetryToResult(result.Error, database, dbArgs, tracer, out TResult? res))
                        return res;

                    ExceptionDispatchInfo.Capture(result.Error).Throw();
                }

                return value!;
            }

            // Otherwise, use the default, cached-per-TResult pipeline.
            var typedResult = await DefaultRetryPipeline<TResult>.Instance.ExecuteAsync(async rc =>
            {
                try
                {
                    return new Result<TResult>(await func(tracer, dbArgs, rc.CancellationToken).ConfigureAwait(false));
                }
                catch (DbException dbex) when (database.IsTransientException(dbex))
                {
                    return new Result<TResult>(dbex);
                }
            }, context).ConfigureAwait(false);

            if (typedResult.IsFailure)
            {
                if (TryConvertExhaustedRetryToResult(typedResult.Error, database, dbArgs, tracer, out TResult? res))
                    return res;

                ExceptionDispatchInfo.Capture(typedResult.Error).Throw();
            }

            return typedResult.Value;
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// Attempts to convert an exhausted-retry <see cref="DbException"/> directly into a <typeparamref name="TResult"/> failure - without throwing/catching - where <see cref="DatabaseArgsBase.TransformException"/>
    /// applies, <see cref="IDatabase.HandleDbException(DbException)"/> converts it, and <typeparamref name="TResult"/> is itself an <see cref="IResult"/> (ROP) capable of carrying that conversion as a failure.
    /// </summary>
    /// <remarks>This exists specifically to avoid an otherwise unnecessary throw/catch round-trip back through <see cref="OnInvokeAsync{TResult}"/> purely to reapply a conversion that can be performed
    /// here directly - Result's entire reason for being is to avoid the cost (and, for <see cref="IExtendedException"/> types, the loss of a natural returned-vs-thrown distinction) of exceptions for
    /// anticipated/expected failure paths, so an exhausted-retry failure destined for a <see cref="Result"/>-shaped <typeparamref name="TResult"/> should never need to be thrown at all.
    /// <para>Returns <see langword="false"/> - leaving the caller to throw <paramref name="error"/> unwrapped (see <see cref="ExceptionDispatchInfo"/>) - for every other case: a non-<see cref="DbException"/>
    /// error, <see cref="DatabaseArgsBase.TransformException"/> disabled, no conversion available, or a non-ROP <typeparamref name="TResult"/>; that single throw is then handled identically to a
    /// non-retried failure by the existing <see cref="DbException"/> handling in <see cref="OnInvokeAsync{TResult}"/>.</para></remarks>
    private static bool TryConvertExhaustedRetryToResult<TResult>(Exception error, IDatabase database, DatabaseArgs dbArgs, InvokerTracer tracer, [NotNullWhen(true)] out TResult? result)
    {
        result = default;

        if (!dbArgs.TransformException || error is not DbException dbex)
            return false;

        var hex = database.HandleDbException(dbex);
        if (hex is null)
            return false;

        if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
            tracer.Logger.LogDebug(dbex, "Database exception converted to '{ExceptionType}': {Message} [DatabaseId: {DatabaseId}]", hex.GetType().Name, hex.Message, database.DatabaseId);

        return ExtendedException.TryConvertExceptionToResult(hex, out result);
    }

    /// <summary>
    /// Caches the <see cref="DatabaseInvokerResiliency.CreateDefaultRetry{TResult}(TimeSpan?, int, DelayBackoffType)"/> pipeline per closed <typeparamref name="TResult"/>, avoiding rebuilding it on
    /// every retry-enabled invocation.
    /// </summary>
    private static class DefaultRetryPipeline<TResult>
    {
        public static readonly ResiliencePipeline<Result<TResult>> Instance = DatabaseInvokerResiliency.CreateDefaultRetry<TResult>();
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

        // Tracks whether THIS invocation is the one that actually called Outbox.PublishAsync - see its remarks (below) for why this cannot rely on Outbox.HasBeenPublished, which is global to the
        // (typically request-scoped, reused-across-calls) Outbox instance, not scoped to this invocation.
        var publishedByThisInvocation = false;

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

            // Where outbox/events are supported then also roll back any added events - Dequeue only functions pre-publish; where THIS invocation itself already published (e.g. it happened successfully
            // but the transaction/save-point itself still failed to commit afterward), use RollbackAsync instead to undo that already-captured publish. This deliberately checks publishedByThisInvocation
            // rather than Outbox.HasBeenPublished: the latter is a one-way, publisher-lifetime flag (see IEventPublisher.HasBeenPublished) that stays true for as long as the same Outbox instance is
            // reused across multiple, entirely independent OrchestrateUnitOfWorkTransactionAsync calls within one scope (e.g. a request-scoped IUnitOfWork used for several sequential TransactionAsync
            // calls) - relying on it here would wrongly invoke RollbackAsync for a later, unrelated failed invocation that never itself published anything, undoing an earlier invocation's genuinely
            // successful and already-committed publish. Dequeue itself also refuses to run at all once HasBeenPublished is (globally) true - regardless of count - so it must only be called when THIS
            // invocation actually added events of its own to remove; a later, unrelated failed invocation that added none has nothing to dequeue and must not touch the Outbox at all.
            if (unitOfWork.Outbox is not null)
            {
                if (publishedByThisInvocation)
                    await unitOfWork.Outbox.RollbackAsync(cancellationToken).ConfigureAwait(false);
                else
                {
                    var addedByThisInvocation = Math.Max(0, unitOfWork.Outbox.Count - eventStartCount);
                    if (addedByThisInvocation > 0)
                        unitOfWork.Outbox.Dequeue(addedByThisInvocation);
                }
            }
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
                    outboxEnqueued = unitOfWork.Outbox!.Count;
                    await unitOfWork.Outbox!.PublishAsync(cancellationToken).ConfigureAwait(false);
                    publishedByThisInvocation = true;
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