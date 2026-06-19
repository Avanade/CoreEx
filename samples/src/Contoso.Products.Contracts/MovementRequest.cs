namespace Contoso.Products.Contracts;

[Contract]
public partial class MovementRequest : IIdentifier<string?>
{
    public string? Id { get; set; }

    public DataMap<MovementRequestProduct>? Products { get; set; } 
}