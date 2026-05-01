namespace Contoso.Products.Infrastructure.Repositories;

[ScopedService<IProductRepository>]
public class ProductRepository(ProductsEfDb ef) : IProductRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();

    private readonly static QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
        .WithFilter(filter => filter
            .WithDefaultModelPrefix("Product")
            .AddField<string>(nameof(Contracts.ProductBase.Sku), c => c.WithOperators(QueryFilterOperator.EqualityOperators | QueryFilterOperator.StartsWith).AsUpperCase())
            .AddField<string>(nameof(Contracts.ProductBase.Text), c => c.WithOperators(QueryFilterOperator.StringFunctions).AsUpperCase())
            .AddReferenceDataField<Contracts.Category>(nameof(Contracts.ProductBase.Category), "CategoryCode", c => c.WithModelPrefix(null))
            .AddReferenceDataField<Contracts.SubCategory>(nameof(Contracts.ProductBase.SubCategory), "SubCategoryCode")
            .AddReferenceDataField<Contracts.Brand>(nameof(Contracts.ProductBase.Brand), "BrandCode"))
        .WithOrderBy(orderby => orderby
            .WithDefaultModelPrefix("Product")
            .AddField(nameof(Contracts.ProductBase.Sku), c => c.WithDefault().WithAlwaysInclude())
            .AddField(nameof(Contracts.ProductBase.Text))
            .AddField(nameof(Contracts.ProductBase.Brand)));

    public Task<Contracts.Product?> GetAsync(string id) => _ef.Products.GetAsync(id);

    public Task<DataResult<Contracts.Product>> CreateAsync(Contracts.Product product) => _ef.Products.CreateAsync(product);

    public Task<DataResult<Contracts.Product>> UpdateAsync(Contracts.Product product) => _ef.Products.UpdateAsync(product);

    public Task<DataResult> DeleteAsync(string id) => _ef.Products.DeleteAsync(id);

    public Task<JsonElement> QuerySchemaAsync() => Task.FromResult(_queryConfig.ToJsonSchema());

    public async Task<ItemsResult<Contracts.ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
    {
        var parsed = _queryConfig.Parse(query).ThrowOnError();

        var products = _ef.Products.Model.Query();
        if (query?.IsIncludeInactive is false)
            products = products.Where(p => !p.IsInactive);

        var subcats = _ef.SubCategories.Query();
        var inventory = _ef.Inventory.Query();

        var q =
            from p in products
            join sc in subcats on p.SubCategoryCode equals sc.Code into scg
            from sc in scg.DefaultIfEmpty()
            join i in inventory on p.Id equals i.Id into ig
            from i in ig.DefaultIfEmpty()
            select new
            {
                Product = p,
                SubCategoryCode = sc.Code,
                sc.CategoryCode,
                QtyOnHand = i == null ? 0 : i.QtyOnHand
            };

        return await q.Where(parsed).OrderBy(parsed).ToMappedItemsResultAsync(x => new Contracts.ProductLite
        {
            Id = x.Product.Id,
            Sku = x.Product.Sku,
            Text = x.Product.Text,
            CategoryCode = x.CategoryCode,
            SubCategoryCode = x.SubCategoryCode,
            BrandCode = x.Product.BrandCode,
            UnitOfMeasureCode = x.Product.UnitOfMeasureCode,
            Price = x.Product.Price,
            IsInactive = x.Product.IsInactive,
            IsNonStocked = x.Product.IsNonStocked,
            QtyOnHand = x.QtyOnHand
        }, paging);
    }

    public Task<Dictionary<string, Contracts.ProductReserve>> GetForReservationAsync(string[] ids)
    {
        var products = _ef.Products.Model.Query();

        var q =
            from p in products
            where ids.Contains(p.Id)
            select new Contracts.ProductReserve
            {
                Id = p.Id!,
                UnitOfMeasureCode = p.UnitOfMeasureCode!,
                IsInactive = p.IsInactive,
                IsNonStocked = p.IsNonStocked
            };

        return q.ToDictionaryAsync(x => x.Id, x => x);
    }
}