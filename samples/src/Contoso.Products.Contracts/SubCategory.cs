namespace Contoso.Products.Contracts;

[ReferenceData]
public partial class SubCategory : ReferenceData<SubCategory>
{
    [ReferenceData<Category>]
    public partial string? CategoryCode { get; init; }
}

public class SubCategoryCollection() : ReferenceDataCollection<SubCategory>(ReferenceDataSortOrder.Code) { }