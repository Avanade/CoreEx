namespace Contoso.Shopping.Infrastructure.Clients.Products;

public class MovementRequestProduct
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Quantity { get; set; }

    public string? UnitOfMeasure { get; set; }
}
