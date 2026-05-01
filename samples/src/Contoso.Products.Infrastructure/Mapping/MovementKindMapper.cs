namespace Contoso.Products.Infrastructure.Mapping;

internal class MovementKindMapper : BiDirectionMapper<Contracts.MovementKind, Persistence.MovementKind, MovementKindMapper>
{
    protected override Persistence.MovementKind OnMap(Contracts.MovementKind source) => throw new NotImplementedException();

    protected override Contracts.MovementKind OnMap(Persistence.MovementKind source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        IsInactive = !source.IsActive,
        ETag = source.ETag
    };
}