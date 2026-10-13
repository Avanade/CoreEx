namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>
/// Provides the <see cref="QueryArgs"/> configuration for <see cref="Contracts.Product"/>.
/// </summary>
/// <remarks><c>Contoso.Products.Api</c>'s <c>Program.cs</c> passes <see cref="Default"/> directly into <c>CoreEx.Data.GraphQL</c>'s <c>AddQuery</c> to drive GraphQL <c>where</c>/<c>orderBy</c> argument mapping.</remarks>
public class ProductQueryArgsConfig : QueryArgsConfig<ProductQueryArgsConfig>
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

        // Configure the free-text $search; keyword mode (unranked) where each term must match the Text (contains) or Sku (starts-with). A semantic/hybrid (vector) search would
        // instead use WithMode(QuerySearchMode.Semantic) and be executed by the repository (see QueryArgsParseResult.IsRankedSearch).
        WithSearch(search => search
            .WithDefaultModelPrefix("Product")
            .AddKeywordField(nameof(Contracts.ProductBase.Text), c => c.AsUpperCase())
            .AddKeywordField(nameof(Contracts.ProductBase.Sku), c => c.WithOperator(QueryFilterOperator.StartsWith).AsUpperCase()));
    }
}
