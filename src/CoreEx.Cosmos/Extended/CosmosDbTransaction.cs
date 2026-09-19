namespace CoreEx.Cosmos.Extended;

/// <summary>
/// Represents the ambient ("current"), single-container/single-partition-key <see cref="TransactionalBatch"/> scope of an active <see cref="CosmosDbUnitOfWork"/>.
/// </summary>
/// <remarks>Mirrors <c>IDatabase.CurrentTransaction</c>/<c>UseTransaction</c> — a plain mutable state holder on the shared, DI-scoped <see cref="ICosmosDb"/> instance, set/cleared by <see cref="CosmosDbUnitOfWork"/>
/// and read by <see cref="CosmosDbContainer{TModel}"/>'s Create/Update/Delete operations to transparently enlist instead of executing directly.
/// <para>Cosmos DB's <see cref="TransactionalBatch"/> is atomic <b>only</b> within a single container and a single logical partition key (a hard service limit, not a design choice) — this type enforces that
/// by binding to the <see cref="Container"/>/<see cref="Microsoft.Azure.Cosmos.PartitionKey"/> of the <i>first</i> enlisted operation and throwing immediately, client-side, on any later operation that
/// targets a different container or partition key.</para></remarks>
public sealed class CosmosDbTransaction
{
    private readonly Dictionary<CompositeKey, int> _operationIndexByKey = [];
    private Container? _container;
    private PartitionKey? _partitionKey;
    private string? _partitionKeyValue;
    private TransactionalBatch? _batch;
    private int _operationCount;

    /// <summary>
    /// Gets the number of operations enlisted so far.
    /// </summary>
    public int OperationCount => _operationCount;

    /// <summary>
    /// Indicates whether at least one operation has been enlisted.
    /// </summary>
    public bool HasOperations => _batch is not null;

    /// <summary>
    /// Indicates whether this transaction has been aborted (see <see cref="Abort"/>) by a failure detected at some nesting level, and must therefore never be committed - even where an enclosing/outer
    /// <see cref="CosmosDbUnitOfWork.TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/> work delegate ignores a nested call's returned failure and otherwise reports its own success.
    /// </summary>
    public bool IsAborted { get; private set; }

    /// <summary>
    /// Marks this transaction as <see cref="IsAborted"/>.
    /// </summary>
    /// <remarks>Cosmos DB's <see cref="TransactionalBatch"/> has no relational save-point equivalent - a nested <see cref="CosmosDbUnitOfWork.TransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>
    /// failure (an <see cref="IResult"/> failure returned by the nested work, or an exception converted to one) discards the <i>whole</i> accumulated batch (root and nested), per <see cref="CosmosDbUnitOfWork"/>'s
    /// documented nesting model. Since execution is deferred until the root call ends, that discard cannot be enforced by simply not enlisting further operations (earlier ones are already enlisted) - this
    /// flag is checked before the root actually executes the batch, so the whole unit-of-work is refused even if the nested failure's <see cref="IResult"/> was never propagated/checked by the enclosing work.</remarks>
    public void Abort() => IsAborted = true;

    /// <summary>
    /// Gets the <see cref="Container"/> bound by the first enlisted operation (see <see cref="Enlist(Container, PartitionKey, string?, CompositeKey, Action{TransactionalBatch})"/>); <see langword="null"/> where nothing has been enlisted yet.
    /// </summary>
    public Container? BoundContainer => _container;

    /// <summary>
    /// Gets the raw partition key <see cref="string"/> value bound by the first enlisted operation; <see langword="null"/> where nothing has been enlisted yet, or the first enlisted model did not expose one
    /// (see <see cref="Enlist(Container, PartitionKey, string?, CompositeKey, Action{TransactionalBatch})"/>).
    /// </summary>
    /// <remarks>The Cosmos DB SDK's <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> struct exposes no public way to extract its own value back out once constructed - this is tracked separately, as a raw
    /// <see langword="string"/>, specifically so a paired outbox-event write (see <see cref="CosmosDbEventPublisher"/>) can reuse the exact same partition key value without needing to re-derive it.</remarks>
    public string? BoundPartitionKeyValue => _partitionKeyValue;

    /// <summary>
    /// Gets the <see cref="TransactionalBatchResponse"/> once the batch has been executed (see <see cref="ExecuteAsync(CancellationToken)"/>); <see langword="null"/> beforehand.
    /// </summary>
    public TransactionalBatchResponse? Response { get; private set; }

