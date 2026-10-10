namespace Contoso.Products.Contracts;

[Contract]
public partial class MovementRequestProduct
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Quantity { get; set; }

    [ReferenceData<UnitOfMeasure>]
    [Localization("Unit-of-measure")]
    [NonNullable]
    public partial string? UnitOfMeasureCode { get; set; }
}