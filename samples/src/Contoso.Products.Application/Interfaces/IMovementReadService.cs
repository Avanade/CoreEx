namespace Contoso.Products.Application.Interfaces;

public interface IMovementReadService
{
    /// <summary>
    /// Gets the movements with the specified reference identifier (see <see cref="Contracts.Movement.ReferenceId"/>).
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <returns>The movements.</returns>
    Task<Contracts.Movement[]> GetAsync(string referenceId);

    /// <summary>
    /// Gets the <see cref="QueryArgs"/> schema.
    /// </summary>
    Task<JsonElement> QuerySchemaAsync();

    /// <summary>
    /// Queries the movements.
    /// </summary>
    /// <param name="query">The <see cref="QueryArgs"/>.</param>
    /// <param name="paging">The<see cref="PagingArgs"/>.</param>
    /// <returns>The resulting movements.</returns>
    Task<ItemsResult<Contracts.Movement>> QueryAsync(QueryArgs? query, PagingArgs? paging);
}