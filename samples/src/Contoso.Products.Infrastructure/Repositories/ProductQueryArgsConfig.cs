namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>
/// Provides the <see cref="QueryArgs"/> configuration for <see cref="Contracts.Product"/>.
/// </summary>
internal class ProductQueryArgsConfig : QueryArgsConfig<ProductQueryArgsConfig>
{
    public ProductQueryArgsConfig()
    {
        // Configure the query arguments for filtering products.
        WithFilter(filter => filter
            .WithDefaultModelPrefix("Product")
            .AddField<string>(nameof(Contracts.ProductBase.Sku), c => c.WithOperators(QueryFilterOperator.EqualityOperators | QueryFilterOperator.StartsWith).AsUpperCase())
            .AddField<string>(nameof(Contracts.ProductBase.Text), c => c.WithOperators(QueryFilterOperator.StringFunctions).AsUpperCase())
            .AddReferenceDataField<Contracts.Category>(nameof(Contracts.ProductBase.Category), "CategoryCode", c => c.WithNoModelPrefix())
            .AddReferenceDataField<Contracts.SubCategory>(nameof(Contracts.ProductBase.SubCategory), "SubCategoryCode")
            .AddReferenceDataField<Contracts.Brand>(nameof(Contracts.ProductBase.Brand), "BrandCode"));

        // Configure the query arguments for ordering products.
        WithOrderBy(orderby => orderby
            .WithDefaultModelPrefix("Product")
            .AddField(nameof(Contracts.ProductBase.Sku), c => c.WithDefault().WithAlwaysInclude())
            .AddField(nameof(Contracts.ProductBase.Text))
            .AddField(nameof(Contracts.ProductBase.Brand)));
    }
}
