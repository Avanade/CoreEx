namespace Contoso.Products.Infrastructure.Mapping;

internal class BrandMapper : BiDirectionMapper<Contracts.Brand, Persistence.Brand, BrandMapper>
{
    protected override Persistence.Brand OnMap(Contracts.Brand source) => throw new NotImplementedException();

    protected override Contracts.Brand OnMap(Persistence.Brand source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        IsInactive = !source.IsActive,
        ETag = source.ETag
    };
}