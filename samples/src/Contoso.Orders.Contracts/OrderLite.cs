namespace Contoso.Orders.Contracts;

[Contract]
public partial class OrderLite : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    public string? CustomerId { get; set; }

    [ReferenceData<OrderStatus>]
    [Localization("Order status")]
    public partial string? StatusCode { get; set; }

    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }
}