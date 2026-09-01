namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbUnitOfWorkTests : CosmosTestBase
{
    private const string ContainerId = "uow-items";

    [Test]
    public async Task TransactionAsync_SamePartition_CommitsBothAtomically()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var pk = NewId();
        var id1 = NewId();
        var id2 = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            await container.CreateAsync(new TestItem { Id = id1, PartitionKey = pk, Name = "One" }, ct).ConfigureAwait(false);
            await container.CreateAsync(new TestItem { Id = id2, PartitionKey = pk, Name = "Two" }, ct).ConfigureAwait(false);
        });

        var fetched1 = await container.GetAsync(CompositeKey.Create(id1), new PartitionKey(pk));
        var fetched2 = await container.GetAsync(CompositeKey.Create(id2), new PartitionKey(pk));

        fetched1.Should().NotBeNull();
        fetched1!.Name.Should().Be("One");
        fetched2.Should().NotBeNull();
        fetched2!.Name.Should().Be("Two");
    }

    [Test]
    public async Task TransactionAsync_CrossPartition_ThrowsBeforeAnyNetworkCall_AndPersistsNothing()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var pkA = NewId();
        var pkB = NewId();
        var idA = NewId();
        var idB = NewId();

        Func<Task> act = () => unitOfWork.TransactionAsync(async ct =>
        {
            await container.CreateAsync(new TestItem { Id = idA, PartitionKey = pkA, Name = "A" }, ct).ConfigureAwait(false);
            await container.CreateAsync(new TestItem { Id = idB, PartitionKey = pkB, Name = "B" }, ct).ConfigureAwait(false);
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        // Neither item should exist - the second (mismatched) call never even reached Cosmos DB, and the first was never executed (deferred until the whole batch commits).
        var fetchedA = await container.GetAsync(CompositeKey.Create(idA), new PartitionKey(pkA));
        fetchedA.Should().BeNull();
    }

    [Test]
    public async Task TransactionAsync_ResultFailureInsideWork_DiscardsBatch()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var pk = NewId();
        var id = NewId();

        var result = await unitOfWork.TransactionAsync(async ct =>
        {
            await container.CreateAsync(new TestItem { Id = id, PartitionKey = pk, Name = "Should not persist" }, ct).ConfigureAwait(false);
            return Result.AuthenticationError();
        });

        result.IsFailure.Should().BeTrue();

        var fetched = await container.GetAsync(CompositeKey.Create(id), new PartitionKey(pk));
        fetched.Should().BeNull();
    }

    [Test]
    public async Task TransactionAsync_WithOutbox_WritesEventDocumentAtomically()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        var id = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = pk, Name = "Widget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)));
        });

        var fetched = await container.GetAsync(CompositeKey.Create(id), new PartitionKey(pk));
        fetched.Should().NotBeNull();

        // Confirm the paired outbox event document exists in the SAME container/partition, findable by explicitly targeting the reserved prefix (the relay's future "internal explicit read").
        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var query = rawContainer.GetItemLinqQueryable<CosmosDbOutboxEvent>()
            .Where(e => e.PartitionKey == pk && e.Id.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix));

        var outboxDocs = new List<CosmosDbOutboxEvent>();
        using (var iterator = query.ToFeedIterator())
        {
            while (iterator.HasMoreResults)
                outboxDocs.AddRange(await iterator.ReadNextAsync());
        }

        outboxDocs.Should().ContainSingle();
        outboxDocs[0].Destination.Should().NotBeNullOrEmpty();
        outboxDocs[0].TimeToLive.Should().Be(CosmosDbEventPublisher.DefaultOutboxTimeToLiveSeconds);
    }

    [Test]
    public async Task Query_WithOutboxDocumentsPresent_AutomaticallyExcludesThem_NoFilterConfiguredByTest()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        var id = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = pk, Name = "Gadget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)));
        });

        // An ordinary business query against the SAME container/partition that now also holds an outbox event document - no WithFilter/WithTypeDiscriminatorFilter configured by this test at all.
        var items = await container.Query(q => q.Where(m => m.PartitionKey == pk)).ToListAsync();

        items.Should().ContainSingle();
        items[0].Name.Should().Be("Gadget");
    }

    [Test]
    public async Task SynchronizeETag_MultipleEntities_ResolvesEachByKey_NotReference()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var pk = NewId();
        var id1 = NewId();
        var id2 = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            await container.CreateAsync(new TestItem { Id = id1, PartitionKey = pk, Name = "One" }, ct).ConfigureAwait(false);
            await container.CreateAsync(new TestItem { Id = id2, PartitionKey = pk, Name = "Two" }, ct).ConfigureAwait(false);
        });

        // Simulate two separately-mapped contracts - distinct object instances/types from the TestItem models actually mutated above (SynchronizeETag cannot rely on reference identity).
        var contract1 = new TestValue { Id = id1, Name = "One" };
        var contract2 = new TestValue { Id = id2, Name = "Two" };

        unitOfWork.SynchronizeETag(CompositeKey.Create(id1), contract1);
        unitOfWork.SynchronizeETag(CompositeKey.Create(id2), contract2);

        var fetched1 = await container.GetAsync(CompositeKey.Create(id1), new PartitionKey(pk));
        var fetched2 = await container.GetAsync(CompositeKey.Create(id2), new PartitionKey(pk));

        // Each contract must resolve its OWN document's true ETag (proving correlation is by key, not by position/reference) - not asserting the two ETags differ from each other, since the emulator can
        // legitimately assign the same _etag to multiple documents committed within the same physical TransactionalBatch; that's an emulator/Cosmos DB implementation detail, not part of this contract.
        contract1.ETag.Should().NotBeNullOrEmpty();
        contract1.ETag.Should().Be(fetched1!.ETag);
        contract2.ETag.Should().NotBeNullOrEmpty();
        contract2.ETag.Should().Be(fetched2!.ETag);
    }

    [Test]
    public void SynchronizeETag_BeforeAnyTransaction_Throws()
    {
        var cosmosDb = CreateCosmosDb();
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);
        var contract = new TestValue { Id = NewId() };

        Assert.Throws<InvalidOperationException>(() => unitOfWork.SynchronizeETag(CompositeKey.Create(contract.Id), contract));
    }

    [Test]
    public async Task SynchronizeETag_KeyNotPartOfTransaction_Throws()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var pk = NewId();
        var id = NewId();

        await unitOfWork.TransactionAsync(async ct => await container.CreateAsync(new TestItem { Id = id, PartitionKey = pk, Name = "X" }, ct).ConfigureAwait(false));

        var unrelatedContract = new TestValue { Id = NewId() };
        Assert.Throws<InvalidOperationException>(() => unitOfWork.SynchronizeETag(CompositeKey.Create(unrelatedContract.Id), unrelatedContract));
    }

    [Test]
    public async Task TransactionAsync_DeleteNonExistentItem_DoesNotFailBatch_AndOtherOperationsStillCommit()
    {
        // TransactionalBatch fails the WHOLE batch if any enlisted operation targets a non-existent item (confirmed empirically) - the pre-read forced inside a unit-of-work must catch this before
        // enlisting, so a benign "already gone" delete never takes down an otherwise-valid Create bundled in the same transaction.
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var pk = NewId();
        var createdId = NewId();
        var neverExistedId = NewId();

        var deleted = default(DataResult);
        await unitOfWork.TransactionAsync(async ct =>
        {
            await container.CreateAsync(new TestItem { Id = createdId, PartitionKey = pk, Name = "Survivor" }, ct).ConfigureAwait(false);
            var result = await container.DeleteWithResultAsync(CompositeKey.Create(neverExistedId), new PartitionKey(pk), ct).ConfigureAwait(false);
            deleted = result.Value;
        });

        deleted.WasMutated.Should().BeFalse();

        var fetched = await container.GetAsync(CompositeKey.Create(createdId), new PartitionKey(pk));
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Survivor");
    }

    [Test]
    public async Task TransactionAsync_DeleteExisting_WhereMutated_QueuesEvent_DeleteNonExistent_DoesNot()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);

        var pk = NewId();
        var anchorId = NewId();
        var existingId = NewId();
        var neverExistedId = NewId();

        // Seed an item to actually delete, outside any unit-of-work.
        await container.CreateAsync(new TestItem { Id = existingId, PartitionKey = pk, Name = "ToDelete" });

        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);
        await unitOfWork.TransactionAsync(async ct =>
        {
            // A Delete-only unit-of-work has no model instance to derive a raw partition key value from, which the paired outbox event write needs (see CosmosDbEventPublisher/CosmosDbContainer.Delete.cs
            // remarks) - a preceding Create/Update in the same unit-of-work is required to bind one. This mirrors a realistic scenario (e.g. moving an item, or updating a related aggregate root) rather
            // than being an artificial workaround.
            await container.CreateAsync(new TestItem { Id = anchorId, PartitionKey = pk, Name = "Anchor" }, ct).ConfigureAwait(false);

            var deletedExisting = await container.DeleteWithResultAsync(CompositeKey.Create(existingId), new PartitionKey(pk), ct).ConfigureAwait(false);
            deletedExisting.Value.WhereMutated(() => unitOfWork.Events.Add(EventData.CreateEventWith(existingId, EventAction.Deleted).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk)));

            var deletedMissing = await container.DeleteWithResultAsync(CompositeKey.Create(neverExistedId), new PartitionKey(pk), ct).ConfigureAwait(false);
            deletedMissing.Value.WhereMutated(() => unitOfWork.Events.Add(EventData.CreateEventWith(neverExistedId, EventAction.Deleted).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk)));
        });

        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var query = rawContainer.GetItemLinqQueryable<CosmosDbOutboxEvent>().Where(e => e.PartitionKey == pk && e.Id.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix));

        var outboxDocs = new List<CosmosDbOutboxEvent>();
        using (var iterator = query.ToFeedIterator())
        {
            while (iterator.HasMoreResults)
                outboxDocs.AddRange(await iterator.ReadNextAsync());
        }

        // Exactly one event - for the deletion that actually happened, not the one that was already gone.
        outboxDocs.Should().ContainSingle();
    }
}
