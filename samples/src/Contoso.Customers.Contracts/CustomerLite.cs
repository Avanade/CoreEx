namespace Contoso.Customers.Contracts;

[Contract]
public partial class CustomerLite : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    [NonNullable]
    public string? FirstName { get; set; }

    [NonNullable]
    public string? LastName { get; set; }

    [NonNullable]
    public string? Email { get; set; }

    [ReferenceData<CustomerType>]
    [NonNullable]
    public partial string? CustomerTypeCode { get; set; }

    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }
}
