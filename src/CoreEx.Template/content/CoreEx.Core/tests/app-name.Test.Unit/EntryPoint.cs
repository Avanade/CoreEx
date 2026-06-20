namespace app-name.Test.Unit;

/// <summary>Provides the host entry point for the unit test configuration.</summary>
public class EntryPoint
{
    /// <summary>Configure the minimum services required for the unit tests.</summary>
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        // Configure the minimum services required for the unit tests.
        builder.Services.AddExecutionContext();
        builder.Services.AddMemoryCache();
// #if refdata-enabled
        builder.Services.AddReferenceDataOrchestrator<ReferenceDataServiceDecorator>();

        // Reuse the "real" database configured reference data.
        var jdr = JsonDataReader.ParseYaml<app-name.Database.Program>("ref-data.seed.yaml", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.SnakeCase));
        builder.Services.AddSingleton(new ReferenceDataServiceDecorator(jdr));
// #endif
    }
// #if refdata-enabled

    /// <summary>Provides a decorator for the <see cref="ReferenceDataService"/> to use JSON data for unit tests.</summary>
    public class ReferenceDataServiceDecorator(JsonDataReader jdr) : ReferenceDataService(Mock.Of<IReferenceDataRepository>())
    {
        public override Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
        {
            // Add a case per reference data type a validator under test needs. '{RefData}' is the contract type and
            // '{RefData}Collection' its collection; 'schema.$^table' is the appropriately-cased key into the seed data.
            //_ when type == typeof(ref-data-name) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<ref-data-nameCollection>("schema.$^table")!),
            _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
        };
    }
// #endif
}