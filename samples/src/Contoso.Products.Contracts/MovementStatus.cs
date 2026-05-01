namespace Contoso.Products.Contracts;

[ReferenceData]
public partial class MovementStatus : ReferenceData<MovementStatus>
{
    public const string Pending = "P";
    public const string Confirmed = "C";
    public const string Canceled = "X";
}

public class MovementStatusCollection() : ReferenceDataCollection<MovementStatus>(ReferenceDataSortOrder.Code) { }