namespace Contoso.Shopping.Application.Repositories;

public interface IBasketRepository
{
    Task<Result<Domain.Basket>> GetAsync(string id, CancellationToken ct = default);

    Task<Result<Domain.Basket>> CreateAsync(Domain.Basket basket, CancellationToken ct = default);

    Task<Result<Domain.Basket>> UpdateAsync(Domain.Basket basket, CancellationToken ct = default);
}