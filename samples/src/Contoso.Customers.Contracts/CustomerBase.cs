namespace Contoso.Customers.Contracts;

[Contract]
public abstract partial class CustomerBase : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    [NonNullable]
    public string? FirstName { get; set; }

    [NonNullable]
    public string? LastName { get; set; }

    [NonNullable]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    public Address? ShippingAddress { get; set; }

    [ReferenceData<CustomerType>]
    [NonNullable]
    public partial string? CustomerTypeCode { get; set; }

    [ReferenceData<ContactMethod>]
    [NonNullable]
    public partial string? ContactMethodCode { get; set; }

    [ReadOnly(true)]
    public bool HasShopped { get; set; }
}
