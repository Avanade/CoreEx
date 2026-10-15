namespace Contoso.Products.Infrastructure.Repositories;

[ScopedService<IProductRepository>]
public class ProductRepository(ProductsEfDb ef) : IProductRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();

    public Task<Contracts.Product?> GetAsync(string id, CancellationToken cancellationToken = default) => _ef.Products.GetAsync(id, cancellationToken);

    public Task<DataResult<Contracts.Product>> CreateAsync(Contracts.Product product, CancellationToken cancellationToken = default) => _ef.Products.CreateAsync(product, cancellationToken);

    public Task<DataResult<Contracts.Product>> UpdateAsync(Contracts.Product product, CancellationToken cancellationToken = default) => _ef.Products.UpdateAsync(product, cancellationToken);

    public Task<DataResult> DeleteAsync(string id, CancellationToken cancellationToken = default) => _ef.Products.DeleteAsync(id, cancellationToken);

    public Task<Dictionary<string, Contracts.ProductReserve>> GetForReservationAsync(string[] ids, CancellationToken cancellationToken = default)
    {
        var products = _ef.Products.Model.Query();
        var list = ids.ToList();    // PostgreSQL EF has issues translating array.Contains, but can do list.Contains; see: https://github.com/npgsql/efcore.pg/issues/3461.

        var q =
            from p in products
            where list.Contains(p.Id)
            select new Contracts.ProductReserve
            {
                Id = p.Id!,
                UnitOfMeasureCode = p.UnitOfMeasureCode!,
                IsInactive = p.IsInactive,
                IsNonStocked = p.IsNonStocked
            };

        return q.ToDictionaryAsync(x => x.Id, x => x, cancellationToken);
    }
}
