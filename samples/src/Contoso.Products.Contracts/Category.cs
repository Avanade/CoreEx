namespace Contoso.Products.Contracts;

[ReferenceData]
public partial class Category : ReferenceData<Category> { }

public class CategoryCollection() : ReferenceDataCollection<Category>(ReferenceDataSortOrder.Code) { }