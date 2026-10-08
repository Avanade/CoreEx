namespace Contoso.Shopping.Application.Adapters.Customers;

/// <summary>
/// Represents the Shopping domain's view of a Customer (from the Customers domain); only the data required by Shopping is included.
/// </summary>
[Contract]
public partial class Customer : IIdentifier<string?>
{
    public string? Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public Address? ShippingAddress { get; set; }
}
