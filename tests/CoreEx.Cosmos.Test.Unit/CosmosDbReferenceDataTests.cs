namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbReferenceDataTests : CosmosTestBase
{
    private const string ContainerId = "ref-data-items";
    private const string OutboxContainerId = "ref-data-outbox-items";
    private const string NoCodeOutboxContainerId = "ref-data-outbox-nocode-items";

    private static readonly TestRefMapper _mapper = new();

    private static async Task EnsureContainerAsync()
    {
        await EnsureUniqueContainerAsync(ContainerId).ConfigureAwait(false);
    }

    // The unique key mirrors the one the reference data containers use (type discriminator + code) so that a duplicate code is rejected by Cosmos DB.
    private static async Task EnsureUniqueContainerAsync(string containerId)
    {
        var properties = new ContainerProperties(containerId, "/partitionKey");
        properties.UniqueKeyPolicy.UniqueKeys.Add(new UniqueKey { Paths = { "/typeDiscriminator", "/code" } });
        await TestDatabase.CreateContainerIfNotExistsAsync(properties).ConfigureAwait(false);
    }

    // Creates within a unit-of-work (so that an outbox event is written to the same container/partition as the business document).
    private static async Task<Result<DataResult<TestRef>>> CreateWithOutboxAsync(CosmosDb cosmosDb, string code, string containerId = OutboxContainerId)
    {
        var container = cosmosDb.Container<TestRefItem>(containerId, o => o.WithTypeDiscriminator());
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb, new CosmosDbEventPublisher(cosmosDb));

        return await unitOfWork.TransactionAsync(async ct =>
        {
            var result = await CosmosDbReferenceData.CreateAsync<string, TestRef, TestRefItem, TestRefMapper>(container, new TestRef { Code = code, Text = code }, _mapper, ct).ConfigureAwait(false);
            if (result.IsSuccess)
                unitOfWork.Events.Add(EventData.CreateEventWith(result.Value.Value, EventAction.Created).WithSource(new Uri("https://unittest/coreex-cosmos", UriKind.Absolute)));

            return result;
        }).ConfigureAwait(false);
    }

    private async Task<CosmosDbContainer<TestRefItem>> GetContainerAsync()
    {
        await EnsureContainerAsync().ConfigureAwait(false);
        return CreateCosmosDb().Container<TestRefItem>(ContainerId, o => o.WithTypeDiscriminator());
    }

    private static Task<Result<DataResult<TestRef>>> CreateAsync(CosmosDbContainer<TestRefItem> container, string code, string? text = null)
        => CosmosDbReferenceData.CreateAsync<string, TestRef, TestRefItem, TestRefMapper>(container, new TestRef { Code = code, Text = text ?? code }, _mapper);

    private static Task<Result<DataResult<TestRef>>> UpdateAsync(CosmosDbContainer<TestRefItem> container, TestRef value)
        => CosmosDbReferenceData.UpdateAsync<string, TestRef, TestRefItem, TestRefMapper>(container, value.Id, value, _mapper);

    private static Task<Result<DataResult<TestRef>>> ActivateAsync(CosmosDbContainer<TestRefItem> container, string id)
        => CosmosDbReferenceData.ActivateAsync<string, TestRef, TestRefItem, TestRefMapper>(container, id, _mapper);

    private static Task<Result<DataResult<TestRef>>> DeactivateAsync(CosmosDbContainer<TestRefItem> container, string id)
        => CosmosDbReferenceData.DeactivateAsync<string, TestRef, TestRefItem, TestRefMapper>(container, id, _mapper);

    private static Task<Result<DataResult>> DeleteAsync(CosmosDbContainer<TestRefItem> container, string id)
        => CosmosDbReferenceData.DeleteAsync<string, TestRef, TestRefItem>(container, id);

    [Test]
    public async Task CreateAsync_GeneratesIdentifier_AndIsInactive()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);
        var code = NewId();

        var result = await CreateAsync(container, code, "Text").ConfigureAwait(false);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasMutated.Should().BeTrue();
        var rd = result.Value.Value;
        rd.Id.Should().NotBeNullOrEmpty();
        rd.Code.Should().Be(code);
        rd.IsInactive.Should().BeTrue();
        rd.ETag.Should().NotBeNullOrEmpty();

        var model = await container.GetAsync(CompositeKey.Create(rd.Id)).ConfigureAwait(false);
        model.Should().NotBeNull();
        model!.Code.Should().Be(code);
        model.IsActive.Should().BeFalse();
        model.TypeDiscriminator.Should().Be(nameof(TestRefItem));
        model.ChangeLog.Should().NotBeNull();
        model.ChangeLog!.CreatedOn.Should().NotBeNull();
    }

    [Test]
    public async Task CreateAsync_WithDuplicateCode_Fails()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);
        var code = NewId();

        (await CreateAsync(container, code).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        var result = await CreateAsync(container, code).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<DuplicateException>();
    }

    [Test]
    public async Task UpdateAsync_ChangesText_ButNotCodeOrActive_AndPreservesChangeLog()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);
        var created = (await CreateAsync(container, NewId(), "Before").ConfigureAwait(false)).Value.Value;
        var activated = (await ActivateAsync(container, created.Id).ConfigureAwait(false)).Value.Value;

        var update = new TestRef { Id = activated.Id, Code = "IGNORED", Text = "After", SortOrder = 7, ETag = activated.ETag };
        var result = await UpdateAsync(container, update).ConfigureAwait(false);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasMutated.Should().BeTrue();
        var rd = result.Value.Value;
        rd.Code.Should().Be(created.Code);
        rd.GetText().Should().Be("After");
        rd.SortOrder.Should().Be(7);
        rd.IsInactive.Should().BeFalse();
        rd.ETag.Should().NotBe(activated.ETag);

        var model = await container.GetAsync(CompositeKey.Create(created.Id)).ConfigureAwait(false);
        model!.ChangeLog!.CreatedOn.Should().NotBeNull();
        model.ChangeLog.UpdatedOn.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAsync_WithStaleETag_Fails_WithConcurrencyError()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);
        var created = (await CreateAsync(container, NewId()).ConfigureAwait(false)).Value.Value;

        var first = await UpdateAsync(container, new TestRef { Id = created.Id, Text = "One", ETag = created.ETag }).ConfigureAwait(false);
        first.IsSuccess.Should().BeTrue();

        var stale = await UpdateAsync(container, new TestRef { Id = created.Id, Text = "Two", ETag = created.ETag }).ConfigureAwait(false);
        stale.IsFailure.Should().BeTrue();
        stale.Error.Should().BeOfType<ConcurrencyException>();
    }

    [Test]
    public async Task UpdateAsync_NotFound_Fails()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);

        var result = await UpdateAsync(container, new TestRef { Id = NewId(), Code = "X", Text = "X" }).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Test]
    public async Task ActivateAsync_And_DeactivateAsync_ToggleState_AndAreIdempotent()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);
        var created = (await CreateAsync(container, NewId()).ConfigureAwait(false)).Value.Value;

        var deactivatedAlready = await DeactivateAsync(container, created.Id).ConfigureAwait(false);
        deactivatedAlready.IsSuccess.Should().BeTrue();
        deactivatedAlready.Value.WasMutated.Should().BeFalse();

        var activated = await ActivateAsync(container, created.Id).ConfigureAwait(false);
        activated.IsSuccess.Should().BeTrue();
        activated.Value.WasMutated.Should().BeTrue();
        activated.Value.Value.IsInactive.Should().BeFalse();
        activated.Value.Value.ETag.Should().NotBe(created.ETag);

        var activatedAgain = await ActivateAsync(container, created.Id).ConfigureAwait(false);
        activatedAgain.Value.WasMutated.Should().BeFalse();

        var deactivated = await DeactivateAsync(container, created.Id).ConfigureAwait(false);
        deactivated.Value.WasMutated.Should().BeTrue();
        deactivated.Value.Value.IsInactive.Should().BeTrue();
    }

    [Test]
    public async Task ActivateAsync_NotFound_Fails()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);

        var result = await ActivateAsync(container, NewId()).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Test]
    public async Task DeleteAsync_Active_Fails()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);
        var created = (await CreateAsync(container, NewId()).ConfigureAwait(false)).Value.Value;
        await ActivateAsync(container, created.Id).ConfigureAwait(false);

        var result = await DeleteAsync(container, created.Id).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        (await container.GetAsync(CompositeKey.Create(created.Id)).ConfigureAwait(false)).Should().NotBeNull();
    }

    [Test]
    public async Task DeleteAsync_Inactive_Succeeds_AndMissingIsNotMutated()
    {
        var container = await GetContainerAsync().ConfigureAwait(false);
        var created = (await CreateAsync(container, NewId()).ConfigureAwait(false)).Value.Value;

        var result = await DeleteAsync(container, created.Id).ConfigureAwait(false);
        result.IsSuccess.Should().BeTrue();
        result.Value.WasMutated.Should().BeTrue();
        (await container.GetAsync(CompositeKey.Create(created.Id)).ConfigureAwait(false)).Should().BeNull();

        var again = await DeleteAsync(container, created.Id).ConfigureAwait(false);
        again.IsSuccess.Should().BeTrue();
        again.Value.WasMutated.Should().BeFalse();
    }

    [Test]
    public async Task CreateAsync_WithinUnitOfWork_PersistsOnCommit()
    {
        await EnsureContainerAsync().ConfigureAwait(false);
        var cosmosDb = CreateCosmosDb();
        var container = cosmosDb.Container<TestRefItem>(ContainerId, o => o.WithTypeDiscriminator());
        var unitOfWork = new CosmosDbUnitOfWork(cosmosDb);

        var result = await unitOfWork.TransactionAsync(_ => CreateAsync(container, NewId())).ConfigureAwait(false);

        result.IsSuccess.Should().BeTrue();
        var model = await container.GetAsync(CompositeKey.Create(result.Value.Value.Id)).ConfigureAwait(false);
        model.Should().NotBeNull();
        model!.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task CreateAsync_WithOutbox_UniqueKeyAndReferenceDataOutboxEvent_AllowsMultipleCreatesAndRejectsDuplicate()
    {
        await EnsureUniqueContainerAsync(OutboxContainerId).ConfigureAwait(false);
        var cosmosDb = new CosmosDb(Client, TestDatabase.Id, new CosmosDbOptions().Container(OutboxContainerId, c => c.WithReferenceDataOutboxEvent()), executionContext: new ExecutionContext { TenantId = "tenant-a" });

        // Each create writes an outbox event into the same container/partition; none may collide with each other (or with the business documents) on the unique key.
        var code = NewId();
        (await CreateWithOutboxAsync(cosmosDb, code).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        (await CreateWithOutboxAsync(cosmosDb, NewId()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        (await CreateWithOutboxAsync(cosmosDb, NewId()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();

        // The duplicate code is atomically rejected by the unique key.
        var duplicate = await CreateWithOutboxAsync(cosmosDb, code).ConfigureAwait(false);
        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Should().BeOfType<DuplicateException>();
    }

    [Test]
    public async Task CreateAsync_WithOutbox_UniqueKeyWithoutReferenceDataOutboxEvent_FailsFastWithClearError()
    {
        // Outbox events lack /typeDiscriminator and /code so would all share the same (null, null) unique key (and the second would fail with a misleading duplicate); the publisher detects this up front.
        await EnsureUniqueContainerAsync(NoCodeOutboxContainerId).ConfigureAwait(false);
        var cosmosDb = new CosmosDb(Client, TestDatabase.Id, new CosmosDbOptions(), executionContext: new ExecutionContext { TenantId = "tenant-a" });

        var act = () => CreateWithOutboxAsync(cosmosDb, NewId(), NoCodeOutboxContainerId);

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Contain("unique key").And.Contain(nameof(CosmosDbContainerOptions.WithReferenceDataOutboxEvent));
    }
}
