namespace Contoso.Products.Application.Repositories;

public interface IMovementRepository
{
    /// <summary>
    /// Creates new movement records (as specified) and applies the necessary adjustments to the underlying inventory based on the underlying <see cref="Contracts.Movement.Kind"/>.
    /// </summary>
    /// <param name="movements">The list of movements to be created and adjusted.</param>
    /// <returns>The mutated movements.</returns>
    /// <remarks>All movements must be of the same <see cref="MovementKind"/>.</remarks>
    Task<List<Contracts.Movement>> CreateAsync(List<Contracts.Movement> movements);

    /// <summary>
    /// Confirms the movements with the specified reference identifier (see <see cref="Contracts.Movement.ReferenceId"/>).
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <returns>The mutated movements.</returns>
    /// <remarks>The movements must be in a pending state to be confirmed.
    /// <para>No underlying inventory adjustment(s) are required as the inventory was already adjusted during creation.</para></remarks>
    Task<List<Contracts.Movement>> ConfirmAsync(string referenceId);

    /// <summary>
    /// Cancels the movements with the specified reference identifier (see <see cref="Contracts.Movement.ReferenceId"/>) and applies the necessary adjustments to the underlying inventory based on the underlying <see cref="Contracts.Movement.Kind"/>.
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <returns>The mutated movements.</returns>
    /// <remarks>The movements must be in a pending state to be cancelled.</remarks>
    Task<List<Contracts.Movement>> CancelAsync(string referenceId);

    /// <summary>
    /// Gets the movements with the specified reference identifier (see <see cref="Contracts.Movement.ReferenceId"/>).
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <returns>The movements.</returns>
    Task<Contracts.Movement[]> GetAsync(string referenceId);

    /// <summary>
    /// Gets the <see cref="QueryArgs"/> schema.
    /// </summary>
    /// <returns></returns>
    Task<JsonElement> QuerySchemaAsync();

    /// <summary>
    /// Queries the movements.
    /// </summary>
    /// <param name="query">The <see cref="QueryArgs"/>.</param>
    /// <param name="paging">The<see cref="PagingArgs"/>.</param>
    /// <returns>The resulting movements.</returns>
    Task<ItemsResult<Contracts.Movement>> QueryAsync(QueryArgs? query, PagingArgs? paging);
}