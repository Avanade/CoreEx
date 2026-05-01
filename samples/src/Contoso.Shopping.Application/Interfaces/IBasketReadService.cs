namespace Contoso.Shopping.Application.Interfaces;

public interface IBasketReadService
{
    /// <summary>
    /// Get the <see cref="Contracts.Basket"/> for the specified <paramref name="basketId"/>
    /// </summary>
    Task<Result<Contracts.Basket>> GetAsync(string basketId);
}