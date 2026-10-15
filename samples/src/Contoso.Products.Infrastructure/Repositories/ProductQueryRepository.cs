namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>Executes product queries, including semantic and lexical retrieval.</summary>
/// <param name="ef">The Products database accessor.</param>
/// <param name="searchIndex">The semantic search projection.</param>
/// <param name="unitOfWork">The PostgreSQL transaction coordinator.</param>
/// <param name="lexicalIndex">The lexical search projection.</param>
/// <param name="configuration">The server-side query and search configuration.</param>
[ScopedService<IProductQueryRepository>]
public class ProductQueryRepository(ProductsEfDb ef, ProductSearchIndex searchIndex, PostgresUnitOfWork unitOfWork, ProductLexicalSearchIndex lexicalIndex, IConfiguration configuration) : IProductQueryRepository
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();
    private readonly ProductSearchIndex _searchIndex = searchIndex.ThrowIfNull();
    private readonly PostgresUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly ProductLexicalSearchIndex _lexicalIndex = lexicalIndex.ThrowIfNull();
    private readonly ProductQueryArgsConfig _queryConfig = ProductQueryArgsConfig.FromConfiguration(configuration.ThrowIfNull());

    /// <inheritdoc/>
    public Task<JsonElement> QuerySchemaAsync(CancellationToken cancellationToken = default) => Task.FromResult(_queryConfig.ToJsonSchema());

    /// <inheritdoc/>
    public async Task<ItemsResult<Contracts.ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken = default)
    {
        var parsed = _queryConfig.Parse(query).ThrowOnError();
        var isLexical = _queryConfig.SearchParser.Mode == QuerySearchMode.FullText;
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
            if (!isLexical)
            {
                await _searchIndex.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
                vector = await _searchIndex.EmbedQueryAsync((parsed.SearchResult?.Text).ThrowIfNull(), cancellationToken).ConfigureAwait(false);
            }
        }

        return !parsed.IsRankedSearch
            ? await ReadAsync(cancellationToken).ConfigureAwait(false)
            : await _unitOfWork.TransactionAsync(_unitOfWork.Database.DbArgs with { IsolationLevel = IsolationLevel.RepeatableRead }, ReadAsync, cancellationToken).ConfigureAwait(false);

        async Task<ItemsResult<Contracts.ProductLite>> ReadAsync(CancellationToken cancellationToken)
        {
            if (parsed.IsRankedSearch && isLexical)
            {
                EnsureSnapshotIsolation();
                await _lexicalIndex.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
                q = from candidate in q
                    join match in _lexicalIndex.Matches((parsed.SearchResult?.Text).ThrowIfNull()) on candidate.Product.Id equals match.Id
                    orderby match.ExactSku descending, match.Rank descending, candidate.Product.Id
                    select candidate;
            }
            else if (vector is not null)
            {
                EnsureSnapshotIsolation();
                await _searchIndex.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
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
            }, paging, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Rejects a parent transaction that cannot provide a consistent PostgreSQL read snapshot.</summary>
    private void EnsureSnapshotIsolation()
    {
        if (_unitOfWork.Database.CurrentTransaction is { } transaction && transaction.IsolationLevel is not (IsolationLevel.RepeatableRead or IsolationLevel.Serializable))
            throw new InvalidOperationException($"{(_queryConfig.SearchParser.Mode == QuerySearchMode.FullText ? "Full-text" : "Semantic")} product search requires a RepeatableRead or Serializable parent transaction.");
    }
}
