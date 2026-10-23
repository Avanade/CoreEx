namespace Contoso.Products.Application.Search;

/// <summary>
/// Represents a canonical, model-independent product document and its content fingerprint.
/// </summary>
/// <param name="Text">The document containing only authoritative searchable attributes.</param>
/// <param name="SourceHash">The SHA-256 fingerprint of the template version and document.</param>
public sealed record ProductSearchDocument(string Text, string SourceHash)
{
    /// <summary>Gets the template version; changes to the document recipe require a new version.</summary>
    public const string TemplateVersion = "product-document-v2";

    /// <summary>
    /// Creates a document using the product's reference-data labels, falling back to codes where labels are unavailable.
    /// </summary>
    /// <remarks>Price, inventory, activity, ETag and changelog are deliberately excluded; use live relational state for eligibility and display.</remarks>
    public static ProductSearchDocument Create(ProductBase product)
    {
        product.ThrowIfNull();
        var sku = Normalize(product.Sku).ThrowIfNullOrEmpty(nameof(product.Sku));
        var name = Normalize(product.Text).ThrowIfNullOrEmpty(nameof(product.Text));
        var tags = product.Tags is null ? [] : product.Tags.Select(Normalize).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var text = string.Join('\n',
            $"Name: {name}",
            $"SKU: {sku}",
            $"Description: {Normalize(product.Description)}",
            $"Brand: {LabelOrCode(product.Brand?.Text, product.BrandCode)}",
            $"Category: {LabelOrCode(product.Category?.Text, product.CategoryCode)}",
            $"Subcategory: {LabelOrCode(product.SubCategory?.Text, product.SubCategoryCode)}",
            $"Tags: {string.Join(", ", tags)}");

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{TemplateVersion}\n{text}")));
        return new ProductSearchDocument(text, hash);
    }

    /// <summary>Normalizes whitespace without changing word order, repetition or casing.</summary>
    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Uses an available reference-data label, otherwise retaining its code.</summary>
    private static string LabelOrCode(string? label, string? code) => string.IsNullOrWhiteSpace(label) ? Normalize(code) : Normalize(label);
}
