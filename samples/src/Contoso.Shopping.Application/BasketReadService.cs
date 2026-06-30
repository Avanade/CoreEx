namespace Contoso.Shopping.Application;

[ScopedService<IBasketReadService>]
public class BasketReadService(IBasketRepository repository) : IBasketReadService
{
    private readonly IBasketRepository _repository = repository.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Result<Basket>> GetAsync(string basketId, CancellationToken ct = default)
        => Result.GoAsync(() => _repository.GetAsync(basketId, ct))
                 .ThenAs(b => BasketMapper.Map(b));
}