namespace Contoso.Orders.Infrastructure.Repositories;

[ScopedService<IOrderRepository>]
public class OrderRepository(OrdersEfDb ef) : IOrderRepository
{
    private readonly OrdersEfDb _ef = ef.ThrowIfNull();

    private static readonly QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
        .WithFilter(filter => filter
            .WithDefaultModelPrefix("Order")
            .AddField<string>(nameof(Contracts.OrderBase.CustomerId), c => c.WithOperators(QueryFilterOperator.EqualityOperators | QueryFilterOperator.StartsWith))
            .AddReferenceDataField<Contracts.OrderStatus>(nameof(Contracts.OrderBase.Status), "StatusCode"))
        .WithOrderBy(orderby => orderby
            .WithDefaultModelPrefix("Order")
            .AddField(nameof(Contracts.OrderBase.CustomerId), c => c.WithDefault().WithAlwaysInclude()));

    public Task<Contracts.Order?> GetAsync(string id) => _ef.Orders.GetAsync(id);

    public Task<DataResult<Contracts.Order>> CreateAsync(Contracts.Order order) => _ef.Orders.CreateAsync(order);

    public Task<DataResult<Contracts.Order>> UpdateAsync(Contracts.Order order) => _ef.Orders.UpdateAsync(order);

    public Task<DataResult> DeleteAsync(string id) => _ef.Orders.DeleteAsync(id);

    public async Task<ItemsResult<Contracts.OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
    {
        var parsed = _queryConfig.Parse(query).ThrowOnError();

        var orders = _ef.Orders.Model.Query();
        return await orders.Where(parsed).OrderBy(parsed).ToMappedItemsResultAsync(x => new Contracts.OrderLite
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            StatusCode = x.StatusCode,
            ChangeLog = new ChangeLog { CreatedBy = x.CreatedBy, CreatedOn = x.CreatedOn, UpdatedBy = x.UpdatedBy, UpdatedOn = x.UpdatedOn }
        }, paging).ConfigureAwait(false);
    }
}