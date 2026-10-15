namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>Maintains the model-independent, rebuildable English full-text projection.</summary>
[ScopedService<ProductLexicalSearchIndex>]
public sealed class ProductLexicalSearchIndex(ProductsEfDb ef, PostgresUnitOfWork unitOfWork)
{
    /// <summary>The weighted English document and normalization recipe.</summary>
    public const string Recipe = "product-lexical-english-v1";
    private static readonly SqlStatement _refresh = SqlStatement.FromResource<ProductLexicalSearchIndex>("ProductLexicalSearchRefresh.pgsql");
    private static readonly SqlStatement _lock = SqlStatement.FromResource<ProductLexicalSearchIndex>("ProductLexicalSearchLock.pgsql");
    private static readonly SqlStatement _isReady = SqlStatement.FromResource<ProductLexicalSearchIndex>("ProductLexicalSearchIsReady.pgsql");
    private static readonly SqlStatement _markReady = SqlStatement.FromResource<ProductLexicalSearchIndex>("ProductLexicalSearchMarkReady.pgsql");
    private static readonly SqlStatement _matches = SqlStatement.FromResource<ProductLexicalSearchIndex>("ProductLexicalSearchMatches.pgsql");
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();
    private readonly PostgresUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();

    /// <summary>Returns one matching row per product, including exact canonical SKU matches independently of tokenization.</summary>
    public IQueryable<SearchMatch> Matches(string text)
        => _ef.SqlQuery<SearchMatch>(_matches, text.ThrowIfNullOrEmpty(), text.Trim().ToUpper(), Recipe);

    /// <summary>Rejects an unprepared recipe, including a partially synchronized initial index.</summary>
    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        if (!await _ef.Database.Statement(_isReady).Param("Recipe", Recipe, dbType: DbType.String).ScalarAsync<bool>(cancellationToken).ConfigureAwait(false))
            throw new TransientException("Product full-text search is not initialized for the configured lexical recipe. Complete a lexical backfill before searching.");
    }

    /// <summary>Commits current text or deletion before inference; never publishes initial readiness.</summary>
    public Task SynchronizeAsync(string id, CancellationToken cancellationToken = default)
        => RefreshAsync(id.ThrowIfNullOrEmpty(), cancellationToken);

    /// <summary>Atomically replaces the full lexical projection and publishes its separate readiness marker.</summary>
    public Task<int> RebuildAsync(CancellationToken cancellationToken = default) => RefreshAsync(null, cancellationToken);

    /// <summary>Serializes publication with catalog writes and other lexical publishers without holding locks during inference.</summary>
    private async Task<int> RefreshAsync(string? id, CancellationToken cancellationToken)
    {
        if (_unitOfWork.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Lexical search synchronization must run outside a parent transaction so publication commits independently of embedding work.");

        return await _unitOfWork.TransactionAsync(async token =>
        {
            await _ef.Database.Statement(_lock).NonQueryAsync(token).ConfigureAwait(false);
            await _ef.Database.Statement(_refresh)
                .Param("ProductId", id, dbType: DbType.String)
                .Param("Recipe", Recipe, dbType: DbType.String)
                .NonQueryAsync(token).ConfigureAwait(false);

            if (id is not null)
                return 0;

            await _ef.Database.Statement(_markReady)
                .Param("Recipe", Recipe, dbType: DbType.String)
                .Param("ReadyOn", Runtime.UtcNow, dbType: DbType.DateTimeOffset)
                .NonQueryAsync(token).ConfigureAwait(false);

            return await _ef.Products.Model.Query().CountAsync(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Represents a unique lexical candidate and its deterministic ranking inputs.</summary>
    public sealed class SearchMatch
    {
        /// <summary>Gets or sets the product identifier.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets whether the entire input matches the canonical SKU.</summary>
        public bool ExactSku { get; set; }
        /// <summary>Gets or sets the PostgreSQL cover-density score.</summary>
        public double Rank { get; set; }
    }
}
