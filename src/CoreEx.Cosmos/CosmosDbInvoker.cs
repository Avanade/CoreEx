namespace CoreEx.Cosmos;

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
    /// ever sent to Cosmos DB before the batch executes). See <see cref="CosmosDbUnitOfWork"/>'s own remarks for the full model.</remarks>
    public static async Task<TResult> OrchestrateUnitOfWorkTransactionAsync<TResult>(InvokerTracer tracer, CosmosDbUnitOfWork unitOfWork, Func<Task<TResult>> work, Action<int>? emitOutboxMetrics, CancellationToken cancellationToken)
    {
        var txn = unitOfWork.CosmosDb.CurrentTransaction;
        var isRoot = txn is null;
        if (isRoot)
        {
            txn = new CosmosDbTransaction();
            unitOfWork.CosmosDb.UseTransaction(txn);
        }

        try
        {
            var result = await work().ConfigureAwait(false);

            // Nothing has been sent to Cosmos DB yet (deferred execution) - a failure simply discards the accumulated batch, no explicit rollback required.
            if (result is IResult ir && ir.IsFailure)
            {
                if (tracer.Logger is not null && tracer.Logger.IsEnabled(LogLevel.Debug))
                    tracer.Logger.LogDebug("Unit-of-work transaction discarded due to error: {Error}", ir.Error?.Message);

                return result;
            }

            if (isRoot)
            {
                var outboxEnqueued = 0;
                if (unitOfWork.AreEventsSupported && !unitOfWork.Events.IsEmpty)
                {
                    outboxEnqueued = unitOfWork.Outbox!.Count;
                    await unitOfWork.Outbox!.PublishAsync(cancellationToken).ConfigureAwait(false);
                }

                if (txn!.HasOperations)
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
                _ => new InvalidOperationException($"The CosmosDbUnitOfWork's TransactionalBatch failed with status code '{response.StatusCode}' at operation index {i} (operation status '{opResult.StatusCode}').")
            };
        }

        return new InvalidOperationException($"The CosmosDbUnitOfWork's TransactionalBatch failed with status code '{response.StatusCode}', but no specific failing operation could be identified.");
    }
}
