namespace Contoso.Orders.Contracts;

[Contract]
public partial class OrderItem : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    public string? ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}