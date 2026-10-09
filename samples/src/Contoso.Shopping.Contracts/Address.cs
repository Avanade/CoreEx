namespace Contoso.Shopping.Contracts;

[Contract]
public partial class Address
{
    [NonNullable]
    public string? Street1 { get; set; }

    public string? Street2 { get; set; }

    [NonNullable]
    public string? City { get; set; }

    [NonNullable]
    public string? PostCode { get; set; }

    [NonNullable]
    public string? State { get; set; }
}
