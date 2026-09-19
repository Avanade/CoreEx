namespace Contoso.Customers.Contracts;

[Contract]
public partial class Customer : CustomerBase, IETag, IChangeLog
{
    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }

    [ReadOnly(true)]
    public string? ETag { get; set; }
}
