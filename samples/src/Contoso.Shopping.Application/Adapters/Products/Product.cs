namespace Contoso.Shopping.Application.Adapters.Products;

[Contract]
public partial class Product : IIdentifier<string?>
{
    public string? Id { get; set; }

    public string? Sku { get; set => field = value?.ToUpper(); }

    public string? Text { get; set; }

    [ReferenceData<UnitOfMeasure>()]
    public partial string? UnitOfMeasureCode { get; set; }

    public decimal Price { get; set; }

    public bool IsInactive { get; set; }

    public bool IsNonStocked { get; set; }
}