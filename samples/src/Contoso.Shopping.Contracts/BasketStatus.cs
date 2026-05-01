namespace Contoso.Shopping.Contracts;

[ReferenceData]
public partial class BasketStatus : ReferenceData<BasketStatus>
{
    public const string Empty = "E";
    public const string Active = "A";
    public const string CheckedOut = "C";
    public const string Abandoned = "B";

    public bool CanBeMutated => Code is Empty or Active;
}

public class BasketStatusCollection() : ReferenceDataCollection<BasketStatus>(ReferenceDataSortOrder.Code) { }