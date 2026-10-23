namespace Contoso.Mcp.Tools;

/// <summary>Live Products reference data available for structured search filters.</summary>
public sealed class ProductSearchOptions
{
    /// <summary>Categories and their filter codes.</summary>
    public required Category[] Categories { get; init; }

    /// <summary>Subcategories, their filter codes and parent category codes.</summary>
    [JsonPropertyName("sub-categories")]
    public required SubCategory[] SubCategories { get; init; }

    /// <summary>Brands and their filter codes.</summary>
    public required Brand[] Brands { get; init; }
}
