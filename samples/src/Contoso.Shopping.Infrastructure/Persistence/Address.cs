namespace Contoso.Shopping.Infrastructure.Persistence;

public class Address
{
    public string Street1 { get; set; } = default!;
    public string? Street2 { get; set; }
    public string City { get; set; } = default!;
    public string PostCode { get; set; } = default!;
    public string State { get; set; } = default!;
}
