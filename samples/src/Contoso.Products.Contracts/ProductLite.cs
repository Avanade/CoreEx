namespace Contoso.Products.Contracts;

[Contract]
public partial class ProductLite : ProductBase
{
    public decimal QtyOnHand { get; set; }
}