namespace Contoso.Products.Infrastructure.Repositories;

[ScopedService<IProductRepository>]
public class ProductRepository(ProductsEfDb ef, ProductSearchIndex searchIndex, PostgresUnitOfWork unitOfWork) : IProductRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();
    private readonly ProductSearchIndex _searchIndex = searchIndex.ThrowIfNull();
    private readonly PostgresUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();

    public Task<Contracts.Product?> GetAsync(string id, CancellationToken ct = default) => _ef.Products.GetAsync(id, ct);

    public Task<DataResult<Contracts.Product>> CreateAsync(Contracts.Product product, CancellationToken ct = default) => _ef.Products.CreateAsync(product, ct);

    public Task<DataResult<Contracts.Product>> UpdateAsync(Contracts.Product product, CancellationToken ct = default) => _ef.Products.UpdateAsync(product, ct);

    public Task<DataResult> DeleteAsync(string id, CancellationToken ct = default) => _ef.Products.DeleteAsync(id, ct);

    public Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default) => Task.FromResult(ProductQueryArgsConfig.Default.ToJsonSchema());

    public async Task<ItemsResult<Contracts.ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default)
    {
        var parsed = ProductQueryArgsConfig.Default.Parse(query).ThrowOnError();
        var products = _ef.Products.Model.Query();
        if (query?.IsIncludeInactive is false)
            products = products.Where(p => !p.IsInactive);

        var subcats = _ef.SubCategories.Query();
        var inventory = _ef.Inventory.Query();

        var q =
            from p in products
            join sc in subcats on p.SubCategoryCode equals sc.Code into scg
            from sc in scg.DefaultIfEmpty()
            join i in inventory on p.Id equals i.Id into ig
            from i in ig.DefaultIfEmpty()
            select new
            {
                Product = p,
                SubCategoryCode = sc.Code,
                sc.CategoryCode,
                QtyOnHand = i == null ? 0 : i.QtyOnHand
            };

        q = q.Where(parsed);
        float[]? vector = null;
        if (parsed.IsRankedSearch)
        {
            EnsureSnapshotIsolation();
            // Fail fast before inference, then recheck in the retrieval snapshot to cover writes during inference.
            await _searchIndex.EnsureFreshAsync(await q.Select(x => x.Product).AsNoTracking().ToListAsync(ct).ConfigureAwait(false), ct).ConfigureAwait(false);
            vector = await _searchIndex.EmbedQueryAsync((parsed.SearchResult?.Text).ThrowIfNull(), ct).ConfigureAwait(false);
        }

        return vector is null
            ? await ReadAsync(ct).ConfigureAwait(false)
            : await _unitOfWork.TransactionAsync(_unitOfWork.Database.DbArgs with { IsolationLevel = IsolationLevel.RepeatableRead }, ReadAsync, ct).ConfigureAwait(false);

        async Task<ItemsResult<Contracts.ProductLite>> ReadAsync(CancellationToken token)
        {
            if (vector is not null)
            {
                EnsureSnapshotIsolation();
                await _searchIndex.EnsureFreshAsync(await q.Select(x => x.Product).AsNoTracking().ToListAsync(token).ConfigureAwait(false), token).ConfigureAwait(false);
                q = (from candidate in q
                     join match in _searchIndex.Matches(vector) on candidate.Product.Id equals match.Id
                     orderby match.Distance, candidate.Product.Id
                     select candidate);
            }
            else
                q = q.OrderBy(parsed);

            return await q.ToMappedItemsResultAsync(x => new Contracts.ProductLite
            {
                Id = x.Product.Id,
                Sku = x.Product.Sku,
                Text = x.Product.Text,
                Description = x.Product.Description,
                CategoryCode = x.CategoryCode,
                SubCategoryCode = x.SubCategoryCode,
                BrandCode = x.Product.BrandCode,
                UnitOfMeasureCode = x.Product.UnitOfMeasureCode,
                Price = x.Product.Price,
                IsInactive = x.Product.IsInactive,
                IsNonStocked = x.Product.IsNonStocked,
                QtyOnHand = x.QtyOnHand
            }, paging, cancellationToken: token).ConfigureAwait(false);
        }
    }

    /// <summary>Rejects a parent transaction that cannot provide a consistent PostgreSQL read snapshot.</summary>
    private void EnsureSnapshotIsolation()
    {
        if (_unitOfWork.Database.CurrentTransaction is { } transaction && transaction.IsolationLevel is not (IsolationLevel.RepeatableRead or IsolationLevel.Serializable))
            throw new InvalidOperationException("Semantic product search requires a RepeatableRead or Serializable parent transaction.");
    }

    public Task<Dictionary<string, Contracts.ProductReserve>> GetForReservationAsync(string[] ids, CancellationToken ct = default)
    {
        var products = _ef.Products.Model.Query();
        var list = ids.ToList();    // PostgreSQL EF has issues translating array.Contains, but can do list.Contains; see: https://github.com/npgsql/efcore.pg/issues/3461.

        var q =
            from p in products
            where list.Contains(p.Id)
            select new Contracts.ProductReserve
            {
                Id = p.Id!,
                UnitOfMeasureCode = p.UnitOfMeasureCode!,
                IsInactive = p.IsInactive,
                IsNonStocked = p.IsNonStocked
            };

        return q.ToDictionaryAsync(x => x.Id, x => x, ct);
    }
}
