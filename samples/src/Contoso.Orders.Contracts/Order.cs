namespace Contoso.Orders.Contracts;

[Contract]
public partial class Order : OrderBase, IETag, IChangeLog
{
    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }

    [ReadOnly(true)]
    public string? ETag { get; set; }
}