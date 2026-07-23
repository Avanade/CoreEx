namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>
/// Provides the <see cref="QueryArgs"/> configuration for <see cref="Contracts.Movement"/>.
/// </summary>
internal class MovementQueryArgsConfig : QueryArgsConfig<MovementQueryArgsConfig>
{
    public MovementQueryArgsConfig()
    {
        // Configure the query arguments for filtering movements.
        WithFilter(filter => filter
            .AddField<string>(nameof(Contracts.Movement.ReferenceId), c => c.WithOperators(QueryFilterOperator.EqualityOperators))
            .AddField<string>(nameof(Contracts.Movement.ProductId), c => c.WithOperators(QueryFilterOperator.EqualityOperators))
            .AddReferenceDataField<Contracts.MovementKind>(nameof(Contracts.Movement.Kind), nameof(Persistence.Movement.MovementKindCode))
            .AddReferenceDataField<Contracts.MovementStatus>(nameof(Contracts.Movement.Status), nameof(Persistence.Movement.MovementStatusCode)));

        // Configure the query arguments for ordering movements.
        WithOrderBy(orderby => orderby
            .AddField(nameof(Contracts.Movement.ReferenceId), c => c.WithDefault().WithAlwaysInclude())
            .AddField(nameof(Contracts.Movement.ProductId), c => c.WithDefault().WithAlwaysInclude()));
    }
}
