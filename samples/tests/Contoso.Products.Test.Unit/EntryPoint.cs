namespace Contoso.Products.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        // Configure the minimum services required for the execution context and reference data orchestrator; caching will be in-memory for the unit tests.
        builder.Services.AddExecutionContext();
        builder.Services.AddMemoryCache();
        builder.Services.AddReferenceDataOrchestrator<ReferenceDataServiceDecorator>();

        // Reuse the "real" database configured reference data.
        var jdr = JsonDataReader.ParseYaml<Contoso.Products.Database.Program>("ref-data.seed.yaml", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.SnakeCase));
        builder.Services.AddSingleton(new ReferenceDataServiceDecorator(jdr));
    }

    public class ReferenceDataServiceDecorator(JsonDataReader jdr) : ReferenceDataService(Mock.Of<IReferenceDataRepository>())
    {
        public override Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
        {
            _ when type == typeof(Category) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<CategoryCollection>("products.$^category")!.ExtendForTesting([new Category { Id = Runtime.NewId(), Code = "X", IsInactive = false }])),
            _ when type == typeof(SubCategory) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<SubCategoryCollection>("products.$^sub_category")!),
            _ when type == typeof(UnitOfMeasure) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<UnitOfMeasureCollection>("products.$^unit_of_measure")!),
            _ when type == typeof(Brand) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<BrandCollection>("products.$^brand")!),
            _ when type == typeof(MovementKind) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<MovementKindCollection>("products.$^movement_kind")!),
            _ when type == typeof(MovementStatus) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<MovementStatusCollection>("products.$^movement_status")!),
            _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
        };
    }
}