namespace Contoso.Orders.Infrastructure.Repositories;

/// <summary>
/// Provides the <see cref="QueryArgs"/> configuration for <see cref="Contracts.Order"/> queries.
/// </summary>
internal class OrderQueryArgsConfig : QueryArgsConfig<OrderQueryArgsConfig>
{
    public OrderQueryArgsConfig()
    {
        // Configure the query arguments for filtering orders.
        WithFilter(filter => filter
            .AddField<string>(nameof(Contracts.OrderBase.CustomerId), c => c.WithOperators(QueryFilterOperator.EqualityOperators))
            .AddReferenceDataField<Contracts.OrderStatus>(nameof(Contracts.OrderBase.Status), "StatusCode"));

        // Configure the query arguments for ordering orders.
        WithOrderBy(orderby => orderby
            .AddField(nameof(Contracts.OrderBase.CustomerId), c => c.WithDefault().WithAlwaysInclude()));
    }
}
