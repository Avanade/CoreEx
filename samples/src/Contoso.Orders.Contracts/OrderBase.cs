namespace Contoso.Orders.Contracts;

[Contract]
public abstract partial class OrderBase : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    [NonNullable]
    public string? CustomerId { get; set; }

    [ReferenceData<OrderStatus>]
    [Localization("Order status")]
    [NonNullable]
    public partial string? StatusCode { get; set; }

    public List<OrderItem>? Items { get; set; }
}