namespace Contoso.Orders.Contracts;

[Contract]
public partial class OrderItem : IIdentifier<string?>
{
    [NonNullable]
    public string? Id { get; set; }

    [NonNullable]
    public string? ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}