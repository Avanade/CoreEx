namespace Contoso.Products.Application;

[ScopedService<IMovementReadService>]
public class MovementReadService(IMovementRepository movementRepository) : IMovementReadService
{
    private readonly IMovementRepository _movementRepository = movementRepository.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Contracts.Movement[]> GetAsync(string referenceId) => _movementRepository.GetAsync(referenceId);

    /// <inheritdoc/>
    public Task<JsonElement> QuerySchemaAsync() => _movementRepository.QuerySchemaAsync();

    /// <inheritdoc/>
    public Task<ItemsResult<Contracts.Movement>> QueryAsync(QueryArgs? query, PagingArgs? paging) => _movementRepository.QueryAsync(query, paging);
}