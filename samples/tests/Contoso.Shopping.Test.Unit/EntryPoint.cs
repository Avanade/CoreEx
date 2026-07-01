namespace Contoso.Shopping.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        // Configure the minimum services required for the execution context and reference data orchestrator; caching will be in-memory for the unit tests.
        builder.Services.AddExecutionContext();
        builder.Services.AddMemoryCache();
        builder.Services.AddReferenceDataOrchestrator<ReferenceDataServiceDecorator>();

        // Configure the products http client.
        builder.AddTypedHttpClient<ProductsHttpClient>("ProductsApi");

        // Reuse the "real" database configured reference data.
        var jdr = JsonDataReader.ParseYaml<Contoso.Shopping.Database.Program>("ref-data.seed.yaml", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.SnakeCase));
        builder.Services.AddSingleton(new ReferenceDataServiceDecorator(jdr));
    }

    public class ReferenceDataServiceDecorator(JsonDataReader jdr) : ReferenceDataService(Mock.Of<IReferenceDataRepository>())
    {
        public override Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
        {
            _ when type == typeof(UnitOfMeasure) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<UnitOfMeasureCollection>("Shopping.$^UnitOfMeasure")!),
            _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
        };
    }
}
