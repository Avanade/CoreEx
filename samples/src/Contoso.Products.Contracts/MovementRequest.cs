namespace Contoso.Products.Contracts;

[Contract]
public partial class MovementRequest : IIdentifier<string?>
{
    [NonNullable]
    public string? Id { get; set; }

    [NonNullable]
    public DataMap<MovementRequestProduct>? Products { get; set; } 
}