    /// <summary>
    /// Enlists an operation into the ambient batch, binding it to the specified <paramref name="container"/>/<paramref name="partitionKey"/> if this is the first enlisted operation, or validating
    /// against that binding otherwise.
    /// </summary>
    /// <param name="container">The <see cref="Container"/> the operation targets.</param>
    /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/> the operation targets.</param>
    /// <param name="partitionKeyValue">The raw partition key <see cref="string"/> value, where known (see <see cref="BoundPartitionKeyValue"/>); <see langword="null"/> where the model does not expose one.</param>
    /// <param name="key">The <see cref="CompositeKey"/> identifying the model instance the operation mutates (used later by <see cref="IUnitOfWork.SynchronizeETag{T}(CompositeKey, T)"/>).</param>
    /// <param name="addOperation">The action which adds the actual operation (<c>CreateItem</c>/<c>ReplaceItem</c>/<c>DeleteItem</c>/etc.) to the <see cref="TransactionalBatch"/>.</param>
    /// <returns>The zero-based operation index (see <see cref="TransactionalBatchResponse.GetOperationResultAtIndex{T}(int)"/>).</returns>
    /// <exception cref="InvalidOperationException">Thrown where <paramref name="container"/>/<paramref name="partitionKey"/> differs from the first enlisted operation's binding.</exception>
    public int Enlist(Container container, PartitionKey partitionKey, string? partitionKeyValue, CompositeKey key, Action<TransactionalBatch> addOperation)
    {
        container.ThrowIfNull();
        addOperation.ThrowIfNull();

        if (_batch is null)
        {
            _container = container;
            _partitionKey = partitionKey;
            _partitionKeyValue = partitionKeyValue;
            _batch = container.CreateTransactionalBatch(partitionKey);
        }
        else if (!IsSameContainer(_container!, container) || !_partitionKey!.Value.Equals(partitionKey))
            throw new InvalidOperationException(
                $"An operation targeting container '{container.Id}'/partition key '{partitionKey}' was attempted within the same CosmosDbUnitOfWork as an earlier operation targeting container " +
                $"'{_container!.Id}'/partition key '{_partitionKey}'. Cosmos DB's TransactionalBatch is atomic only within a single container and a single logical partition key; all operations within " +
                $"one unit-of-work must target the same container and partition key.");

        addOperation(_batch);
        var index = _operationCount++;
        _operationIndexByKey[key] = index;
        return index;
    }

    /// <summary>
    /// Attempts to resolve the batch operation index for the specified <paramref name="key"/> (see <see cref="Enlist(Container, PartitionKey, string?, CompositeKey, Action{TransactionalBatch})"/>).
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="index">The resolved zero-based operation index, where found.</param>
    /// <returns><see langword="true"/> where <paramref name="key"/> was enlisted as part of this transaction; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOperationIndex(CompositeKey key, out int index) => _operationIndexByKey.TryGetValue(key, out index);

    /// <summary>
    /// Determines whether <paramref name="x"/> and <paramref name="y"/> represent the same logical container.
    /// </summary>
    /// <remarks>Compares <see cref="Container.Id"/>/<see cref="Database.Id"/> rather than reference equality. In practice every <see cref="Container"/> enlisted here is resolved via <c>ICosmosDb</c>'s own
    /// per-<c>containerId</c> cache, so reference equality alone would already hold - but relying on that as an implicit invariant is fragile (e.g. a caller constructing a <see cref="Container"/> directly
    /// from a <see cref="CosmosClient"/>/<see cref="Database"/>, bypassing that cache, would otherwise be wrongly treated as a different container to the one already bound). Comparing identifiers is just
    /// as cheap and removes the hidden coupling.</remarks>
    private static bool IsSameContainer(Container x, Container y) => ReferenceEquals(x, y) || (x.Id == y.Id && x.Database.Id == y.Database.Id);

    /// <summary>
    /// Executes the accumulated <see cref="TransactionalBatch"/> (where <see cref="HasOperations"/>); a no-op returning <see langword="null"/> otherwise.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="TransactionalBatchResponse"/>; <see langword="null"/> where <see cref="HasOperations"/> is <see langword="false"/>.</returns>
    /// <remarks>This is the single round trip that actually sends every enlisted operation to Cosmos DB, atomically, all-or-nothing — nothing enlisted via <see cref="Enlist(Container, PartitionKey, string?, CompositeKey, Action{TransactionalBatch})"/>
    /// is sent to Cosmos DB before this executes.</remarks>
    public async Task<TransactionalBatchResponse?> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_batch is null)
            return null;

        Response = await _batch.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Response;
    }
}
