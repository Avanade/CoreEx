namespace Contoso.Products.Infrastructure.Mapping;

internal class MovementStatusMapper : BiDirectionMapper<Contracts.MovementStatus, Persistence.MovementStatus, MovementStatusMapper>
{
    protected override Persistence.MovementStatus OnMap(Contracts.MovementStatus source) => throw new NotImplementedException();

    protected override Contracts.MovementStatus OnMap(Persistence.MovementStatus source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        IsInactive = !source.IsActive,
        ETag = source.ETag
    };
}