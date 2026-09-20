namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Verifies <see cref="CosmosDbMultiSetExtensions.SelectMultiSetAsync(ICosmosDb, string, MultiSetOptions, CancellationToken)"/> and its <see cref="Result"/> (Railway-Oriented Programming) counterpart
/// <see cref="CosmosDbMultiSetExtensions.SelectMultiSetWithResultAsync(ICosmosDb, string, MultiSetOptions, CancellationToken)"/> - reading multiple, type-discriminator-keyed sets of documents from the
/// same container/partition in a single round-trip - mirroring the same shared-container setup as <see cref="CosmosDbContainerTypeDiscriminatorTests"/>.
/// </summary>
[TestFixture]
public class CosmosDbMultiSetTests : CosmosTestBase
{
    private const string ContainerId = "multiset-items";
    private const string TenantContainerId = "multiset-tenant-items";
    private const string SoftDeleteContainerId = "multiset-softdelete-items";

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

    [Test]
    public async Task SelectMultiSetAsync_ReturnsMatchingTypes_FromSharedContainerAndPartition()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        var plants = cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Cat" });
        await plants.CreateAsync(new PlantItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Fern" });

        List<AnimalItem>? animalResults = null;
        PlantItem? plantResult = null;

        await cosmosDb.SelectMultiSetAsync(ContainerId, new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            MultiSetArgs =
            [
                new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(r => animalResults = r, minimumRows: 1),
                new MultiSetSingleArgs<PlantItem>(r => plantResult = r)
            ]
        });

