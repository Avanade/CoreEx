namespace Contoso.Orders.Infrastructure.Persistence;

public partial class OrderItem : ModelBase<string>
{
    public string OrderId { get; set; }

    public string ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}