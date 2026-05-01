namespace Contoso.Shopping.Infrastructure.Repositories;

[ScopedService<IBasketRepository>]
public class BasketRepository(ShoppingEfDb ef) : IBasketRepository
{
    private readonly ShoppingEfDb _ef = ef.ThrowIfNull();

    public Task<Result<Domain.Basket>> GetAsync(string id) => Result
        .GoAsync(() => _ef.Baskets.GetWithResultAsync(id))
        .ThenAs(model => BasketMapper.Map(model));

    public Task<Result<Domain.Basket>> CreateAsync(Domain.Basket basket) => Result
        .Go(() =>
        {
            var model = new Persistence.Basket();
            BasketIntoMapper.MapInto(basket, model);
            return SynchronizeItems(basket, model);
        })
        .ThenAsAsync(model => _ef.Baskets.CreateWithResultAsync(model))
        .ThenAs(b => BasketMapper.Map(b));

    public Task<Result<Domain.Basket>> UpdateAsync(Domain.Basket basket) => Result
        .GoAsync(() => _ef.Baskets.GetWithResultAsync(basket.Id))
        .Then(model =>
        {
            BasketIntoMapper.MapInto(basket, model);
            return SynchronizeItems(basket, model);
        })
        .ThenAsAsync(model => _ef.Baskets.UpdateWithResultAsync(model))
        .ThenAs(basket => BasketMapper.Map(basket));

    /// <summary>
    /// Synchronize the items between the domain and model, ensuring the appropriate EntityState is set for each item based on its PersistenceState.
    /// </summary>
    private Result<Persistence.Basket> SynchronizeItems(Domain.Basket basket, Persistence.Basket model)
    {
        _ef.DbContext.Entry(model).State = EntityState.Modified; // Ensure parent is marked as modified.

        for (var i = 0; i < basket.Items.Count; i++)
        {
            switch (basket.Items[i].PersistenceState)
            {
                case PersistenceState.New:
                    var itemModel = new Persistence.BasketItem { BasketId = model.Id };
                    BasketItemIntoMapper.MapInto(basket.Items[i], itemModel);
                    model.Items ??= [];
                    model.Items.Add(itemModel);
                    _ef.DbContext.Entry(itemModel).State = EntityState.Added;
                    break;

                case PersistenceState.Modified:
                    itemModel = model.Items!.SingleOrDefault(x => x.Id == basket.Items[i].Id) ?? throw new ConcurrencyException(/* Edge-case: item no longer found */);
                    if (!ETag.TryCompare(basket.Items[i], itemModel)) // Ensure item etag matches as EF-caches/uses its own read value.
                        return Result.ConcurrencyError();

                    BasketItemIntoMapper.MapInto(basket.Items[i], itemModel);
                    _ef.DbContext.Entry(itemModel).State = EntityState.Modified;
                    break;

                case PersistenceState.Removed:
                    itemModel = model.Items!.SingleOrDefault(x => x.Id == basket.Items[i].Id);
                    if (itemModel is not null)
                        _ef.DbContext.Entry(itemModel).State = EntityState.Deleted;

                    break;
            }
        }

        return model;
    }
}