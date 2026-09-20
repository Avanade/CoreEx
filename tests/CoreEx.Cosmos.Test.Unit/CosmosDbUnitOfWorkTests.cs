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

        var fetched1 = await container.GetAsync(CompositeKey.Create(id1), pk);
        var fetched2 = await container.GetAsync(CompositeKey.Create(id2), pk);

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
        var fetchedA = await container.GetAsync(CompositeKey.Create(idA), pkA);
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

        var fetched = await container.GetAsync(CompositeKey.Create(id), pk);
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

        var fetched = await container.GetAsync(CompositeKey.Create(id), pk);
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
    public async Task TransactionAsync_WithOutbox_PartitionKeyNone_WritesEventDocumentAtomically()
    {
        // NoPartitionKeyItem implements neither IPartitionKey nor IReadOnlyPartitionKey, and no WithPartitionKey/WithFixedPartitionKey is configured here either - the business mutation's own partition
        // key resolves to PartitionKey.None, a real, valid single logical partition (not an error) - confirms the outbox event document can still be enlisted/co-located there too (see the
        // CosmosDbEventPublisher.OnPublishAsync fix this test guards against regressing).
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<NoPartitionKeyItem>(ContainerId);
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var id = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new NoPartitionKeyItem { Id = id, Name = "Widget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)));
        });

        var fetched = await container.GetAsync(CompositeKey.Create(id));
        fetched.Should().NotBeNull();

        // Confirm the paired outbox event document exists in the SAME container/PartitionKey.None partition. Filtering "PartitionKey == null" server-side would not match a truly absent field in
        // Cosmos SQL (undefined != null), so the None-partition check is applied client-side after retrieval instead. Scoped to this run's own event via the CloudEvent "subject" (the entity id) -
        // unlike the sibling TransactionAsync_WithOutbox_WritesEventDocumentAtomically test (scoped by a unique per-run partition key value), every run of this test shares the same PartitionKey.None
        // partition, so leftover documents from earlier runs against this same (never-reset) container would otherwise also match.
        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var query = rawContainer.GetItemLinqQueryable<CosmosDbOutboxEvent>()
            .Where(e => e.Id.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix));

        var outboxDocs = new List<CosmosDbOutboxEvent>();
        using (var iterator = query.ToFeedIterator())
        {
            while (iterator.HasMoreResults)
                outboxDocs.AddRange(await iterator.ReadNextAsync());
        }

        var matchingDocs = outboxDocs.Where(d => d.PartitionKey is null && d.Event.GetProperty("subject").GetString() == id).ToList();
        matchingDocs.Should().ContainSingle();
        matchingDocs[0].Destination.Should().NotBeNullOrEmpty();
    }

    // Regression test for a review-flagged bug: CosmosDbOutboxEvent always serializes its partition key under the fixed JSON property name "partitionKey", which is only correct where the container's
    // actual, physical partition-key path (set at container-creation time, independent of C# property names) is literally "/partitionKey" - the convention every other test container in this fixture
    // uses. A container configured with a different path (here "/tenantId") would otherwise silently produce an outbox document with no value at that path, and the paired TransactionalBatch would then
    // fail with an undiagnosable BadRequest. CosmosDbEventPublisher.OnPublishAsync now validates this up front (see EnsureOutboxPartitionKeyPathAsync) and fails fast with a clear, actionable exception -
    // and, since that check runs before anything is enlisted, the business mutation itself is never committed either.
    [Test]
    public async Task TransactionAsync_WithOutbox_ContainerPartitionKeyPathIsNotPartitionKey_ThrowsBeforeEnlistingAnything()
    {
        const string containerId = "uow-wrong-pk-path";
        await GetOrCreateContainerAsync(containerId, "/tenantId").ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<NoPartitionKeyItem>(containerId);
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var id = NewId();

        Func<Task> act = () => unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new NoPartitionKeyItem { Id = id, Name = "Widget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)));
        });

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Contain("/tenantId");

        // The business mutation must not have been committed either - the pre-flight partition-key-path check runs, and throws, before anything is enlisted into the TransactionalBatch.
        var fetched = await container.GetAsync(CompositeKey.Create(id));
        fetched.Should().BeNull();
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

        // An ordinary business query against the SAME container/partition that now also holds an outbox event document - no WithFilter/WithTypeDiscriminator configured by this test at all.
        var items = await container.Query(q => q.Where(m => m.PartitionKey == pk)).ToListAsync();

        items.Should().ContainSingle();
        items[0].Name.Should().Be("Gadget");
    }

    [Test]
    public async Task Query_WithOutboxDocumentsPresent_AutomaticallyExcludesThem_ModelWithoutIIdentifierInterface()
    {
        // NonIdentifierKeyedItem deliberately implements neither IIdentifier<string> nor IReadOnlyIdentifier<string> (its Cosmos DB "id" is exposed via a differently-named, [JsonPropertyName("id")]
        // decorated property instead) - regression test for CosmosDbModelOptions<TModel>.ApplyFilters' reflection-based fallback, confirming the automatic outbox-document exclusion still applies even
        // when IdentifierSupport is not supported.
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<NonIdentifierKeyedItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        var id = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new NonIdentifierKeyedItem { DocumentId = id, PartitionKey = pk, Name = "Gadget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value.DocumentId, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk));
        });

        // An ordinary business query against the SAME container/partition that now also holds an outbox event document.
        var items = await container.Query(q => q.Where(m => m.PartitionKey == pk)).ToListAsync();

        items.Should().ContainSingle();
        items[0].Name.Should().Be("Gadget");
    }

    [Test]
    public async Task Query_WithOutboxDocumentsPresent_AutomaticallyExcludesThem_ModelWithConventionalUnannotatedIdProperty()
    {
        // ConventionIdKeyedItem deliberately implements neither IIdentifier<string> nor IReadOnlyIdentifier<string>, and its "Id" property carries no [JsonPropertyName] attribute at all - regression test
        // for CosmosDbModelOptions<TModel>.ApplyFilters' reflection-based fallback, confirming the automatic outbox-document exclusion also applies to a plain, conventionally-named "Id" property (as a
        // serializer configured with a naming policy, e.g. camelCase, would map to Cosmos DB's reserved "id"), not just one carrying an explicit [JsonPropertyName("id")] attribute.
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<ConventionIdKeyedItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        var id = NewId();

        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new ConventionIdKeyedItem { Id = id, PartitionKey = pk, Name = "Gadget" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value.Id, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk));
        });

        // An ordinary business query against the SAME container/partition that now also holds an outbox event document.
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

        var fetched1 = await container.GetAsync(CompositeKey.Create(id1), pk);
        var fetched2 = await container.GetAsync(CompositeKey.Create(id2), pk);

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
            var result = await container.DeleteWithResultAsync(CompositeKey.Create(neverExistedId), pk, ct).ConfigureAwait(false);
            deleted = result.Value;
        });

        deleted.WasMutated.Should().BeFalse();

        var fetched = await container.GetAsync(CompositeKey.Create(createdId), pk);
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

            var deletedExisting = await container.DeleteWithResultAsync(CompositeKey.Create(existingId), pk, ct).ConfigureAwait(false);
            deletedExisting.Value.WhereMutated(() => unitOfWork.Events.Add(EventData.CreateEventWith(existingId, EventAction.Deleted).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)).WithPartitionKey(pk)));

            var deletedMissing = await container.DeleteWithResultAsync(CompositeKey.Create(neverExistedId), pk, ct).ConfigureAwait(false);
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

    [Test]
    public async Task TransactionAsync_UpsertNewKey_CreatesItem_DoesNotFailBatch()
    {
        // Regression: UpsertAsync's non-transactional "try Update, retry as Create on Not Found" cannot work inside a CosmosDbUnitOfWork - ReplaceItem is only enlisted (queued), so a missing item's 404
        // can only be observed once the whole TransactionalBatch executes, by which point retrying is too late and the entire batch fails instead. A forced pre-read must determine existence up-front.
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var pk = NewId();
        var newId = NewId();

        var upserted = default(DataResult<TestItem>);
        await unitOfWork.TransactionAsync(async ct =>
        {
            upserted = await container.UpsertAsync(new TestItem { Id = newId, PartitionKey = pk, Name = "Brand New" }, ct).ConfigureAwait(false);
        });

        upserted.WasMutated.Should().BeTrue();

        var fetched = await container.GetAsync(CompositeKey.Create(newId), pk);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Brand New");
    }

    [Test]
    public async Task TransactionAsync_UpsertExistingKey_UpdatesItem()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));

        var pk = NewId();
        var id = NewId();

        // Seed outside any unit-of-work.
        await container.CreateAsync(new TestItem { Id = id, PartitionKey = pk, Name = "Original" });

        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);
        await unitOfWork.TransactionAsync(async ct => await container.UpsertAsync(new TestItem { Id = id, PartitionKey = pk, Name = "Replaced" }, ct).ConfigureAwait(false));

        var fetched = await container.GetAsync(CompositeKey.Create(id), pk);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Replaced");
    }

    [Test]
    public async Task TransactionAsync_UpsertExistingKey_PartitionKeySelectorDependsOnStampedTenantId_UpdatesItem()
    {
        // Regression: the transactional upsert's pre-read must resolve the partition key AFTER the tenant stamping that Model.PrepareCreate/PrepareUpdate perform, not before. Here WithPartitionKey
        // selects off TenantId, which is auto-stamped from the ExecutionContext and never caller-supplied. Resolving the partition key from the caller's unstamped model (TenantId still null) would
        // read under PartitionKey.None, miss the already-existing item (seeded under the real "tenant-a" partition), and wrongly enlist a Create instead of an Update - which then fails the whole
        // batch with a conflict, since a document with that id already exists (just in a different partition than the one the buggy pre-read checked).
        const string containerId = "uow-tenant-pk-items";
        await GetOrCreateContainerAsync(containerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb("tenant-a");
        var container = cosmosDb.Container<TenantItem>(containerId, o => o.WithPartitionKey(m => m.TenantId));

        var id = NewId();

        // Seed outside any unit-of-work; TenantId (and therefore the partition key) is stamped automatically from the ExecutionContext ("tenant-a").
        await container.CreateAsync(new TenantItem { Id = id, Name = "Original" });

        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        // The caller does not set TenantId - by design it is never caller-supplied, only auto-stamped - so the model handed to UpsertAsync starts with a null TenantId/partition key.
        await unitOfWork.TransactionAsync(async ct => await container.UpsertAsync(new TenantItem { Id = id, Name = "Replaced" }, ct).ConfigureAwait(false));

        var fetched = await container.GetAsync(CompositeKey.Create(id), "tenant-a");
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Replaced");
    }

    [Test]
    public async Task TransactionAsync_NestedFailureIgnoredByOuterWork_AbortsWholeBatch_NothingPersists()
    {
        // Regression: a nested TransactionAsync failure only returns the failed IResult - it does not itself prevent a later root commit, since Cosmos DB execution is deferred until the root call ends
        // and everything enlisted so far (root and nested) is still sitting in the same ambient TransactionalBatch. If the outer work below ignores/swallows that failure (a caller bug) and otherwise
        // reports its own success, the whole unit-of-work must still be discarded - not partially committed - per CosmosDbUnitOfWork's documented "a nested failure discards the whole batch" model.
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var pk = NewId();
        var outerCreatedId = NewId();
        var nestedCreatedId = NewId();

        Func<Task> act = () => unitOfWork.TransactionAsync(async ct =>
        {
            await container.CreateAsync(new TestItem { Id = outerCreatedId, PartitionKey = pk, Name = "Outer" }, ct).ConfigureAwait(false);

            var nestedResult = await unitOfWork.TransactionAsync(async ct2 =>
            {
                await container.CreateAsync(new TestItem { Id = nestedCreatedId, PartitionKey = pk, Name = "Nested" }, ct2).ConfigureAwait(false);
                return Result.AuthenticationError();
            });

            // Deliberately not checking nestedResult - simulates a caller bug that ignores a nested TransactionAsync failure and continues regardless.
            _ = nestedResult;
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        var fetchedOuter = await container.GetAsync(CompositeKey.Create(outerCreatedId), pk);
        var fetchedNested = await container.GetAsync(CompositeKey.Create(nestedCreatedId), pk);
        fetchedOuter.Should().BeNull();
        fetchedNested.Should().BeNull();
    }

    [Test]
    public async Task TransactionAsync_ReusedAfterFailure_DoesNotLeakEventFromAbandonedTransaction()
    {
        // Regression: an event queued inside a failed/abandoned TransactionAsync must be removed from the shared outbox queue - otherwise a later, successful reuse of the SAME CosmosDbUnitOfWork would
        // publish it alongside (or instead of) the genuinely new event, breaking atomic outbox semantics.
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var outbox = new CosmosDbEventPublisher(cosmosDb);
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, outbox);

        var pk = NewId();
        var abandonedId = NewId();
        var succeededId = NewId();

        var failResult = await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new TestItem { Id = abandonedId, PartitionKey = pk, Name = "Abandoned" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)));
            return Result.AuthenticationError();
        });

        failResult.IsFailure.Should().BeTrue();
        unitOfWork.Events.IsEmpty.Should().BeTrue();

        // Reuse the SAME unit-of-work for a genuinely successful transaction.
        await unitOfWork.TransactionAsync(async ct =>
        {
            var created = await container.CreateAsync(new TestItem { Id = succeededId, PartitionKey = pk, Name = "Real" }, ct).ConfigureAwait(false);
            unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)));
        });

        var rawContainer = cosmosDb.GetContainer(ContainerId);
        var query = rawContainer.GetItemLinqQueryable<CosmosDbOutboxEvent>().Where(e => e.PartitionKey == pk && e.Id.StartsWith(CosmosDbOutboxEvent.OutboxKeyPrefix));

        var outboxDocs = new List<CosmosDbOutboxEvent>();
        using (var iterator = query.ToFeedIterator())
        {
            while (iterator.HasMoreResults)
                outboxDocs.AddRange(await iterator.ReadNextAsync());
        }

        // Exactly one event - for the successful transaction, not a leaked one from the earlier abandoned/failed transaction.
        outboxDocs.Should().ContainSingle();
        outboxDocs[0].Event.GetProperty("subject").GetString().Should().Be(succeededId);
    }
}
