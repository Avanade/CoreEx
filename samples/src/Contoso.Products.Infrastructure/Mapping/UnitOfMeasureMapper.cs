namespace Contoso.Products.Infrastructure.Mapping;

internal class UnitOfMeasureMapper : BiDirectionMapper<Contracts.UnitOfMeasure, Persistence.UnitOfMeasure, UnitOfMeasureMapper>
{
    protected override Persistence.UnitOfMeasure OnMap(Contracts.UnitOfMeasure source) => throw new NotImplementedException();

    protected override Contracts.UnitOfMeasure OnMap(Persistence.UnitOfMeasure source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        Scale = source.Scale,
        IsInactive = !source.IsActive
    };
}