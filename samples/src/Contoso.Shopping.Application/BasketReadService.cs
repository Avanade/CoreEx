namespace Contoso.Shopping.Application;

[ScopedService<IBasketReadService>]
public class BasketReadService(IBasketRepository repository) : IBasketReadService
{
    private readonly IBasketRepository _repository = repository.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Result<Basket>> GetAsync(string basketId)
        => Result.GoAsync(() => _repository.GetAsync(basketId))
                 .ThenAs(b => BasketMapper.Map(b));
}