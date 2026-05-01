namespace Contoso.Products.Contracts;

[ReferenceData]
public partial class MovementKind : ReferenceData<MovementKind>
{
    public const string Adjust = "A";
    public const string Issue = "I";
    public const string Receive = "R";
}

public class MovementKindCollection() : ReferenceDataCollection<MovementKind>(ReferenceDataSortOrder.Code) { }