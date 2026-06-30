namespace Contoso.Products.Application;

[ScopedService<IMovementReadService>]
public class MovementReadService(IMovementRepository movementRepository) : IMovementReadService
{
    private readonly IMovementRepository _movementRepository = movementRepository.ThrowIfNull();

    /// <inheritdoc/>
    public Task<Contracts.Movement[]> GetAsync(string referenceId, CancellationToken ct = default) => _movementRepository.GetAsync(referenceId, ct);

    /// <inheritdoc/>
    public Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default) => _movementRepository.QuerySchemaAsync(ct);

    /// <inheritdoc/>
    public Task<ItemsResult<Contracts.Movement>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default) => _movementRepository.QueryAsync(query, paging, ct);
}