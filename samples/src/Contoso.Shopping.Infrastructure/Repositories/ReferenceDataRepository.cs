namespace Contoso.Shopping.Infrastructure.Repositories;

[ScopedService<IReferenceDataRepository>]
public class ReferenceDataRepository(ShoppingEfDb ef) : IReferenceDataRepository
{
    private readonly ShoppingEfDb _ef = ef.ThrowIfNull();

    public Task<Contracts.BasketStatusCollection> GetAllBasketStatusesAsync()
        => _ef.BasketStatuses.Query().ToMappedItemsAsync<Persistence.BasketStatus, Contracts.BasketStatusCollection, Contracts.BasketStatus>(BasketStatusMapper.From);

    public Task<Contracts.DiscountCouponCollection> GetAllDiscountCouponsAsync()
        => _ef.DiscountCoupons.Query().ToMappedItemsAsync<Persistence.DiscountCoupon, Contracts.DiscountCouponCollection, Contracts.DiscountCoupon>(DiscountCouponMapper.From);

    public Task<Contracts.UnitOfMeasureCollection> GetAllUnitsOfMeasureAsync()
        => _ef.UnitsOfMeasure.Query().ToMappedItemsAsync<Persistence.UnitOfMeasure, Contracts.UnitOfMeasureCollection, Contracts.UnitOfMeasure>(UnitOfMeasureMapper.From);
}
