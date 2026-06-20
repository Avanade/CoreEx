namespace Contoso.Orders.Contracts;

[Contract]
public abstract partial class OrderBase : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    public string? CustomerId { get; set; }

    [ReferenceData<OrderStatus>]
    [Localization("Order status")]
    public partial string? StatusCode { get; set; }

    public List<OrderItem>? Items { get; set; }
}