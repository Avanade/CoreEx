namespace Contoso.Shopping.Infrastructure.Clients.Products;

public class MovementRequest : IIdentifier<string?>
{
    public string? Id { get; set; }

    public DataMap<MovementRequestProduct>? Products { get; set; }
}
