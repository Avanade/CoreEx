namespace Contoso.Products.Infrastructure.Repositories;

[ScopedService<IMovementRepository>]
public class MovementRepository(ProductsEfDb ef) : IMovementRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();

    /// <inheritdoc/>
    public async Task<List<Contracts.Movement>> CreateAsync(List<Contracts.Movement> movements, CancellationToken ct = default)
    {   
        movements.ThrowIfNull().ThrowWhen(movements => movements.Select(x => x.Kind).Distinct().Count() > 1, "All movements must be of the same kind.")
            .ThrowWhen(movements => movements.Any(m => !m.IsQuantityValidForKind), "One or more movements have invalid quantities for their kind.");

        // Process the movements and related inventory items.
        var ids = movements.Select(m => m.ProductId).Distinct();
        var inventoryItems = await _ef.Inventory.QueryTracked().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id!, i => i, ct).ConfigureAwait(false);
        var args = new EfDbArgs { SaveChanges = false };

        foreach (var movement in movements)
        {
            movement.Id = Runtime.NewId();
            var movementModel = MovementMapper.To.Map(movement);

            // Adjust the inventory item for the movement.
            await InventoryAdjustAsync(args, inventoryItems, movementModel, ct).ConfigureAwait(false);

            // Create the movement.
            movement.Id = Runtime.NewId();
            await _ef.Movements.CreateAsync(args, MovementMapper.To.Map(movement), ct).ConfigureAwait(false);
        }

        // Save changes and return the mutated movements.
        return await SaveAndGetAllMutatedMovementsAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Adjust the inventory item for the movement.
    /// </summary>
    private async Task InventoryAdjustAsync(EfDbArgs args, Dictionary<string, Persistence.Inventory> inventoryItems, Persistence.Movement movement, CancellationToken ct)
    {
        // Create/adjust the inventory item for the movement.
        if (inventoryItems.TryGetValue(movement.ProductId!, out var inventoryItem))
        {
            if (movement.MovementKindCode == Contracts.MovementKind.Adjust)
                inventoryItem.QtyOnHand = movement.Quantity;
            else
                inventoryItem.QtyOnHand += movement.Quantity;

            if (inventoryItem.QtyOnHand < 0)
                throw new BusinessException($"Product '{movement.ProductId}' does not have sufficient quantity on hand.").WithErrorCode("insufficient-quantity").WithKey(movement.ProductId);

            await _ef.Inventory.UpdateAsync(args, inventoryItem, ct).ConfigureAwait(false);
        }
        else
        {
            if (movement.MovementKindCode == Contracts.MovementKind.Issue)
                throw new BusinessException($"Product '{movement.ProductId}' does not have sufficient quantity on hand.").WithErrorCode("insufficient-quantity").WithKey(movement.ProductId);

            var newInventoryItem = new Persistence.Inventory
            {
                Id = movement.ProductId,
                QtyOnHand = movement.Quantity
            };

            await _ef.Inventory.CreateAsync(args, newInventoryItem, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<List<Contracts.Movement>> ConfirmAsync(string referenceId, CancellationToken ct = default)
    {
        // Get all pending movements for the reference identifier.
        var movements = await _ef.Movements.QueryTracked().Where(m => m.ReferenceId == referenceId && m.MovementStatusCode == Contracts.MovementStatus.Pending).ToListAsync(ct).ConfigureAwait(false);
        if (movements.Count == 0)
            return [];

        // Update the movement status to confirmed for all movements; no inventory adjustment is needed as the inventory was already adjusted during creation.
        var args = new EfDbArgs { SaveChanges = false };
        foreach (var movement in movements)
        {
            movement.MovementStatusCode = Contracts.MovementStatus.Confirmed;
            await _ef.Movements.UpdateAsync(args, movement, ct).ConfigureAwait(false);
        }

        // Save changes and return the mutated movements.
        return await SaveAndGetAllMutatedMovementsAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<Contracts.Movement>> CancelAsync(string referenceId, CancellationToken ct = default)
    {
        // Get all pending movements for the reference identifier.
        var movements = await _ef.Movements.QueryTracked().Where(m => m.ReferenceId == referenceId && m.MovementStatusCode == Contracts.MovementStatus.Pending).ToListAsync(ct).ConfigureAwait(false);
        if (movements.Count == 0)
            return [];

        // Get the related inventory ready to adjust.
        var ids = movements.Select(m => m.ProductId).Distinct();
        var inventoryItems = await _ef.Inventory.QueryTracked().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id!, i => i, ct).ConfigureAwait(false);

        // Update the movement status to cancelled and adjust inventory back for all movements.
        var args = new EfDbArgs { SaveChanges = false };
        foreach (var movement in movements)
        {
            // Create a fake movement with opposite quantity to adjust the inventory back.
            var fakeMovement = new Persistence.Movement
            {
                ProductId = movement.ProductId,
                Quantity = -movement.Quantity,
                MovementKindCode = CreateReversalMovementKind(movement.MovementKindCode) 
            };

            // Reverse (adjust back) the inventory for the movement.
            await InventoryAdjustAsync(args, inventoryItems, fakeMovement, ct).ConfigureAwait(false);

            // Update movement status to cancelled.
            movement.MovementStatusCode = Contracts.MovementStatus.Canceled;
            await _ef.Movements.UpdateAsync(args, movement, ct).ConfigureAwait(false);
        }

        // Save changes and return the mutated movements.
        return await SaveAndGetAllMutatedMovementsAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Create a reversal movement kind for the given movement kind.
    /// </summary>
    private static string CreateReversalMovementKind(string? kind) => kind switch
    {
        Contracts.MovementKind.Issue => Contracts.MovementKind.Receive,
        Contracts.MovementKind.Receive => Contracts.MovementKind.Issue,
        _ => throw new InvalidOperationException($"Unsupported movement kind: {kind}")
    };

    /// <summary>
    /// Saves all the changes and returns the mutated movements mapping to the contract version.
    /// </summary>
    private async Task<List<Contracts.Movement>> SaveAndGetAllMutatedMovementsAsync(CancellationToken ct)
    {
        // Save all changes.
        await _ef.DbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        // Return all mutated movements from the change tracker.
        return [.. _ef.DbContext.ChangeTracker.Entries<Persistence.Movement>().Select(m => MovementMapper.From.Map(m.Entity))];
    }

    /// <inheritdoc/>
    public async Task<Contracts.Movement[]> GetAsync(string referenceId, CancellationToken ct = default)
    {
        var movements = await _ef.Movements.Query().Where(m => m.ReferenceId == referenceId).OrderBy(m => m.Id).ToArrayAsync(ct).ConfigureAwait(false);
        return movements is not null ? [.. movements.Select(m => MovementMapper.From.Map(m))] : [];
    }

    /// <inheritdoc/>
    public Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default) => Task.FromResult(MovementQueryArgsConfig.Default.ToJsonSchema());

    /// <inheritdoc/>
    public async Task<ItemsResult<Contracts.Movement>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default)
    {
        var parsed = MovementQueryArgsConfig.Default.Parse(query).ThrowOnError();
        var movements = _ef.Movements.Query();
        var q = from m in movements select m;
        return await q.Where(parsed).OrderBy(parsed).ToMappedItemsResultAsync(m => MovementMapper.From.Map(m), paging, cancellationToken: ct).ConfigureAwait(false);
    }
}