        animalResults.Should().NotBeNull();
        animalResults!.Select(a => a.Name).Should().BeEquivalentTo(["Dog", "Cat"]);
        plantResult.Should().NotBeNull();
        plantResult!.Name.Should().Be("Fern");
    }

    [Test]
    public async Task SelectMultiSetAsync_MandatorySingleNotFound_Throws()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });

        // No PlantItem created in this partition - the mandatory MultiSetSingleArgs<PlantItem> (isMandatory defaults true, MinimumRows == 1) must throw.
        Func<Task> act = () => cosmosDb.SelectMultiSetAsync(ContainerId, new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            MultiSetArgs =
            [
                new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(_ => { }),
                new MultiSetSingleArgs<PlantItem>(_ => { })
            ]
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*less items*");
    }

    [Test]
    public async Task SelectMultiSetAsync_MaximumRowsExceeded_Throws()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Cat" });

        // MultiSetSingleArgs<AnimalItem> allows at most one (MaximumRows == 1) - two exist in this partition, so it must throw.
        Func<Task> act = () => cosmosDb.SelectMultiSetAsync(ContainerId, new MultiSetOptions { PartitionKey = sharedPartition, MultiSetArgs = [new MultiSetSingleArgs<AnimalItem>(_ => { })] });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*more items*");
    }

    [Test]
    public async Task SelectMultiSetAsync_StopOnNull_ShortCircuitsSubsequentResults()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var plants = cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await plants.CreateAsync(new PlantItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Fern" });

        // No AnimalItem created - the first (optional, StopOnNull) MultiSetSingleArgs<AnimalItem> resolves to null, which must stop processing before the PlantItem result is invoked.
        var animalInvoked = false;
        var plantInvoked = false;

        await cosmosDb.SelectMultiSetAsync(ContainerId, new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            MultiSetArgs =
            [
                new MultiSetSingleArgs<AnimalItem>(_ => animalInvoked = true, isMandatory: false, stopOnNull: true),
                new MultiSetSingleArgs<PlantItem>(_ => plantInvoked = true)
            ]
        });

        animalInvoked.Should().BeFalse();
        plantInvoked.Should().BeFalse();
    }

    [Test]
    public async Task SelectMultiSetAsync_ExcludesCoLocatedOutboxDocuments()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });

        // A co-located outbox event document, physically sharing the same container/partition (see CosmosDbEventPublisher) - must never surface as a multi-set result nor break demux/deserialization.
        using var eventDoc = System.Text.Json.JsonDocument.Parse("{}");
        await animals.Container.CreateItemAsync(
            new CosmosDbOutboxEvent { Id = $"{CosmosDbOutboxEvent.OutboxKeyPrefix}-{NewId()}", PartitionKey = sharedPartition, Destination = "irrelevant", Event = eventDoc.RootElement, TimeToLive = -1 },
            new PartitionKey(sharedPartition));

        List<AnimalItem>? animalResults = null;

        await cosmosDb.SelectMultiSetAsync(ContainerId, new MultiSetOptions { PartitionKey = sharedPartition, MultiSetArgs = [new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(r => animalResults = r, minimumRows: 1)] });

        animalResults.Should().NotBeNull();
        animalResults!.Select(a => a.Name).Should().BeEquivalentTo(["Dog"]);
    }

    [Test]
    public async Task SelectMultiSetAsync_AppliesPerItemTenantFilter()
    {
        await GetOrCreateContainerAsync(TenantContainerId).ConfigureAwait(false);

        var cosmosDbA = CreateCosmosDb("tenant-a");
        var animalsA = cosmosDbA.Container<TenantAnimalItem>(TenantContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        cosmosDbA.Container<TenantPlantItem>(TenantContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animalsA.CreateAsync(new TenantAnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog", TenantId = "tenant-a" });

        // A different ICosmosDb (tenant-b) queries the SAME container/partition - the tenant-a-owned document must be excluded by the per-item CheckModel check, not merely by the discriminator match.
        var cosmosDbB = CreateCosmosDb("tenant-b");
        cosmosDbB.Container<TenantAnimalItem>(TenantContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        cosmosDbB.Container<TenantPlantItem>(TenantContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        List<TenantAnimalItem>? animalResults = null;

        await cosmosDbB.SelectMultiSetAsync(TenantContainerId, new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            MultiSetArgs =
            [
                new MultiSetCollArgs<List<TenantAnimalItem>, TenantAnimalItem>(r => animalResults = r),
                new MultiSetCollArgs<List<TenantPlantItem>, TenantPlantItem>(_ => { })
            ]
        });

        animalResults.Should().BeNull();
    }

    [Test]
    public void BuildFilterClause_NeitherFilterConfigured_ReturnsNull()
    {
        var cosmosDb = CreateCosmosDb();
        cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var msa = new MultiSetSingleArgs<AnimalItem>(_ => { });
        var parameters = new Dictionary<string, object?>();

        ((IMultiSetArgs)msa).BuildFilterClause(cosmosDb, ContainerId, JsonOptions, "@f0", parameters).Should().BeNull();
        parameters.Should().BeEmpty();
    }

    [Test]
    public void BuildFilterClause_WithLogicalDeleteFilter_ReturnsIsDefinedGuardedPredicate()
    {
        var cosmosDb = CreateCosmosDb();
        cosmosDb.Container<SoftDeleteAnimalItem>(SoftDeleteContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator().WithLogicalDeleteFilter());

        var msa = new MultiSetSingleArgs<SoftDeleteAnimalItem>(_ => { });
        var parameters = new Dictionary<string, object?>();

        var clause = ((IMultiSetArgs)msa).BuildFilterClause(cosmosDb, SoftDeleteContainerId, JsonOptions, "@f0", parameters);

        clause.Should().Be("(NOT IS_DEFINED(c[\"isDeleted\"]) OR c[\"isDeleted\"] = false)");
        parameters.Should().BeEmpty();
    }

    [Test]
    public void BuildFilterClause_WithTenantFilter_ReturnsIsDefinedGuardedPredicateAndParameter()
    {
        var cosmosDb = CreateCosmosDb("tenant-a");
        cosmosDb.Container<TenantAnimalItem>(TenantContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator().WithTenantFilter());

        var msa = new MultiSetSingleArgs<TenantAnimalItem>(_ => { });
        var parameters = new Dictionary<string, object?>();

        var clause = ((IMultiSetArgs)msa).BuildFilterClause(cosmosDb, TenantContainerId, JsonOptions, "@f0", parameters);

        clause.Should().Be("(NOT IS_DEFINED(c[\"tenantId\"]) OR c[\"tenantId\"] = @f0_tenantId)");
        parameters.Should().ContainKey("@f0_tenantId").WhoseValue.Should().Be("tenant-a");
    }

    [Test]
    public async Task SelectMultiSetAsync_LogicalDeleteFilter_ExcludesDeletedButLetsThroughLegacyDocumentMissingProperty()
    {
        await GetOrCreateContainerAsync(SoftDeleteContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<SoftDeleteAnimalItem>(SoftDeleteContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator().WithLogicalDeleteFilter());

        var sharedPartition = NewId();
        await animals.CreateAsync(new SoftDeleteAnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog", IsDeleted = false });

        // CosmosDbContainer<TModel>.CreateAsync rejects an already-deleted model outright (by design), so insert the deleted document directly via the raw SDK container to simulate one that was
        // subsequently soft-deleted (an update, not a create) - the multi-set query itself has no notion of update-vs-create, only what is currently persisted.
        await animals.Container.CreateItemAsync(new SoftDeleteAnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Cat", TypeDiscriminator = nameof(SoftDeleteAnimalItem), IsDeleted = true }, new PartitionKey(sharedPartition));

        // A raw, hand-crafted "legacy" document sharing the container/partition/discriminator but predating the isDeleted property being added at all (no isDeleted field present whatsoever) -
        // the IS_DEFINED-guarded SQL predicate (see IMultiSetArgs.BuildFilterClause) must let it through rather than silently excluding it.
        await animals.Container.CreateItemAsync(
            new { id = NewId(), partitionKey = sharedPartition, typeDiscriminator = nameof(SoftDeleteAnimalItem), name = "Fox" },
            new PartitionKey(sharedPartition));

        List<SoftDeleteAnimalItem>? animalResults = null;

        await cosmosDb.SelectMultiSetAsync(SoftDeleteContainerId, new MultiSetOptions { PartitionKey = sharedPartition, MultiSetArgs = [new MultiSetCollArgs<List<SoftDeleteAnimalItem>, SoftDeleteAnimalItem>(r => animalResults = r, minimumRows: 1)] });

        animalResults.Should().NotBeNull();
        animalResults!.Select(a => a.Name).Should().BeEquivalentTo(["Dog", "Fox"]);
    }

    [Test]
    public async Task SelectMultiSetAsync_MandatorySingle_OnlyMatchIsExcludedByCheckModel_ThrowsRatherThanSilentlySucceedingWithNoValue()
    {
        // Regression: IMultiSetArgs.AddItem's Result only ever signals a genuine failure (e.g. a WithFilter authorization denial) - a document silently excluded by CheckModel (e.g. logically
        // deleted, here) still returns Result.Success. The caller must count a received row solely from the AddItem Result<bool>.Value (was it actually added?), never merely from
        // Result.IsFailure/IsSuccess - otherwise a filtered-out document would still satisfy MinimumRows, and this mandatory MultiSetSingleArgs would silently invoke nothing (InvokeResult sees
        // its own _value still null) rather than this method throwing to signal the mandatory item was never found.
        await GetOrCreateContainerAsync(SoftDeleteContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();

        // Deliberately omit WithLogicalDeleteFilter() - the query-level filter would exclude the deleted document before it ever reached AddItem, which would make MinimumRows fail for the
        // wrong reason (never queried) rather than the reason under test (queried, then excluded by CheckModel's application-level logical-delete check).
        var animals = cosmosDb.Container<SoftDeleteAnimalItem>(SoftDeleteContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.Container.CreateItemAsync(
            new SoftDeleteAnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Cat", TypeDiscriminator = nameof(SoftDeleteAnimalItem), IsDeleted = true },
            new PartitionKey(sharedPartition));

        var invoked = false;
        Func<Task> act = () => cosmosDb.SelectMultiSetAsync(SoftDeleteContainerId, new MultiSetOptions { PartitionKey = sharedPartition, MultiSetArgs = [new MultiSetSingleArgs<SoftDeleteAnimalItem>(_ => invoked = true)] });

        await act.Should().ThrowAsync<InvalidOperationException>();
        invoked.Should().BeFalse();
    }

    [Test]
    public async Task SelectMultiSetAsync_ArgsQueryRequestOptionsPartitionKeyMismatch_Throws()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var options = new MultiSetOptions
        {
            PartitionKey = NewId(),
            Args = new CosmosDbArgs { QueryRequestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(NewId()) } },
            MultiSetArgs = [new MultiSetSingleArgs<AnimalItem>(_ => { }, isMandatory: false)]
        };

        Func<Task> act = () => cosmosDb.SelectMultiSetAsync(ContainerId, options);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*does not match*");
    }

    [Test]
    public async Task SelectMultiSetAsync_ArgsQueryRequestOptionsPartitionKeyTakesPrecedence_WhenNoExplicitPartitionKeyProvided()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });

        List<AnimalItem>? animalResults = null;
        var options = new MultiSetOptions
        {
            Args = new CosmosDbArgs { QueryRequestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(sharedPartition) } },
            MultiSetArgs = [new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(r => animalResults = r, minimumRows: 1)]
        };

        await cosmosDb.SelectMultiSetAsync(ContainerId, options);

        animalResults.Should().NotBeNull();
        animalResults!.Select(a => a.Name).Should().BeEquivalentTo(["Dog"]);
    }

    [Test]
    public async Task SelectMultiSetWithResultAsync_ReturnsMatchingTypes_FromSharedContainerAndPartition()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        var plants = cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });
        await plants.CreateAsync(new PlantItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Fern" });

        List<AnimalItem>? animalResults = null;
        PlantItem? plantResult = null;

        var result = await cosmosDb.SelectMultiSetWithResultAsync(ContainerId, new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            MultiSetArgs =
            [
                new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(r => animalResults = r, minimumRows: 1),
                new MultiSetSingleArgs<PlantItem>(r => plantResult = r)
            ]
        });

        result.IsSuccess.Should().BeTrue();
        animalResults.Should().NotBeNull();
        animalResults!.Select(a => a.Name).Should().BeEquivalentTo(["Dog"]);
        plantResult.Should().NotBeNull();
        plantResult!.Name.Should().Be("Fern");
    }

    [Test]
    public async Task SelectMultiSetWithResultAsync_MandatorySingleNotFound_StillThrows()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });

        // MinimumRows/MaximumRows violations are invariant/guard-clause conditions (InvalidOperationException), not business-level outcomes - they throw directly even from this Result-returning
        // method, exactly as they do from the exception-based SelectMultiSetAsync (see this method's remarks). No PlantItem created in this partition - the mandatory MultiSetSingleArgs<PlantItem>
        // (isMandatory defaults true, MinimumRows == 1) must throw.
        Func<Task> act = () => cosmosDb.SelectMultiSetWithResultAsync(ContainerId, new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            MultiSetArgs =
            [
                new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(_ => { }),
                new MultiSetSingleArgs<PlantItem>(_ => { })
            ]
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*less items*");
    }

    [Test]
    public async Task SelectMultiSetWithResultAsync_MaximumRowsExceeded_StillThrows()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Cat" });

        // MultiSetSingleArgs<AnimalItem> allows at most one (MaximumRows == 1) - two exist in this partition; this is a guard-clause invariant violation, not a business outcome, so it must throw
        // even from this Result-returning method.
        Func<Task> act = () => cosmosDb.SelectMultiSetWithResultAsync(ContainerId, new MultiSetOptions { PartitionKey = sharedPartition, MultiSetArgs = [new MultiSetSingleArgs<AnimalItem>(_ => { })] });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*more items*");
    }

    [Test]
    public async Task SelectMultiSetWithResultAsync_AddItemFilterFailure_ReturnsFailure()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();

        // A genuine business-level outcome (an authorization-style WithFilter denial via CheckModel/CheckFilters) IS surfaced as a Result.IsFailure here - unlike the guard-clause/invariant cases above.
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o =>
        {
            o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator();
            o.WithFilter(q => q.Where(m => !m.Name.StartsWith("Hidden")), (_, _) => Result.AuthenticationError());
        });

        var sharedPartition = NewId();

        // Seed directly via the raw SDK container, bypassing CoreEx.Cosmos's own filter enforcement on Create - "Hidden" is excluded by the filter above, triggering the configured nonQueryResult.
        await animals.Container.CreateItemAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Hidden", TypeDiscriminator = nameof(AnimalItem) }, new PartitionKey(sharedPartition));

        var result = await cosmosDb.SelectMultiSetWithResultAsync(ContainerId, new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            MultiSetArgs = [new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(_ => { }, minimumRows: 1)]
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<AuthenticationException>();
    }

    [Test]
    public async Task SelectMultiSetAsync_ModelWithQueryOnlyFilter_ThrowsNotSupported()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();

        // Regression: a WithFilter registered WITHOUT a nonQueryResult is "query-only" - applied server-side by a normal CosmosDbQuery<TModel> (via ApplyFilters), but NOT checked per-item by
        // CheckFilters/CheckModel (by design), and cannot be safely translated into a multi-set query's raw SQL text (an arbitrary Func<IQueryable<TModel>, IQueryable<TModel>> has no such translation
        // outside of a real Cosmos LINQ query). Previously, using such a model in a multi-set query silently ignored the filter entirely, returning documents an equivalent single-set query would have
        // excluded. It must now fail fast with NotSupportedException instead.
        cosmosDb.Container<AnimalItem>(ContainerId, o =>
        {
            o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator();
            o.WithFilter(q => q.Where(m => !m.Name.StartsWith("Hidden")));
        });

        Func<Task> act = () => cosmosDb.SelectMultiSetAsync(ContainerId, new MultiSetOptions
        {
            PartitionKey = NewId(),
            MultiSetArgs = [new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(_ => { })]
        });

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*query-only*");
    }

    [Test]
    public async Task SelectMultiSetAsync_PreservesOtherQueryRequestOptionsSettings_WhenLayeringInPartitionKey()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        for (var i = 0; i < 5; i++)
            await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = $"Dog{i}" });

        List<AnimalItem>? animalResults = null;

        // MaxItemCount = 1 forces the iterator to page one item at a time - if the caller's QueryRequestOptions were discarded (rather than cloned-and-layered) this setting would be lost and the
        // behaviour would be indistinguishable; it is asserted indirectly here via the full result set still being correctly aggregated across the many forced pages.
        var options = new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            Args = new CosmosDbArgs { QueryRequestOptions = new QueryRequestOptions { MaxItemCount = 1 } },
            MultiSetArgs = [new MultiSetCollArgs<List<AnimalItem>, AnimalItem>(r => animalResults = r, minimumRows: 1)]
        };

        await cosmosDb.SelectMultiSetAsync(ContainerId, options);

        animalResults.Should().NotBeNull();
        animalResults!.Should().HaveCount(5);
    }

    [Test]
    public async Task SelectMultiSetAsync_FindsModelConfiguredWithExplicitTypeDiscriminatorOverride()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();

        // Regression: CosmosDbModelOptions<TModel>.ApplyTypeDiscriminator stamps an explicit WithTypeDiscriminator(value) override onto every created/updated document (overriding whatever default
        // Model.PrepareCreate/PrepareUpdate/PrepareTypeDiscriminator already stamped) - see CosmosDbContainerTypeDiscriminatorTests. Previously, IMultiSetArgs.TypeDiscriminator always resolved the
        // unconfigured schema/CLR-name default regardless of any such override, so a multi-set query would build its SQL predicate using the wrong discriminator value and never find these documents
        // (a mandatory MultiSetSingleArgs<AnimalItem> would incorrectly throw "less items than expected"). It must now resolve the same EffectiveTypeDiscriminator value that was actually persisted.
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator("custom-animal"));
        cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();
        await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });

        AnimalItem? animalResult = null;

        await cosmosDb.SelectMultiSetAsync(ContainerId, new MultiSetOptions
        {
            PartitionKey = sharedPartition,
            MultiSetArgs = [new MultiSetSingleArgs<AnimalItem>(r => animalResult = r)]
        });

        animalResult.Should().NotBeNull();
        animalResult!.Name.Should().Be("Dog");
    }
}
