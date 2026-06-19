namespace Contoso.Shopping.Application.Repositories;

public interface IBasketRepository
{
    Task<Result<Domain.Basket>> GetAsync(string id);

    Task<Result<Domain.Basket>> CreateAsync(Domain.Basket basket);

    Task<Result<Domain.Basket>> UpdateAsync(Domain.Basket basket);
}