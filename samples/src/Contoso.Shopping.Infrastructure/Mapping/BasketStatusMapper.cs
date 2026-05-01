namespace Contoso.Shopping.Infrastructure.Mapping;

internal class BasketStatusMapper : BiDirectionMapper<Contracts.BasketStatus, Persistence.BasketStatus, BasketStatusMapper>
{
    protected override Persistence.BasketStatus OnMap(Contracts.BasketStatus source) => throw new NotImplementedException();

    protected override Contracts.BasketStatus OnMap(Persistence.BasketStatus source) => new()
    {
        Id = source.Id!,
        Code = source.Code,
        Text = source.Text,
        SortOrder = source.SortOrder,
        IsInactive = !source.IsActive
    };
}