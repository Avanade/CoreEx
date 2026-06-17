
namespace Contoso.Products.Infrastructure.Mapping;

public class MovementMapper : BiDirectionMapper<Contracts.Movement, Persistence.Movement, MovementMapper>
{
    protected override Persistence.Movement OnMap(Contracts.Movement source) => new()
    {
        Id = source.Id!,
        ReferenceId = source.ReferenceId!,
        MovementKindCode = source.KindCode!,
        MovementStatusCode = source.StatusCode!,
        ProductId = source.ProductId!,
        Quantity = source.Quantity,
        UnitOfMeasureCode = source.UnitOfMeasureCode!
    };

    protected override Contracts.Movement OnMap(Persistence.Movement source) => new()
    {
        Id = source.Id,
        ReferenceId = source.ReferenceId,
        KindCode = source.MovementKindCode,
        StatusCode = source.MovementStatusCode,
        ProductId = source.ProductId,
        Quantity = source.Quantity,
        UnitOfMeasureCode = source.UnitOfMeasureCode
    };
}