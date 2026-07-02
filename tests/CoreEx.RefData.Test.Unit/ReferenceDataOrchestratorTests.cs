using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.RefData.Abstractions;
using CoreEx.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CoreEx.RefData.Test.Unit;

public partial class ReferenceDataOrchestratorTests
{
    [ReferenceData]
    internal partial class DummyRefData : ReferenceData<int, DummyRefData> { }

    [ReferenceData]
    internal partial class DummyRefData2 : ReferenceData<DummyRefData2> 
    {
        [ReferenceData<DummyRefData>]
        public partial string? ParentSid { get; set; }

        [ReferenceData<DummyRefData>]
        public partial string? InitParentSid { get; init; }

        [ReferenceData<DummyRefData>]
        public partial string? ReadOnlyParentSid { get; }
    }

    internal class InvalidRefData : ReferenceData<int, InvalidRefData> { }

    internal class DummyRefDataCollection() : ReferenceDataCollection<int, DummyRefData>(ReferenceDataSortOrder.Code, StringComparer.OrdinalIgnoreCase) { }

    internal class DummyRefData2Collection() : ReferenceDataCollection<DummyRefData2>() { }

    private class DummyProvider : IReferenceDataProvider
    {
        public IEnumerable<(Type, Type)> Types =>
        [
            (typeof(DummyRefData), typeof(DummyRefDataCollection)),
            (typeof(DummyRefData2), typeof(DummyRefData2Collection))
        ];

