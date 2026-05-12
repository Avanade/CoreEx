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

    public async Task<DataResult<Contracts.Order>> UpdateAsync(Contracts.Order order)
    {
        // Load the existing order with its items so EF tracks the child collection before the mapped update.
        var existing = await _ef.DbContext.Set<Persistence.Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id)
            .ConfigureAwait(false);

        if (existing is not null)
            SynchronizeItems(order, existing);

        return await _ef.Orders.UpdateAsync(order).ConfigureAwait(false);
    }

    public Task<DataResult> DeleteAsync(string id) => _ef.Orders.DeleteAsync(id);

    public async Task<ItemsResult<Contracts.OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
    {
        var parsed = _queryConfig.Parse(query).ThrowOnError();

        var orders = _ef.Orders.Model.Query().IgnoreAutoIncludes();
        return await orders.Where(parsed).OrderBy(parsed).ToMappedItemsResultAsync(x => new Contracts.OrderLite
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            StatusCode = x.StatusCode,
            ChangeLog = new ChangeLog { CreatedBy = x.CreatedBy, CreatedOn = x.CreatedOn, UpdatedBy = x.UpdatedBy, UpdatedOn = x.UpdatedOn }
        }, paging).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronizes the items collection between the contract order and the tracked persistence model.
    /// </summary>
    private void SynchronizeItems(Contracts.Order order, Persistence.Order model)
    {
        var newItems = order.Items ?? [];
        var existingItems = model.Items?.ToList() ?? [];

        // Remove items that are no longer present in the updated order.
        var toRemove = existingItems.Where(e => !newItems.Any(n => n.Id == e.Id)).ToList();
        foreach (var item in toRemove)
            _ef.DbContext.Entry(item).State = EntityState.Deleted;

        // Add new items or update existing items.
        foreach (var newItem in newItems)
        {
            var existingItem = existingItems.FirstOrDefault(e => e.Id == newItem.Id);
            if (existingItem is null)
            {
                // New item: preserve the client-supplied identity used for future updates.
                var addedItem = new Persistence.OrderItem
                {
                    Id = newItem.Id!,
                    OrderId = order.Id!,
                    ProductId = newItem.ProductId!,
                    Quantity = newItem.Quantity,
                    UnitPrice = newItem.UnitPrice
                };
                model.Items ??= [];
                model.Items.Add(addedItem);
                _ef.DbContext.Entry(addedItem).State = EntityState.Added;
            }
            else
            {
                // Existing item: update its properties.
                existingItem.ProductId = newItem.ProductId!;
                existingItem.Quantity = newItem.Quantity;
                existingItem.UnitPrice = newItem.UnitPrice;
                _ef.DbContext.Entry(existingItem).State = EntityState.Modified;
            }
        }
    }
}