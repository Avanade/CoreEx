namespace Contoso.Products.Application.Repositories;

public interface IReferenceDataRepository
{
    public Task<Contracts.CategoryCollection> GetAllCategoriesAsync();

    public Task<Contracts.SubCategoryCollection> GetAllSubCategoriesAsync();

    public Task<Contracts.UnitOfMeasureCollection> GetAllUnitsOfMeasureAsync();

    public Task<Contracts.BrandCollection> GetAllBrandsAsync();

    public Task<Contracts.MovementKindCollection> GetAllMovementKindsAsync();

    public Task<Contracts.MovementStatusCollection> GetAllMovementStatusesAsync();
}