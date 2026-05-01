namespace Contoso.Products.Application;

[ScopedService]
public class ReferenceDataService(IReferenceDataRepository repository) : IReferenceDataProvider
{
    private readonly IReferenceDataRepository _repository = repository.ThrowIfNull();

    public IEnumerable<(Type, Type)> Types =>
    [
        (typeof(Category), typeof(CategoryCollection)),
        (typeof(SubCategory), typeof(SubCategoryCollection)),
        (typeof(UnitOfMeasure), typeof(UnitOfMeasureCollection)),
        (typeof(Brand), typeof(BrandCollection)),
        (typeof(MovementKind), typeof(MovementKindCollection)),
        (typeof(MovementStatus), typeof(MovementStatusCollection)),
    ];

    public async Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
    {
        _ when type == typeof(Category) => await _repository.GetAllCategoriesAsync().ConfigureAwait(false),
        _ when type == typeof(SubCategory) => await _repository.GetAllSubCategoriesAsync().ConfigureAwait(false),
        _ when type == typeof(UnitOfMeasure) => await _repository.GetAllUnitsOfMeasureAsync().ConfigureAwait(false),
        _ when type == typeof(Brand) => await _repository.GetAllBrandsAsync().ConfigureAwait(false),
        _ when type == typeof(MovementKind) => await _repository.GetAllMovementKindsAsync().ConfigureAwait(false),
        _ when type == typeof(MovementStatus) => await _repository.GetAllMovementStatusesAsync().ConfigureAwait(false),
        _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
    };
}