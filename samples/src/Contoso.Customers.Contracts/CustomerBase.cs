namespace Contoso.Customers.Contracts;

[Contract]
public abstract partial class CustomerBase : IIdentifier<string?>
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public Address? ShippingAddress { get; set; }

    [ReferenceData<CustomerType>]
    public partial string? CustomerTypeCode { get; set; }

    [ReferenceData<ContactMethod>]
    public partial string? ContactMethodCode { get; set; }

    [ReadOnly(true)]
    public bool HasShopped { get; set; }
}
