namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Provides the standard <see cref="ICosmosDb"/> invoker functionality.
/// </summary>
/// <remarks>Catches any unhandled <see cref="CosmosException"/> and invokes <see cref="ICosmosDb.HandleCosmosException(CosmosException)"/> to handle before bubbling up.</remarks>
[InvokerName("CoreEx.Cosmos.CosmosDb")]
public class CosmosDbInvoker : InvokerBase<ICosmosDb, CosmosDbArgs>
{
    private static CosmosDbInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="CosmosDbInvoker"/> instance.
    /// </summary>
    public static CosmosDbInvoker Default => ExecutionContext.GetService<CosmosDbInvoker>() ?? (_default ??= new CosmosDbInvoker());

    /// <inheritdoc/>
    public override bool IsTracingDisabled => true;

    /// <inheritdoc/>
    protected override async Task<TResult> OnInvokeAsync<TResult>(InvokerTracer tracer, ICosmosDb cosmosDb, CosmosDbArgs args, Func<InvokerTracer, CosmosDbArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
    {
        try
        {
            return await base.OnInvokeAsync(tracer, cosmosDb, args, func, cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException cex)
        {
            var hex = cosmosDb.HandleCosmosException(cex);
            if (hex is not null)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug(cex, "Cosmos exception converted to '{ExceptionType}': {Message}", hex.GetType().Name, hex.Message);

                // Where the result is an IResult (ROP) and the exception is considered an error then return as an IResult _failure_.
                if (ExtendedException.TryConvertExceptionToResult<TResult>(hex, out var res))
                    return res;

                throw hex;
            }

            throw;
        }
    }

    /// <summary>
    /// Provides standardized Cosmos DB unit-of-work transaction handling for a <see cref="CosmosDbUnitOfWork"/>, including nested-transaction flow-through and outbox/event publishing where supported.
    /// </summary>
    /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
    /// <param name="tracer">The <see cref="InvokerTracer"/>.</param>
    /// <param name="unitOfWork">The <see cref="CosmosDbUnitOfWork"/>.</param>
    /// <param name="work">The work to be performed within the unit-of-work.</param>
    /// <param name="emitOutboxMetrics">The action to emit outbox metrics (where applicable).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The result of the <paramref name="work"/>.</returns>
    /// <remarks>This is intended to be used by <see cref="Extended.CosmosDbUnitOfWorkInvoker"/> to provide the <see cref="CosmosDbUnitOfWork"/>-specific transaction handling, mirroring
    /// <c>DatabaseInvoker.OrchestrateUnitOfWorkTransactionAsync</c>'s shape while diverging in mechanics where Cosmos DB's <see cref="TransactionalBatch"/> genuinely differs from an ADO.NET transaction
    /// (deferred, all-at-once execution rather than immediate per-statement execution with a later commit; no save-point equivalent for nesting; nothing to explicitly roll back on failure, since nothing is
    /// ever sent to Cosmos DB before the batch executes). See <see cref="CosmosDbUnitOfWork"/>'s own remarks for the full model.
    /// <para><see cref="IEventPublisher.PublishAsync(CancellationToken)"/> (below) necessarily happens <i>before</i> the batch actually executes - it is what enlists the outbox event document into the
    /// same atomic batch as the business mutation in the first place. This means a batch that fails to commit (e.g. a concurrency conflict) does so <i>after</i> publish already completed successfully;
    /// <see cref="IEventPublisher.RollbackAsync(CancellationToken)"/> is called in that case so a test-only capture (see <c>EventPublisherDecorator</c>) doesn't wrongly believe an event was published
    /// when nothing was ever actually persisted. Where publish has not yet happened, a failure at any nesting level instead <see cref="IEventPublisher.Dequeue(int)"/>s only the events <i>that level</i>
    /// itself added - mirroring <c>DatabaseInvoker.OrchestrateUnitOfWorkTransactionAsync</c>'s <c>eventStartCount</c> bookkeeping - so a failure inside a nested <see cref="CosmosDbUnitOfWork.TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
    /// never discards events an enclosing/outer scope already queued before the nested call began.</para>
    /// <para>A failure at any nesting level also marks the shared ambient <see cref="CosmosDbTransaction"/> <see cref="CosmosDbTransaction.Abort"/>ed. Since execution is deferred until the root call
    /// ends, a nested failure cannot itself prevent a later root commit by simply not enlisting further operations - operations from before the failure are already enlisted. The root checks
    /// <see cref="CosmosDbTransaction.IsAborted"/> before executing the batch, so the whole unit-of-work is still refused even where an enclosing/outer work delegate ignores a nested call's returned
    /// failure and otherwise reports its own success - consistent with <see cref="CosmosDbUnitOfWork"/>'s documented "a nested failure discards the whole accumulated batch" nesting model.</para></remarks>
    public static async Task<TResult> OrchestrateUnitOfWorkTransactionAsync<TResult>(InvokerTracer tracer, CosmosDbUnitOfWork unitOfWork, Func<Task<TResult>> work, Action<int>? emitOutboxMetrics, CancellationToken cancellationToken)
    {
        var txn = unitOfWork.CosmosDb.CurrentTransaction;
        var isRoot = txn is null;
        if (isRoot)
        {
            txn = new CosmosDbTransaction();
            unitOfWork.CosmosDb.UseTransaction(txn);
        }

        // Events queued by an OUTER/ancestor scope (before this nesting level's work even started) must never be discarded by a failure at THIS level alone - only the events this level itself added.
        var eventStartCount = unitOfWork.Outbox?.Count ?? 0;

        // Reusable discard logic for any failure detected at this nesting level. Marks the shared ambient CosmosDbTransaction as aborted (see CosmosDbTransaction.Abort's remarks) so the root refuses to
        // commit even where an enclosing/outer work delegate ignores this level's returned failure and otherwise reports its own success, and rolls back/dequeues only the outbox events added at this
        // level - Dequeue only functions pre-publish (publish only ever happens once, at the very end of the root's own commit step); where it already completed, RollbackAsync undoes it instead.
        async Task DiscardAsync()
        {
            txn!.Abort();

            if (unitOfWork.Outbox is not null)
            {
                if (unitOfWork.Outbox.HasBeenPublished)
                    await unitOfWork.Outbox.RollbackAsync(cancellationToken).ConfigureAwait(false);
                else
                    unitOfWork.Outbox.Dequeue(Math.Max(0, unitOfWork.Outbox.Count - eventStartCount));
            }
        }

        try
        {
            var result = await work().ConfigureAwait(false);

            // Nothing has been sent to Cosmos DB yet (deferred execution) - a failure simply discards the accumulated batch, no explicit rollback required.
            if (result is IResult ir && ir.IsFailure)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug("Unit-of-work transaction discarded due to error: {Error}", ir.Error?.Message);

                await DiscardAsync().ConfigureAwait(false);
                return result;
            }

            if (isRoot)
            {
                // A nested TransactionAsync failure that the enclosing work silently ignored (returned its own success despite it) must still discard the whole batch - see CosmosDbTransaction.Abort's remarks.
                if (txn!.IsAborted)
                    throw new InvalidOperationException("The CosmosDbUnitOfWork's transaction was aborted by a nested TransactionAsync failure that was not returned/propagated by the enclosing work; the accumulated batch has been discarded and cannot be committed.");

                var outboxEnqueued = 0;
                if (unitOfWork.AreEventsSupported && !unitOfWork.Events.IsEmpty)
                {
                    outboxEnqueued = unitOfWork.Outbox!.Count;
                    await unitOfWork.Outbox!.PublishAsync(cancellationToken).ConfigureAwait(false);
                }

                if (txn.HasOperations)
                {
                    var response = await txn.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    if (response is not null && !response.IsSuccessStatusCode)
                        throw CreateBatchFailureException(response);

                    if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                        tracer.Logger.LogDebug("Unit-of-work transaction committed successfully.");
                }

                if (outboxEnqueued > 0)
                    emitOutboxMetrics?.Invoke(outboxEnqueued);
            }

            return result;
        }
        catch (CosmosException cex)
        {
            await DiscardAsync().ConfigureAwait(false);

            // Mirrors OnInvokeAsync's per-call exception mapping - a raw CosmosException can still surface directly from ExecuteAsync itself (e.g. a genuine transport/service failure), distinct from a
            // "logical" failure already surfaced via the TransactionalBatchResponse and translated by CreateBatchFailureException below.
            var hex = unitOfWork.CosmosDb.HandleCosmosException(cex);
            if (hex is not null)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug(cex, "Unit-of-work transaction discarded; Cosmos exception converted to '{ExceptionType}': {Message}", hex.GetType().Name, hex.Message);

                if (ExtendedException.TryConvertExceptionToResult<TResult>(hex, out var hres))
                    return hres;

                throw hex;
            }

            throw;
        }
        catch (Exception ex)
        {
            await DiscardAsync().ConfigureAwait(false);

            if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Error))
                tracer.Logger.LogError(ex, "Unit-of-work transaction discarded due to an unexpected error: {Error}", ex.Message);

            if (ExtendedException.TryConvertExceptionToResult<TResult>(ex, out var result))
                return result;

            throw;
        }
        finally
        {
            if (isRoot)
            {
                // Retained (independent of the ambient scope, which is always cleared here) so IUnitOfWork.SynchronizeETag can resolve against it after this call returns.
                unitOfWork.LastTransaction = txn;
                unitOfWork.CosmosDb.UseTransaction(null);
            }
        }
    }

    /// <summary>
    /// Builds a representative exception for a failed <see cref="TransactionalBatchResponse"/>, using the same status-code-to-exception mapping as <see cref="ICosmosDb.HandleCosmosException(CosmosException)"/>.
    /// </summary>
    private static Exception CreateBatchFailureException(TransactionalBatchResponse response)
    {
        for (var i = 0; i < response.Count; i++)
        {
            var opResult = response.GetOperationResultAtIndex<object>(i);

            // A 'FailedDependency' operation did not itself fail - it was rolled back because another operation in the same batch did; skip to find the actual cause.
            if (opResult.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.NoContent or HttpStatusCode.FailedDependency)
                continue;

            return opResult.StatusCode switch
            {
                HttpStatusCode.NotFound => new NotFoundException(),
                HttpStatusCode.Conflict => new DuplicateException(),
                HttpStatusCode.PreconditionFailed => new ConcurrencyException(),
                _ => new InvalidOperationException($"The CosmosDbUnitOfWork's TransactionalBatch failed with status code '{response.StatusCode}' at operation index {i} (operation status '{opResult.StatusCode}'): {response.ErrorMessage}")
            };
        }

        return new InvalidOperationException($"The CosmosDbUnitOfWork's TransactionalBatch failed with status code '{response.StatusCode}', but no specific failing operation could be identified.");
    }
}
