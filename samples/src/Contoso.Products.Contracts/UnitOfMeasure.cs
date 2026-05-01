namespace Contoso.Products.Contracts;

[ReferenceData]
public partial class UnitOfMeasure : ReferenceData<UnitOfMeasure>
{
    [JsonIgnore]
    public int Precision => 16 - Scale;

    public int Scale { get; init; }
}

public class UnitOfMeasureCollection() : ReferenceDataCollection<UnitOfMeasure>(ReferenceDataSortOrder.Code) { }