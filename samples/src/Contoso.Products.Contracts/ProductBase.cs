namespace Contoso.Products.Contracts;

[Contract]
public abstract partial class ProductBase : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    public string? Sku { get; set => field = value?.ToUpper(); }

    public string? Text { get; set; }

    [ReadOnly(true)]
    [ReferenceData<Category>]
    public partial string? CategoryCode { get; set; }

    [ReferenceData<SubCategory>]
    [Localization("Sub-category")]
    public partial string? SubCategoryCode { get; set; }

    [ReferenceData<UnitOfMeasure>()]
    [Localization("Unit-of-measure")]
    public partial string? UnitOfMeasureCode { get; set; }

    [ReferenceData<Brand>()]
    public partial string? BrandCode { get; set; }

    public decimal Price { get; set; }

    public bool IsNonStocked { get; set; }

    [ReadOnly(true)]
    public bool IsInactive { get; set; }
}