namespace Contoso.Shopping.Infrastructure.Repositories;

/// <summary>
/// Provides the <see cref="QueryArgs"/> configuration for <see cref="Contracts.Basket"/>.
/// </summary>
/// <remarks>The <see cref="Contracts.Basket.CustomerId"/> is not a query field; it is always applied by the <see cref="BasketRepository"/>.</remarks>
public class BasketQueryArgsConfig : QueryArgsConfig<BasketQueryArgsConfig>
{
    private const string _pricingTotal = $"{nameof(Contracts.Basket.Pricing)}.{nameof(Contracts.BasketPricing.Total)}";

    public BasketQueryArgsConfig()
    {
        // Configure the query arguments for filtering baskets.
        WithFilter(filter => filter
            .AddReferenceDataField<Contracts.BasketStatus>("Status", nameof(Persistence.Basket.BasketStatusCode))
            .AddField<decimal>(_pricingTotal, nameof(Persistence.Basket.Total), c => c.WithOperators(QueryFilterOperator.ComparisonOperators)));

        // Configure the query arguments for ordering baskets; CreatedOn is always the final order-by to ensure consistent sequencing.
        WithOrderBy(orderby => orderby
            .AddField(nameof(Contracts.Basket.Status), nameof(Persistence.Basket.BasketStatusCode))
            .AddField(_pricingTotal, nameof(Persistence.Basket.Total))
            .AddField(nameof(Persistence.Basket.CreatedOn), c => c.WithDefault().WithAlwaysInclude()));
    }
}
