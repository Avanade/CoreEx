namespace Contoso.Orders.Infrastructure.Persistence;

public partial class Order
{
    public virtual ICollection<OrderItem> Items { get; set; } = [];
}