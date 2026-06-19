using CoreEx.RefData;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using UnitTestEx.Expectations;

namespace CoreEx.Caching.Redis.Test.Unit;

public class ReferenceDataCacheTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void NoInMemory_GoTo_Distributed()
    {
        Test.ReplaceScoped<ReferenceDataProvider>();
        Test.ReplaceScoped<IReferenceDataCache, ReferenceDataHybridCache>();
        Test.ReplaceScoped(sp =>
        {
            var rdo = ActivatorUtilities.CreateInstance<ReferenceDataOrchestrator>(sp);
            rdo.Register<ReferenceDataProvider>();
            return rdo;
        });

        Test.ScopedType<ExecutionContext>(test =>
        {
            test.ExpectLogContains("calling the factory")
                .Run(async _ =>
                {
                    // Clear any existing.
                    await HybridCacheTests.ClearKeyAsync(test.Service.ServiceProvider!, "UnitTest:RefData:CoreEx.Caching.Redis.Test.Unit.ColorCollection");

                    // Should cache for the first time (both in-memory and distributed).
                    var rdo = test.Services.GetRequiredService<ReferenceDataOrchestrator>();
                    var colors = await rdo.GetByTypeAsync<Color>();
                    colors.Should().NotBeNull().And.BeOfType<ColorCollection>().Which.Should().HaveCount(3);
                }).AssertSuccess();
        });

        // Second time should be from cache, so log should indicate that.
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.ExpectLogContains("[MC] memory entry found")
                .Run(async _ =>
                {
                    var rdo = test.Services.GetRequiredService<ReferenceDataOrchestrator>();
                    var colors = await rdo.GetByTypeAsync<Color>();
                    colors.Should().NotBeNull().And.BeOfType<ColorCollection>().Which.Should().HaveCount(3);
                }).AssertSuccess();
        });

        // Clear the in-memory cache and try again, should be from the distributed cache.
        var mc = Test.Services.GetRequiredService<IMemoryCache>();
        ((MemoryCache)mc).Clear();

        // Third time should be from distributed cache, so log should indicate that.
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.ExpectLogContains("[MC] memory entry not found")
                .ExpectLogContains("[DC] distributed entry found")
                .Run(async _ =>
                {
                    var rdo = test.Services.GetRequiredService<ReferenceDataOrchestrator>();
                    var colors = await rdo.GetByTypeAsync<Color>();
                    colors.Should().NotBeNull().And.BeOfType<ColorCollection>().Which.Should().HaveCount(3);
                }).AssertSuccess();
        });
    }

    [Test]
    public void InMemory_Expired_GetFromDistributed()
    {
        Test.ReplaceScoped<ReferenceDataProvider>();
        Test.ReplaceScoped<IReferenceDataCache>(sp =>
        {
            var rdc = ActivatorUtilities.CreateInstance<ReferenceDataHybridCache>(sp);
            rdc.RegisterCacheEntryOptions<ColorCollection>(new HybridCacheEntryOptions { LocalExpiration = TimeSpan.FromSeconds(1), DistributedExpiration = TimeSpan.FromSeconds(1) });
            return rdc;
        });
        Test.ReplaceScoped(sp =>
        {
            var rdo = ActivatorUtilities.CreateInstance<ReferenceDataOrchestrator>(sp);
            rdo.Register<ReferenceDataProvider>();
            return rdo;
        });

        Test.ScopedType<ExecutionContext>(test =>
        {
            test.ExpectLogContains("calling the factory")
                .Run(async _ =>
                {
                    // Clear any existing.
                    await HybridCacheTests.ClearKeyAsync(test.Service.ServiceProvider!, "UnitTest:RefData:CoreEx.Caching.Redis.Test.Unit.ColorCollection");

                    // Should cache for the first time (both in-memory and distributed).
                    var rdo = test.Services.GetRequiredService<ReferenceDataOrchestrator>();
                    var colors = await rdo.GetByTypeAsync<Color>();
                    colors.Should().NotBeNull().And.BeOfType<ColorCollection>().Which.Should().HaveCount(3);
                }).AssertSuccess();
        });

        // Second time should be from cache, so log should indicate that.
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.ExpectLogContains("[MC] memory entry found")
                .Run(async _ =>
                {
                    var rdo = test.Services.GetRequiredService<ReferenceDataOrchestrator>();
                    var colors = await rdo.GetByTypeAsync<Color>();
                    colors.Should().NotBeNull().And.BeOfType<ColorCollection>().Which.Should().HaveCount(3);
                }).AssertSuccess();
        });

        // Wait for the in-memory cache to expire.
        Thread.Sleep(1500);

        // Should be from the distributed cache, so log should indicate that.
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.ExpectLogContains("[MC] memory entry not found")
                .Run(async _ =>
                {
                    var rdo = test.Services.GetRequiredService<ReferenceDataOrchestrator>();
                    var colors = await rdo.GetByTypeAsync<Color>();
                    colors.Should().NotBeNull().And.BeOfType<ColorCollection>().Which.Should().HaveCount(3);
                }).AssertSuccess();
        });
    }

    public class Color : ReferenceData<int, Color> { }

    public class ColorCollection :ReferenceDataCollection<int, Color> { }

    public class ReferenceDataProvider : IReferenceDataProvider
    {
        public IEnumerable<(Type, Type)> Types => [(typeof(Color), typeof(ColorCollection))];

        public Task<RefData.Abstractions.IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default)
            => Task.FromResult<RefData.Abstractions.IReferenceDataCollection>(new ColorCollection { new Color { Id = 1, Code = "R", Text = "Red" }, new Color { Id = 2, Code = "G", Text = "Green" }, new Color { Id = 3, Code = "B", Text = "Blue" } });
    }
}