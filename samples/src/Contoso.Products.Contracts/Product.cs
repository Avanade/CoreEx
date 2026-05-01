namespace Contoso.Products.Contracts;

[Contract]
public partial class Product : ProductBase, IETag, IChangeLog
{
    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }

    [ReadOnly(true)]
    public string? ETag { get; set; }
}