namespace Contoso.Customers.Contracts;

[Contract]
public partial class CustomerLite : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    [ReferenceData<CustomerType>]
    public partial string? CustomerTypeCode { get; set; }

    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }
}
