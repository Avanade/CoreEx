namespace Contoso.Orders.Infrastructure.Repositories;

[ScopedService<IOrderRepository>]
public class OrderRepository(OrdersEfDb ef) : IOrderRepository
{
    private readonly OrdersEfDb _ef = ef.ThrowIfNull();

    public Task<Contracts.Order?> GetAsync(string id, CancellationToken ct = default) => _ef.Orders.GetAsync(id, ct);

    public Task<DataResult<Contracts.Order>> CreateAsync(Contracts.Order order, CancellationToken ct = default) => _ef.Orders.CreateAsync(order, ct);

    public async Task<DataResult<Contracts.Order>> UpdateAsync(Contracts.Order order, CancellationToken ct = default)
    {
        // Load the existing order with its items so EF tracks the child collection before the mapped update.
        var existing = await _ef.DbContext.Set<Persistence.Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            SynchronizeItems(order, existing);

        return await _ef.Orders.UpdateAsync(order, ct).ConfigureAwait(false);
    }

    public Task<DataResult> DeleteAsync(string id, CancellationToken ct = default) => _ef.Orders.DeleteAsync(id, ct);

    public async Task<ItemsResult<Contracts.OrderLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default)
    {
        var parsed = OrderQueryArgsConfig.Default.Parse(query).ThrowOnError();

        var orders = _ef.Orders.Model.Query().IgnoreAutoIncludes();
        return await orders.Where(parsed).OrderBy(parsed).ToMappedItemsResultAsync(x => new Contracts.OrderLite
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            StatusCode = x.StatusCode,
            ChangeLog = new ChangeLog { CreatedBy = x.CreatedBy, CreatedOn = x.CreatedOn, UpdatedBy = x.UpdatedBy, UpdatedOn = x.UpdatedOn }
        }, paging, cancellationToken: ct).ConfigureAwait(false);
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
