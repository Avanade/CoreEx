namespace Contoso.Products.Infrastructure.Repositories;

[ScopedService<IMovementRepository>]
public class MovementRepository(ProductsEfDb ef) : IMovementRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();

    private readonly static QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
        .WithFilter(filter => filter
            .AddField<string>(nameof(Contracts.Movement.ReferenceId), c => c.WithOperators(QueryFilterOperator.EqualityOperators))
            .AddField<string>(nameof(Contracts.Movement.ProductId), c => c.WithOperators(QueryFilterOperator.EqualityOperators))
            .AddReferenceDataField<Contracts.MovementKind>(nameof(Contracts.Movement.Kind), nameof(Persistence.Movement.MovementKindCode))
            .AddReferenceDataField<Contracts.MovementStatus>(nameof(Contracts.Movement.Status), nameof(Persistence.Movement.MovementStatusCode)))
        .WithOrderBy(orderby => orderby
            .AddField(nameof(Contracts.Movement.ReferenceId), c => c.WithDefault().WithAlwaysInclude())
            .AddField(nameof(Contracts.Movement.ProductId), c => c.WithDefault().WithAlwaysInclude()));

    /// <inheritdoc/>
    public async Task<List<Contracts.Movement>> CreateAsync(List<Contracts.Movement> movements)
    {   
        movements.ThrowIfNull().ThrowWhen(movements => movements.Select(x => x.Kind).Distinct().Count() > 1, "All movements must be of the same kind.")
            .ThrowWhen(movements => movements.Any(m => !m.IsQuantityValidForKind), "One or more movements have invalid quantities for their kind.");

        // Process the movements and related inventory items.
        var ids = movements.Select(m => m.ProductId).Distinct();
        var inventoryItems = await _ef.Inventory.QueryTracked().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id!, i => i).ConfigureAwait(false);
        var args = new EfDbArgs { SaveChanges = false };

        foreach (var movement in movements)
        {
            movement.Id = Runtime.NewId();
            var movementModel = MovementMapper.To.Map(movement);

            // Adjust the inventory item for the movement.
            await InventoryAdjustAsync(args, inventoryItems, movementModel).ConfigureAwait(false);

            // Create the movement.
            movement.Id = Runtime.NewId();
            await _ef.Movements.CreateAsync(args, MovementMapper.To.Map(movement)).ConfigureAwait(false);
        }

        // Save changes and return the mutated movements.
        return await SaveAndGetAllMutatedMovements().ConfigureAwait(false);
    }

    /// <summary>
    /// Adjust the inventory item for the movement.
    /// </summary>
    private async Task InventoryAdjustAsync(EfDbArgs args, Dictionary<string, Persistence.Inventory> inventoryItems, Persistence.Movement movement)
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

            await _ef.Inventory.UpdateAsync(args, inventoryItem).ConfigureAwait(false);
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

            await _ef.Inventory.CreateAsync(args, newInventoryItem).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<List<Contracts.Movement>> ConfirmAsync(string referenceId)
    {
        // Get all pending movements for the reference identifier.
        var movements = await _ef.Movements.QueryTracked().Where(m => m.ReferenceId == referenceId && m.MovementStatusCode == Contracts.MovementStatus.Pending).ToListAsync().ConfigureAwait(false);
        if (movements.Count == 0)
            return [];

        // Update the movement status to confirmed for all movements; no inventory adjustment is needed as the inventory was already adjusted during creation.
        var args = new EfDbArgs { SaveChanges = false };
        foreach (var movement in movements)
        {
            movement.MovementStatusCode = Contracts.MovementStatus.Confirmed;
            await _ef.Movements.UpdateAsync(args, movement).ConfigureAwait(false);
        }

        // Save changes and return the mutated movements.
        return await SaveAndGetAllMutatedMovements().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<Contracts.Movement>> CancelAsync(string referenceId)
    {
        // Get all pending movements for the reference identifier.
        var movements = await _ef.Movements.QueryTracked().Where(m => m.ReferenceId == referenceId && m.MovementStatusCode == Contracts.MovementStatus.Pending).ToListAsync().ConfigureAwait(false);
        if (movements.Count == 0)
            return [];

        // Get the related inventory ready to adjust.
        var ids = movements.Select(m => m.ProductId).Distinct();
        var inventoryItems = await _ef.Inventory.QueryTracked().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id!, i => i).ConfigureAwait(false);

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
            await InventoryAdjustAsync(args, inventoryItems, fakeMovement).ConfigureAwait(false);

            // Update movement status to cancelled.
            movement.MovementStatusCode = Contracts.MovementStatus.Canceled;
            await _ef.Movements.UpdateAsync(args, movement).ConfigureAwait(false);
        }

        // Save changes and return the mutated movements.
        return await SaveAndGetAllMutatedMovements().ConfigureAwait(false);
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
    private async Task<List<Contracts.Movement>> SaveAndGetAllMutatedMovements()
    {
        // Save all changes.
        await _ef.DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Return all mutated movements from the change tracker.
        return [.. _ef.DbContext.ChangeTracker.Entries<Persistence.Movement>().Select(m => MovementMapper.From.Map(m.Entity))];
    }

    /// <inheritdoc/>
    public async Task<Contracts.Movement[]> GetAsync(string referenceId)
    {
        var movements = await _ef.Movements.Query().Where(m => m.ReferenceId == referenceId).OrderBy(m => m.Id).ToArrayAsync().ConfigureAwait(false);
        return movements is not null ? [.. movements.Select(m => MovementMapper.From.Map(m))] : [];
    }

    /// <inheritdoc/>
    public Task<JsonElement> QuerySchemaAsync() => Task.FromResult(_queryConfig.ToJsonSchema());

    /// <inheritdoc/>
    public async Task<ItemsResult<Contracts.Movement>> QueryAsync(QueryArgs? query, PagingArgs? paging)
    {
        var parsed = _queryConfig.Parse(query).ThrowOnError();
        var movements = _ef.Movements.Query();
        var q = from m in movements select m;
        return await q.Where(parsed).OrderBy(parsed).ToMappedItemsResultAsync(m => MovementMapper.From.Map(m), paging).ConfigureAwait(false);
    }
}