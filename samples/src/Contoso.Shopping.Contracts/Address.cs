namespace Contoso.Shopping.Contracts;

[Contract]
public partial class Address
{
    public string? Street1 { get; set; }
    public string? Street2 { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? State { get; set; }
}
