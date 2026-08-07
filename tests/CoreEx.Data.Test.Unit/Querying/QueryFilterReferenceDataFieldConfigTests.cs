using CoreEx.Caching;
using CoreEx.Data.Querying;
using CoreEx.RefData;
using CoreEx.RefData.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEx.Data.Test.Unit.Querying;

[TestFixture]
public class QueryFilterReferenceDataFieldConfigTests
{
    private class FakeStatus : ReferenceData<FakeStatus> { }

    private class FakeStatusCollection() : ReferenceDataCollection<FakeStatus>(ReferenceDataSortOrder.Code, StringComparer.OrdinalIgnoreCase) { }

    private class FakeStatusProvider : IReferenceDataProvider
    {
        public IEnumerable<(Type, Type)> Types => [(typeof(FakeStatus), typeof(FakeStatusCollection))];

        public Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default)
            => Task.FromResult<IReferenceDataCollection>(new FakeStatusCollection
            {
                new FakeStatus { Id = "1", Code = "ACTIVE", Text = "Active" },
                new FakeStatus { Id = "2", Code = "INACTIVE", Text = "Inactive", IsInactive = true }
            });
    }

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddExecutionContext(sp => new ExecutionContext { ServiceProvider = sp });
        sc.AddSingleton<IReferenceDataCache>(new ReferenceDataHybridCache(new MemoryOnlyHybridCache()));
        sc.AddScoped<FakeStatusProvider>();
        var sp = sc.BuildServiceProvider();

        var ro = new ReferenceDataOrchestrator(sp, NullLogger<ReferenceDataOrchestrator>.Instance);
        ro.Register<FakeStatusProvider>();
        ReferenceDataOrchestrator.SetCurrent(ro);

        _ = sp.GetRequiredService<ExecutionContext>();
    }

    [TearDown]
    public void TearDown() => ReferenceDataOrchestrator.SetCurrent(null);

    private static QueryArgsConfig CreateConfig(bool mustBeActive = true)
        => QueryArgsConfig.Create().WithFilter(filter => filter.AddReferenceDataField<FakeStatus>("Status", c => c.MustBeActive(mustBeActive)));

    [Test]
    public void Parse_ActiveCode_Success() => TestUtility.AssertFilterSuccess(CreateConfig(), "status eq 'ACTIVE'", "Status == @0", "ACTIVE");

    [Test]
    public void Parse_InactiveCode_MustBeActive_Error()
        => TestUtility.AssertFilterError(CreateConfig(), "status eq 'INACTIVE'", "Field 'status' has a value 'INACTIVE' that is not a valid FakeStatus: Not an active FakeStatus.");

    [Test]
    public void Parse_InactiveCode_MustBeActiveFalse_Success() => TestUtility.AssertFilterSuccess(CreateConfig(mustBeActive: false), "status eq 'INACTIVE'", "Status == @0", "INACTIVE");

    [Test]
    public void Parse_UnknownCode_Error()
        => TestUtility.AssertFilterError(CreateConfig(), "status eq 'UNKNOWN'", "Field 'status' has a value 'UNKNOWN' that is not a valid FakeStatus: Not a valid FakeStatus.");
}
