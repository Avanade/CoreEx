namespace Contoso.Shopping.Infrastructure.Repositories;

public sealed class ShoppingEfDb(ShoppingDbContext dbContext) : EfDb<ShoppingDbContext>(dbContext, _options)
{
    private static readonly EfDbOptions _options = new();

    public EfDbModel<Persistence.BasketStatus> BasketStatuses => Model<Persistence.BasketStatus>();

    public EfDbModel<Persistence.DiscountCoupon> DiscountCoupons => Model<Persistence.DiscountCoupon>();

    public EfDbModel<Persistence.UnitOfMeasure> UnitsOfMeasure => Model<Persistence.UnitOfMeasure>();

    public EfDbModel<Persistence.Basket> Baskets => Model<Persistence.Basket>();

    public EfDbModel<Persistence.BasketItem> BasketItems => Model<Persistence.BasketItem>();

    public EfDbModel<Persistence.Product> Products => Model<Persistence.Product>();
}