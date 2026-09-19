namespace Contoso.Customers.Infrastructure.Persistence;

public class Customer : CosmosDbModelBase
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public Address? ShippingAddress { get; set; }
    public string? CustomerTypeCode { get; set; }
    public string? ContactMethodCode { get; set; }
    public bool HasShopped { get; set; }
}
