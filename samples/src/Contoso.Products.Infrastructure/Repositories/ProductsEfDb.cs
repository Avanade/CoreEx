namespace Contoso.Products.Infrastructure.Repositories;

public sealed class ProductsEfDb(ProductsDbContext dbContext) : EfDb<ProductsDbContext>(dbContext, _options)
{
    private static readonly EfDbOptions _options = new EfDbOptions()
        .WithModel<Persistence.Product>(m => m.WithLogicalDeleteFilter());

    public EfDbModel<Persistence.Category> Categories => Model<Persistence.Category>();

    public EfDbModel<Persistence.SubCategory> SubCategories => Model<Persistence.SubCategory>();

    public EfDbModel<Persistence.UnitOfMeasure> UnitsOfMeasure => Model<Persistence.UnitOfMeasure>();

    public EfDbModel<Persistence.Brand> Brands => Model<Persistence.Brand>();

    public EfDbModel<Persistence.MovementKind> MovementKinds => Model<Persistence.MovementKind>();

    public EfDbModel<Persistence.MovementStatus> MovementStatuses => Model<Persistence.MovementStatus>();

    public EfDbMappedModel<Contracts.Product, Persistence.Product, ProductMapper> Products => Model<Persistence.Product>().ToMappedModel<Contracts.Product, ProductMapper>(ProductMapper.Default);

    public EfDbModel<Persistence.Inventory> Inventory => Model<Persistence.Inventory>();

    public EfDbModel<Persistence.Movement> Movements => Model<Persistence.Movement>();
}