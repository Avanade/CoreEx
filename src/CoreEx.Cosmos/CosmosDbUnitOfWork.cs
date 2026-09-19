namespace CoreEx.Cosmos;

/// <summary>
/// Provides the transactional <see cref="IUnitOfWork"/> implementation for <see cref="ICosmosDb"/>, including support for a
/// <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see> via <see cref="CosmosDbEventPublisher"/>.
/// </summary>
/// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
/// <param name="outbox">The optional <see cref="IEventPublisher"/> (typically a <see cref="CosmosDbEventPublisher"/>).</param>
/// <param name="invoker">The optional <see cref="CosmosDbUnitOfWorkInvoker"/> used to orchestrate the <see cref="IUnitOfWork"/> functionality.</param>
/// <remarks>Implements the tech-agnostic <see cref="IUnitOfWork"/> directly — deliberately <b>not</b> a Cosmos-specific sub-interface — so application-layer services stay fully provider-agnostic.
/// <para><b>Cosmos DB's only atomic multi-operation primitive (<see cref="TransactionalBatch"/>) is atomic only within a single container and a single logical partition key</b> — a hard Cosmos DB service
/// limit, not a design choice. This is surfaced as an explicit, enforced rule: the first <see cref="CosmosDbContainer{TModel}"/> Create/Update/Delete call made from within <see cref="TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
/// binds the ambient <see cref="CosmosDbTransaction"/> to that container/partition key; any later operation targeting a different one throws <see cref="InvalidOperationException"/> immediately, client-side,
/// before any network call — reinforced by (not solely reliant on) Cosmos DB's own server-side all-or-nothing rejection of a genuinely mismatched batch.</para>
/// <para><b>Execution is deferred</b> — enlisted operations are queued into the ambient <see cref="TransactionalBatch"/>, not sent immediately, and the batch executes once, at the end of the <i>root</i>
/// <see cref="TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/> call. On failure (an exception, or an <see cref="IResult"/> failure returned by the work), the accumulated batch is simply
/// discarded — nothing was ever sent to Cosmos DB, so there is nothing to roll back, unlike a relational unit-of-work's real rollback. A consequence of this: there is <b>no "read your own uncommitted
/// writes" within a single unit-of-work</b> — a <c>Query()</c>/<c>GetAsync</c> call inside the work cannot see an earlier write from the <i>same</i> unit-of-work, since nothing is actually persisted until
/// the final batch executes. This is a real, unavoidable divergence from a relational unit-of-work's immediate-execution-within-an-open-transaction model.</para>
/// <para><b>Nesting</b>: a nested <see cref="TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/> call flows into the same outer ambient batch (consistent with <see cref="IUnitOfWork"/>'s own
/// "flows an existing transaction" contract) rather than throwing. There is no Cosmos DB equivalent of a relational save point, so a nested failure discards the <i>whole</i> accumulated batch (root and
/// nested) — simpler than save-point rollback, and still fully safe, since nothing ever partially persists.</para>
/// <para>The actual transaction orchestration (begin/flow/execute, outbox publish, exception mapping, metrics) lives in <see cref="CosmosDbInvoker.OrchestrateUnitOfWorkTransactionAsync{TResult}(InvokerTracer, CosmosDbUnitOfWork, Func{Task{TResult}}, Action{int}?, CancellationToken)"/>,
/// invoked via <see cref="UnitOfWorkInvoker"/> — mirrors <c>SqlServerUnitOfWork</c>/<c>SqlServerUnitOfWorkInvoker</c>'s split of responsibility exactly.</para></remarks>
public sealed class CosmosDbUnitOfWork(ICosmosDb cosmosDb, IEventPublisher? outbox = null, CosmosDbUnitOfWorkInvoker? invoker = null) : IUnitOfWork
{
    /// <summary>
    /// Gets the underlying <see cref="ICosmosDb"/>.
    /// </summary>
    public ICosmosDb CosmosDb { get; } = cosmosDb.ThrowIfNull();

    /// <summary>
    /// Gets the optional <see cref="IEventPublisher"/> to be used as a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
    /// </summary>
    public IEventPublisher? Outbox { get; } = outbox;

