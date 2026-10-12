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
        var args = new EfDbArgs { SaveChanges = false };

        foreach (var movement in movements)
        {
            // Adjust the inventory item for the movement.
            await InventoryAdjustAsync(movement.ProductId!, movement.KindCode, movement.Quantity, ct).ConfigureAwait(false);

            // Create the movement.
            movement.Id = Runtime.NewId();
            await _ef.Movements.CreateAsync(args, MovementMapper.To.Map(movement), ct).ConfigureAwait(false);
        }

        // Save changes and return the mutated movements.
        return await SaveAndGetAllMutatedMovementsAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Adjusts the inventory item for the movement using a single atomic statement.
    /// </summary>
    /// <remarks>A tracked read-modify-write is deliberately avoided: concurrent movements for the same product would otherwise race on the row version
    /// (<c>xmin</c>) and fail with a concurrency error. The database applies the delta atomically (row-locked within the enclosing transaction), and the
    /// sufficient-quantity guard is evaluated against the latest committed value.</remarks>
    private async Task InventoryAdjustAsync(string productId, string? kind, decimal quantity, CancellationToken ct)
    {
        var db = _ef.DbContext.Database;
        var rows = kind switch
        {
            // Set the absolute quantity on hand; create where it does not yet exist.
            Contracts.MovementKind.Adjust => await db.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "products"."inventory" ("inventory_id", "qty_on_hand") VALUES ({productId}, {quantity})
                ON CONFLICT ("inventory_id") DO UPDATE SET "qty_on_hand" = EXCLUDED."qty_on_hand"
                """, ct).ConfigureAwait(false),

            // Increase the quantity on hand; create where it does not yet exist.
            _ when quantity >= 0 => await db.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "products"."inventory" ("inventory_id", "qty_on_hand") VALUES ({productId}, {quantity})
                ON CONFLICT ("inventory_id") DO UPDATE SET "qty_on_hand" = "inventory"."qty_on_hand" + EXCLUDED."qty_on_hand"
                """, ct).ConfigureAwait(false),

            // Decrease the quantity on hand only where sufficient; no row affected means insufficient (or non-existent) inventory.
            _ => await db.ExecuteSqlInterpolatedAsync($"""
                UPDATE "products"."inventory" SET "qty_on_hand" = "qty_on_hand" + {quantity}
                WHERE "inventory_id" = {productId} AND "qty_on_hand" + {quantity} >= 0
                """, ct).ConfigureAwait(false)
        };

        if (rows == 0)
            throw new BusinessException($"Product '{productId}' does not have sufficient quantity on hand.").WithErrorCode("insufficient-quantity").WithKey(productId);
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

        // Update the movement status to cancelled and adjust inventory back for all movements.
        var args = new EfDbArgs { SaveChanges = false };
        foreach (var movement in movements)
        {
            // Reverse (adjust back) the inventory for the movement using the opposite kind and quantity.
            await InventoryAdjustAsync(movement.ProductId!, CreateReversalMovementKind(movement.MovementKindCode), -movement.Quantity, ct).ConfigureAwait(false);

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
