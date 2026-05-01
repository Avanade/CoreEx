namespace Contoso.Products.Contracts;

[ReferenceData]
public partial class Brand : ReferenceData<Brand> { }

public class BrandCollection() : ReferenceDataCollection<Brand>(ReferenceDataSortOrder.Code) { }