        public Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default)
            => type == typeof(DummyRefData) 
            ?
            Task.FromResult<IReferenceDataCollection>(new DummyRefDataCollection 
            { 
                new DummyRefData { Id = 1, Code = "A", Text = "Alpha" },
                new DummyRefData { Id = 2, Code = "C", Text = "Charlie" },
                new DummyRefData { Id = 3, Code = "B", Text = "Beta" },
                new DummyRefData { Id = 4, Code = "D", Text = "Delta", IsInactive = true }
            }) 
            :
            Task.FromResult<IReferenceDataCollection>(new DummyRefData2Collection
            {
                new DummyRefData2 { Id = "A-1", Code = "A", Text = "Alpha", ParentSid = "P" },
                new DummyRefData2 { Id = "C-3", Code = "C", Text = "Charlie" }
            });
    }

    [Contract]
    internal partial class DummyEntity
    {
        [ReferenceData<DummyRefData>]
        public partial string? RefDataSid { get; set; }
    }

    [Contract]
    internal partial class DummyEntity2
    {
        [ReferenceDataCodeCollection<DummyRefData>]
        public partial List<string>? RefDataSids { get; set; }
    }

    private static ReferenceDataOrchestrator CreateOrchestrator()
    {
        var sc = new ServiceCollection();
        sc.AddExecutionContext(sp => new ExecutionContext { ServiceProvider = sp });
        sc.AddSingleton<IReferenceDataCache>(new ReferenceDataHybridCache(new Caching.MemoryOnlyHybridCache()));
        sc.AddScoped<DummyProvider>();
        var sp = sc.BuildServiceProvider();

        var logger = Mock.Of<ILogger<ReferenceDataOrchestrator>>();
        var ro = new ReferenceDataOrchestrator(sp, logger);
        ro.Register<DummyProvider>();
        return ro;
    }

    [SetUp]
    public void SetUp()
    {
        var orch = CreateOrchestrator();
        ReferenceDataOrchestrator.SetCurrent(orch);
        ReferenceDataOrchestrator.HasCurrent.Should().BeTrue();

        _ = orch.ServiceProvider.GetRequiredService<ExecutionContext>();
    }

    [TearDown]
    public void TearDown()
    {
        ReferenceDataOrchestrator.SetCurrent(null);
        ReferenceDataOrchestrator.HasCurrent.Should().BeFalse();
    }

    [Test]
    public void PrefetchMaxDegreeOfParallelism_GetSet()
    {
        var orch = CreateOrchestrator();
        orch.PrefetchMaxDegreeOfParallelism = 5;
        orch.PrefetchMaxDegreeOfParallelism.Should().Be(5);
    }

    [Test]
    public void Register_And_ContainsType_ContainsName()
    {
        var orch = CreateOrchestrator();
        var type = typeof(DummyRefData);
        orch.ContainsType<DummyRefData>().Should().BeTrue();
        orch.ContainsType(type).Should().BeTrue();
        orch.ContainsName(nameof(DummyRefData)).Should().BeTrue();

        type = typeof(InvalidRefData);
        orch.ContainsType<InvalidRefData>().Should().BeFalse();
        orch.ContainsType(type).Should().BeFalse();
        orch.ContainsName(nameof(InvalidRefData)).Should().BeFalse();
    }

    [Test]
    public void GetAllTypes_ReturnsRegisteredTypes()
    {
        var orch = CreateOrchestrator();
        var types = orch.GetAllTypes();
        types.Should().HaveCount(2);
    }

    [Test]
    public void Indexer_ByType_And_ByName()
    {
        var orch = CreateOrchestrator();
        var byType = orch[typeof(DummyRefData)];
        var byName = orch[nameof(DummyRefData)];
        byType.Should().NotBeNull();
        byName.Should().NotBeNull();
    }

    [Test]
    public void GetByType_And_GetByTypeRequired()
    {
        var orch = CreateOrchestrator();
        var coll = orch.GetByType<DummyRefData>();
        coll.Should().NotBeNull();
        var req = orch.GetByTypeRequired<DummyRefData>();
        req.Should().NotBeNull();
    }

    [Test]
    public void GetByName_And_GetByNameRequired()
    {
        var orch = CreateOrchestrator();
        var coll = orch.GetByName(nameof(DummyRefData));
        coll.Should().NotBeNull();
        var req = orch.GetByNameRequired(nameof(DummyRefData));
        req.Should().NotBeNull();
    }

    [Test]
    public void GetAllTypesInNamespace_ReturnsTypes()
    {
        var types = ReferenceDataOrchestrator.GetAllTypesInNamespace<DummyRefData>();
        types.Should().Contain(typeof(DummyRefData));

        types = ReferenceDataOrchestrator.GetAllTypesInNamespace<Int32>();
        types.Should().BeEmpty();
    }

    [Test]
    public void TryGetByCode_ReturnsValidWhenFound()
    {
        ReferenceDataOrchestrator.TryGetByCode<DummyRefData>("A", out var item).Should().BeTrue();
        item.Should().NotBeNull();
        item.Id.Should().Be(1);
        item.Code.Should().Be("A");
        item.IsActive.Should().BeTrue();
        ((IReferenceData)item).IsValid.Should().BeTrue();
    }

    [Test]
    public void TryGetByCode_ReturnsInvalidWhenNotFound()
    {
        ReferenceDataOrchestrator.TryGetByCode<DummyRefData>("X", out var item).Should().BeFalse();
        item.Should().NotBeNull();
        item.Id.Should().Be(0);
        item.Code.Should().Be("X");
        item.IsActive.Should().BeFalse();
        ((IReferenceData)item).IsValid.Should().BeFalse();
    }

    [Test]
    public void TryGetById_ReturnsValidWhenFound()
    {
        ReferenceDataOrchestrator.TryGetById<DummyRefData>(1, out var item).Should().BeTrue();
        item.Should().NotBeNull();
        item.Id.Should().Be(1);
        item.Code.Should().Be("A");
        item.IsActive.Should().BeTrue();
        ((IReferenceData)item).IsValid.Should().BeTrue();
    }

    [Test]
    public void TryGetById_ReturnsInvalidWhenNotFound()
    {
        ReferenceDataOrchestrator.TryGetById<DummyRefData>(404, out var item).Should().BeFalse();
        item.Should().NotBeNull();
        item.Id.Should().Be(404);
        item.Code.Should().BeNull();
        item.IsActive.Should().BeFalse();
        ((IReferenceData)item).IsValid.Should().BeFalse();
    }

    [Test]
    public void TryGetById_Generic_ReturnsValidWhenFound()
    {
        ReferenceDataOrchestrator.TryGetById<DummyRefData, int>(1, out var item).Should().BeTrue();
        item.Should().NotBeNull();
        item.Id.Should().Be(1);
        item.Code.Should().Be("A");
        item.IsActive.Should().BeTrue();
        ((IReferenceData)item).IsValid.Should().BeTrue();
    }

    [Test]
    public void TryGetById_Generic_ReturnsInvalidWhenNotFound()
    {
        ReferenceDataOrchestrator.TryGetById<DummyRefData, int>(404, out var item).Should().BeFalse();
        item.Should().NotBeNull();
        item.Id.Should().Be(404);
        item.Code.Should().BeNull();
        item.IsActive.Should().BeFalse();
        ((IReferenceData)item).IsValid.Should().BeFalse();
    }

    [Test]
    public async Task GetWithFilterAsync_All()
    {
        var coll = await ReferenceDataOrchestrator.Current.GetWithFilterAsync<DummyRefData>();
        coll.Should().NotBeNull();
        coll.Count().Should().Be(3);
    }

    [Test]
    public async Task GetWithFilterAsync_Codes()
    {
        var coll = await ReferenceDataOrchestrator.Current.GetWithFilterAsync<DummyRefData>(["A","C","Z"]);
        coll.Should().NotBeNull();
        coll.Count().Should().Be(2);
        coll.Select(x => x.Code).Should().BeEquivalentTo(["A", "C"]);
    }

    [Test]
    public async Task GetWithFilterAsync_Text_Wildcard()
    {
        var coll = await ReferenceDataOrchestrator.Current.GetWithFilterAsync<DummyRefData>(null, "*a");
        coll.Should().NotBeNull();
        coll.Count().Should().Be(2);
        coll.Select(x => x.Code).Should().BeEquivalentTo(["A", "B"]);
    }

    [Test]
    public async Task GetWithFilterAsync_Text_Wildcard_And_Inactive()
    {
        var coll = await ReferenceDataOrchestrator.Current.GetWithFilterAsync<DummyRefData>(null, "*a", true);
        coll.Should().NotBeNull();
        coll.Count().Should().Be(3);
        coll.Select(x => x.Code).Should().BeEquivalentTo(["A", "B", "D"]);
    }

    // Entity source generation tests.

    [Test]
    public void Entity_ReferenceData_Property()
    {
        var entity = new DummyEntity { RefDataSid = "A" };
        entity.RefDataSid.Should().Be("A");
        entity.RefData.Should().NotBeNull();
        entity.RefData.Code.Should().Be("A");
        entity.RefData.Id.Should().Be(1);

        entity.RefDataSid = DummyRefData.TryGetByCode("C", out var item) ? item : item;
        entity.RefDataSid.Should().Be("C");

        entity.RefDataSid = DummyRefData.TryGetByCode("Z", out item) ? item : item;
        entity.RefDataSid.Should().Be("Z");
        entity.RefData.Should().NotBeNull();
        entity.RefData.Code.Should().Be("Z");
        entity.RefData.Id.Should().Be(0);
        entity.RefData.IsActive.Should().BeFalse();
        ((IReferenceData)entity.RefData).IsValid.Should().BeFalse();

        entity.RefDataSid = null;
        entity.RefDataSid.Should().BeNull();
        entity.RefData.Should().BeNull();
    }

    [Test]
    public void Entity_ReferenceData_Text()
    {
        var entity = new DummyEntity { RefDataSid = "A" };
        entity.RefDataText.Should().BeNull();

        ExecutionContext.Current.IncludeRelatedText = true;
        entity.RefDataText.Should().Be("Alpha");
    }

    // Explicit casting magic!

    [Test]
    public void Explicit_Cast_From_Id()
    {
        var rd = (DummyRefData)1;
        rd.Id.Should().Be(1);
        rd.Code.Should().Be("A");
        rd.IsActive.Should().BeTrue();

        rd = (DummyRefData)404;
        rd.Id.Should().Be(404);
        rd.Code.Should().BeNull();
        rd.IsActive.Should().BeFalse();
        ((IReferenceData)rd).IsValid.Should().BeFalse();
    }

    [Test]
    public void Explict_Cast_From_Code()
    {
        var rd = (DummyRefData)"A";
        rd.Id.Should().Be(1);
        rd.Code.Should().Be("A");
        rd.IsActive.Should().BeTrue();

        rd = (DummyRefData)"G";
        rd.Id.Should().Be(0);
        rd.Code.Should().Be("G");
        rd.IsActive.Should().BeFalse();
        ((IReferenceData)rd).IsValid.Should().BeFalse();
    }

    [Test]
    public void Implicit_Cast_From_Code()
    {
        DummyRefData? rd = "A";
        rd.Id.Should().Be(1);
        rd.Code.Should().Be("A");
        rd.IsActive.Should().BeTrue();

        rd = "G";
        rd.Id.Should().Be(0);
        rd.Code.Should().Be("G");
        rd.IsActive.Should().BeFalse();
        ((IReferenceData)rd).IsValid.Should().BeFalse();

        rd = (string?)null;
        rd.Should().BeNull();
    }

    [Test]
    public void Explicit_Cast_From_Id_String()
    {
        var rd = (DummyRefData2)"A";
        rd.Id.Should().Be("A-1");
        rd.Code.Should().Be("A");
        rd.IsActive.Should().BeTrue();

        rd = (DummyRefData2)"ABC";
        rd.Id.Should().BeNull();
        rd.Code.Should().Be("ABC");
        rd.IsActive.Should().BeFalse();
        ((IReferenceData)rd).IsValid.Should().BeFalse();
    }


    [Test]
    public void ReferenceDataBase_TryGetBy()
    {
        DummyRefData.TryGetById(1, out var rd).Should().BeTrue();
        rd.Id.Should().Be(1);
        rd.Code.Should().Be("A");

        DummyRefData.TryGetByCode("A", out rd).Should().BeTrue();
        rd.Id.Should().Be(1);
        rd.Code.Should().Be("A");

        DummyRefData2.TryGetById("C-3", out var rd2).Should().BeTrue();
        rd2.Id.Should().Be("C-3");
        rd2.Code.Should().Be("C");

        DummyRefData2.TryGetByCode("C", out rd2).Should().BeTrue();
        rd2.Id.Should().Be("C-3");
        rd2.Code.Should().Be("C");
    }

    [Test]
    public void ReferenceDataCollection_Serialization_RoundTrip()
    {
        var orch = CreateOrchestrator();
        var coll = orch[typeof(DummyRefData)];

        var json = System.Text.Json.JsonSerializer.Serialize<DummyRefDataCollection>((DummyRefDataCollection)coll!, JsonDefaults.SerializerOptions);

        var deserialColl = System.Text.Json.JsonSerializer.Deserialize<DummyRefDataCollection>(json, JsonDefaults.SerializerOptions);
        var json2 = System.Text.Json.JsonSerializer.Serialize(deserialColl, JsonDefaults.SerializerOptions);

        json.Should().Be(json2);
    }

    [Test]
    public void ReferenceDataCollection_Serialization_RoundTrip2()
    {
        var orch = CreateOrchestrator();
        var coll = orch[typeof(DummyRefData2)];

        var json = System.Text.Json.JsonSerializer.Serialize(coll!, JsonDefaults.SerializerOptions);

        var deserialColl = System.Text.Json.JsonSerializer.Deserialize<DummyRefData2Collection>(json, JsonDefaults.SerializerOptions);

        // ReferenceDataCollectionCore<TId, TRef> is backed by a ConcurrentDictionary and its remarks explicitly state there is
        // no implied enumeration order. Comparing the raw serialized JSON strings is therefore unreliable: string ids hash
        // differently per process (string hash randomization), so the enumerated/serialized order flips between runs. Assert
        // via GetItems(), which returns a stable, sorted List<TRef>, rather than BeEquivalentTo on the collection directly -
        // the latter enumerates via ICollection<TRef>.CopyTo, which this collection explicitly does not support.
        deserialColl!.GetItems().Should().BeEquivalentTo(((DummyRefData2Collection)coll!).GetItems());

        deserialColl!.Should().Contain(x => x.ParentSid == "P");
    }
}
