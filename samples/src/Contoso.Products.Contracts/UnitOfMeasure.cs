namespace Contoso.Products.Contracts;

public partial class UnitOfMeasure
{
    [JsonIgnore]
    public int Precision => 16 - Scale;
}