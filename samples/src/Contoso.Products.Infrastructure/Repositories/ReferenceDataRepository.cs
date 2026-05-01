namespace Contoso.Products.Infrastructure.Repositories;

[ScopedService<IReferenceDataRepository>]
public class ReferenceDataRepository(ProductsEfDb ef) : IReferenceDataRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();

    public Task<Contracts.CategoryCollection> GetAllCategoriesAsync()
        => _ef.Categories.Query().ToMappedItemsAsync<Persistence.Category, Contracts.CategoryCollection, Contracts.Category>(CategoryMapper.From);

    public Task<Contracts.SubCategoryCollection> GetAllSubCategoriesAsync()
        => _ef.SubCategories.Query().ToMappedItemsAsync<Persistence.SubCategory, Contracts.SubCategoryCollection, Contracts.SubCategory>(SubCategoryMapper.From);

    public Task<Contracts.UnitOfMeasureCollection> GetAllUnitsOfMeasureAsync()
        => _ef.UnitsOfMeasure.Query().ToMappedItemsAsync<Persistence.UnitOfMeasure, Contracts.UnitOfMeasureCollection, Contracts.UnitOfMeasure>(UnitOfMeasureMapper.From);

    public Task<Contracts.BrandCollection> GetAllBrandsAsync()
        => _ef.Brands.Query().ToMappedItemsAsync<Persistence.Brand, Contracts.BrandCollection, Contracts.Brand>(BrandMapper.From);

    public Task<Contracts.MovementKindCollection> GetAllMovementKindsAsync()
        => _ef.MovementKinds.Query().ToMappedItemsAsync<Persistence.MovementKind, Contracts.MovementKindCollection, Contracts.MovementKind>(MovementKindMapper.From);

    public Task<Contracts.MovementStatusCollection> GetAllMovementStatusesAsync()
        => _ef.MovementStatuses.Query().ToMappedItemsAsync<Persistence.MovementStatus, Contracts.MovementStatusCollection, Contracts.MovementStatus>(MovementStatusMapper.From);
}