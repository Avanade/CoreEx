namespace Contoso.Products.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        // Configure the minimum services required for the execution context and reference data orchestrator; caching will be in-memory for the unit tests.
        builder.Services.AddExecutionContext();
        builder.Services.AddMemoryCache();
        builder.Services.AddReferenceDataOrchestrator<ReferenceDataProvider>();

        // Reuse the "real" database configured reference data.
        var jdr = JsonDataReader.ParseYaml<Contoso.Products.Database.Program>("ref-data.yaml", JsonDataReaderOptions.CreateForReferenceData());
        builder.Services.AddSingleton(new ReferenceDataProvider(jdr));
    }

    public class ReferenceDataProvider(JsonDataReader jdr) : IReferenceDataProvider
    {
        public IEnumerable<(Type, Type)> Types =>
        [
            (typeof(Category), typeof(CategoryCollection)),
            (typeof(SubCategory), typeof(SubCategoryCollection)),
            (typeof(UnitOfMeasure), typeof(UnitOfMeasureCollection)),
            (typeof(Brand), typeof(BrandCollection)),
            (typeof(MovementKind), typeof(MovementKindCollection)),
            (typeof(MovementStatus), typeof(MovementStatusCollection)),
        ];

        public Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
        {
            _ when type == typeof(Category) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<CategoryCollection>("products.$^category")!),
            _ when type == typeof(SubCategory) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<SubCategoryCollection>("products.$^sub_category")!),
            _ when type == typeof(UnitOfMeasure) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<UnitOfMeasureCollection>("products.$^unit_of_measure")!),
            _ when type == typeof(Brand) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<BrandCollection>("products.$^brand")!),
            _ when type == typeof(MovementKind) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<MovementKindCollection>("products.$^movement_kind")!),
            _ when type == typeof(MovementStatus) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<MovementStatusCollection>("products.$^movement_status")!),
            _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
        };
    }
}