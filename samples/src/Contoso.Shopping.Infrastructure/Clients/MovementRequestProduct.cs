namespace Contoso.Shopping.Infrastructure.Clients;

public class MovementRequestProduct
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Quantity { get; set; }

    public string? UnitOfMeasure { get; set; }
}