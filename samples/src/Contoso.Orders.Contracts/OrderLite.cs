namespace Contoso.Orders.Contracts;

[Contract]
public partial class OrderLite : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    [NonNullable]
    public string? CustomerId { get; set; }

    [ReferenceData<OrderStatus>]
    [Localization("Order status")]
    [NonNullable]
    public partial string? StatusCode { get; set; }

    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }
}