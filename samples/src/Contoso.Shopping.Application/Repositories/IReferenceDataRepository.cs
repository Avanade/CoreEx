namespace Contoso.Shopping.Application.Repositories;

public interface IReferenceDataRepository
{
    public Task<Contracts.BasketStatusCollection> GetAllBasketStatusesAsync();

    public Task<Contracts.DiscountCouponCollection> GetAllDiscountCouponsAsync();

    public Task<Contracts.UnitOfMeasureCollection> GetAllUnitsOfMeasureAsync();
}