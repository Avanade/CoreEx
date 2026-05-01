namespace Contoso.Shopping.Contracts;

[ReferenceData]
public partial class UnitOfMeasure : ReferenceData<UnitOfMeasure>
{
    public int Scale { get; init; }
}

public class UnitOfMeasureCollection() : ReferenceDataCollection<UnitOfMeasure>(ReferenceDataSortOrder.Code) { }