    /// <summary>
    /// Gets the underlying <see cref="CosmosDbUnitOfWorkInvoker"/> used to orchestrate the <see cref="IUnitOfWork"/> functionality.
    /// </summary>
    public CosmosDbUnitOfWorkInvoker UnitOfWorkInvoker { get; } = invoker ?? CosmosDbUnitOfWorkInvoker.Default;

    /// <summary>
    /// Gets or sets the most recently completed root <see cref="CosmosDbTransaction"/> in this scope, retained independently of the ambient <see cref="ICosmosDb.CurrentTransaction"/> (which is always
    /// cleared once <see cref="TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/> returns) specifically so <see cref="SynchronizeETag{T}(CompositeKey, T)"/> can resolve against it
    /// afterwards. Set by <see cref="CosmosDbInvoker.OrchestrateUnitOfWorkTransactionAsync{TResult}(InvokerTracer, CosmosDbUnitOfWork, Func{Task{TResult}}, Action{int}?, CancellationToken)"/> only.
    /// </summary>
    public CosmosDbTransaction? LastTransaction { get; internal set; }

    /// <inheritdoc/>
    /// <remarks>The <see cref="Outbox"/> is required to enable.</remarks>
    public bool AreEventsSupported => Outbox is not null;

    /// <inheritdoc/>
    public IEventQueue Events => Outbox ?? throw new NotSupportedException($"A Transaction {nameof(Outbox)} has not been provided to enable {nameof(Events)}.");

    /// <inheritdoc/>
    public Task TransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default) => TransactionAsync(CosmosDb.DbArgs, work, cancellationToken);

    /// <inheritdoc/>
    public Task<T> TransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default) => TransactionAsync(CosmosDb.DbArgs, work, cancellationToken);

    /// <inheritdoc/>
    /// <remarks><paramref name="args"/> is not currently leveraged by the Cosmos DB implementation; provided only to satisfy <see cref="IUnitOfWork"/>'s advanced/configurable-scenario overload.</remarks>
    public Task TransactionAsync(IDataArgs args, Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
        => UnitOfWorkInvoker.InvokeAsync(this, (CosmosDbArgs)args, async (_, _, ct) => { await work(ct).ConfigureAwait(false); return true; }, cancellationToken);

    /// <inheritdoc/>
    /// <remarks><paramref name="args"/> is not currently leveraged by the Cosmos DB implementation; provided only to satisfy <see cref="IUnitOfWork"/>'s advanced/configurable-scenario overload.</remarks>
    public Task<T> TransactionAsync<T>(IDataArgs args, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
        => UnitOfWorkInvoker.InvokeAsync(this, (CosmosDbArgs)args, async (_, _, ct) => await work(ct).ConfigureAwait(false), cancellationToken);

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown where there is no completed transaction to synchronize from (i.e. called before any <see cref="TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
    /// in this scope has completed, or from within its <c>work</c> delegate before the batch has actually executed), or where <paramref name="key"/> was not part of the most recently completed
    /// transaction's tracked mutations.</exception>
    public void SynchronizeETag<T>(CompositeKey key, T value) where T : IETag
    {
        value.ThrowIfNull();

        var txn = LastTransaction ?? throw new InvalidOperationException($"{nameof(SynchronizeETag)} can only be called after a {nameof(TransactionAsync)} has completed; there is no completed transaction to synchronize from.");

        if (txn.Response is null)
            throw new InvalidOperationException($"{nameof(SynchronizeETag)} can only be called after {nameof(TransactionAsync)} has completed successfully with at least one persisted operation - it cannot be called from within the {nameof(TransactionAsync)} work delegate itself, before the batch has executed.");

        if (!txn.TryGetOperationIndex(key, out var index))
            throw new InvalidOperationException($"The specified key was not part of the most recently completed {nameof(TransactionAsync)}'s tracked mutations; {nameof(SynchronizeETag)} cannot resolve an ETag for it.");

        value.ETag = txn.Response.GetOperationResultAtIndex<object>(index).ETag;
    }
}
