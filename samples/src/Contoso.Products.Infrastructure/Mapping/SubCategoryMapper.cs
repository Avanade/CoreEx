namespace Contoso.Products.Infrastructure.Mapping;

internal class SubCategoryMapper : BiDirectionMapper<Contracts.SubCategory, Persistence.SubCategory, SubCategoryMapper>
{
    protected override Persistence.SubCategory OnMap(Contracts.SubCategory source) => throw new NotImplementedException();

    protected override Contracts.SubCategory OnMap(Persistence.SubCategory source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        IsInactive = !source.IsActive,
        CategoryCode = source.CategoryCode
    };
}