namespace Contoso.Products.Contracts;

[Contract]
public partial class ProductReserve : IIdentifier<string>
{
    public string Id { get; set; } = default!;

    public string UnitOfMeasureCode { get; set; } = default!;

    public bool IsNonStocked { get; set; }

    public bool IsInactive { get; set; }
}