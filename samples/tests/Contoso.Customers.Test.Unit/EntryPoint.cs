using CoreEx.Data.Json;

namespace Contoso.Customers.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        // Configure the minimum services required for the execution context and reference data orchestrator; caching will be in-memory for the unit tests.
        builder.Services.AddExecutionContext();
        builder.Services.AddMemoryCache();
        builder.Services.AddReferenceDataOrchestrator<ReferenceDataServiceDecorator>();

        // Reuse the "test" configured reference data.
        var jdr = JsonDataReader.ParseYaml<Contoso.Customers.Test.Common.TestData>("ref-data.seed.yaml", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.CamelCase));
        builder.Services.AddSingleton(new ReferenceDataServiceDecorator(jdr));

    }

    // TODO: temporary hard-coded stand-in values only, pending the real Cosmos-backed seed data fixture (see CustomersCosmosDb/ReferenceDataRepository) - replace once that seed data exists.
    public class ReferenceDataServiceDecorator(JsonDataReader jdr) : ReferenceDataService(Mock.Of<IReferenceDataRepository>())
    {
        public override Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
        {
            _ when type == typeof(CustomerType) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<CustomerTypeCollection>("ref-data.$^CustomerType")!),
            _ when type == typeof(ContactMethod) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<ContactMethodCollection>("ref-data.$^ContactMethod")!),
            _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
        };
    }
}
