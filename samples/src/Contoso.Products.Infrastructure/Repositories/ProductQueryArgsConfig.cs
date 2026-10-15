namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>
/// Provides the <see cref="QueryArgs"/> configuration for <see cref="Contracts.Product"/>.
/// </summary>
/// <remarks><c>Contoso.Products.Api</c>'s <c>Program.cs</c> passes <see cref="Default"/> directly into <c>CoreEx.Data.GraphQL</c>'s <c>AddQuery</c> to drive GraphQL <c>where</c>/<c>orderBy</c> argument mapping.</remarks>
public class ProductQueryArgsConfig : QueryArgsConfig<ProductQueryArgsConfig>
{
    /// <summary>Creates the default semantic query configuration.</summary>
    public ProductQueryArgsConfig() : this(QuerySearchMode.Semantic) { }

    /// <summary>The server-selected full-text configuration; filter fields remain identical.</summary>
    public static ProductQueryArgsConfig FullText { get; } = new(QuerySearchMode.FullText);

    /// <summary>The server-selected hybrid configuration; filter fields remain identical.</summary>
    public static ProductQueryArgsConfig Hybrid { get; } = new(QuerySearchMode.Hybrid);

    /// <summary>Resolves the server-selected search mode without constructing a query configuration.</summary>
    public static QuerySearchMode ResolveMode(IConfiguration configuration)
        => configuration.ThrowIfNull()["Products:Search:Mode"] switch
        {
            null or "" or "Semantic" => QuerySearchMode.Semantic,
            "FullText" => QuerySearchMode.FullText,
            "Hybrid" => QuerySearchMode.Hybrid,
            var mode => throw new InvalidOperationException($"Unsupported Products:Search:Mode '{mode}'. Expected Semantic, FullText or Hybrid.")
        };

    /// <summary>Resolves the explicit server setting without changing the public query arguments.</summary>
    public static ProductQueryArgsConfig FromConfiguration(IConfiguration configuration) => ResolveMode(configuration) switch
    {
        QuerySearchMode.FullText => FullText,
        QuerySearchMode.Hybrid => Hybrid,
        _ => Default
    };

    private ProductQueryArgsConfig(QuerySearchMode mode)
    {
        // Configure the query arguments for filtering products.
        WithFilter(filter => filter
            .WithDefaultModelPrefix("Product")
            .AddField<string>(nameof(Contracts.ProductBase.Sku), c => c.WithOperators(QueryFilterOperator.EqualityOperators | QueryFilterOperator.StartsWith).AsUpperCase())
            .AddField<string>(nameof(Contracts.ProductBase.Text), c => c.WithOperators(QueryFilterOperator.StringFunctions).AsUpperCase())
            .AddField<decimal>(nameof(Contracts.ProductBase.Price), c => c.WithOperators(QueryFilterOperator.ComparisonOperators))
            .AddReferenceDataField<Contracts.Category>(nameof(Contracts.ProductBase.Category), "CategoryCode", c => c.WithNoModelPrefix())
            .AddReferenceDataField<Contracts.SubCategory>(nameof(Contracts.ProductBase.SubCategory), "SubCategoryCode")
            .AddReferenceDataField<Contracts.Brand>(nameof(Contracts.ProductBase.Brand), "BrandCode"));

        // Configure the query arguments for ordering products.
        WithOrderBy(orderby => orderby
            .WithDefaultModelPrefix("Product")
            .AddField(nameof(Contracts.ProductBase.Sku), c => c.WithDefault().WithAlwaysInclude())
            .AddField(nameof(Contracts.ProductBase.Text))
            .AddField(nameof(Contracts.ProductBase.Brand)));

        // Ranked search is executed by the repository; ordinary queries retain the configured SKU ordering.
        WithSearch(search => search
            .WithMode(mode)
            .WithHelpText(mode switch
            {
                QuerySearchMode.FullText => "English full-text matching with websearch syntax, weighted SKU/name, category/brand, tags and description; exact canonical SKU first, then cover-density rank. No semantic expansion.",
                QuerySearchMode.Hybrid => $"Reciprocal Rank Fusion (RRF) combines English full-text and exact semantic rankings. Each branch contributes at most {ProductSearchFusion.CandidateLimit} eligible candidates (RRF k={ProductSearchFusion.RrfK}); an exact eligible canonical SKU is prioritized. $count reports the deduplicated candidate union (at most {ProductSearchFusion.MaximumCandidateCount}), not all eligible products. Paging is over this stable bounded pool.",
                _ => "Semantic similarity using exact cosine distance over the configured product embedding profile."
            }));
    }
}
