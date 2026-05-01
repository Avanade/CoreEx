namespace Contoso.Products.Infrastructure.Mapping;

internal class CategoryMapper : BiDirectionMapper<Contracts.Category, Persistence.Category, CategoryMapper>
{
    protected override Persistence.Category OnMap(Contracts.Category source) => throw new NotImplementedException();

    protected override Contracts.Category OnMap(Persistence.Category source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        IsInactive = !source.IsActive,
        ETag = source.ETag
    };
}