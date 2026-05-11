namespace Contoso.Orders.Infrastructure.Persistence;

public partial class Order : ModelBase<string?>
{
    public string? CustomerId { get; set; }

    public string? StatusCode { get; set; }

    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}