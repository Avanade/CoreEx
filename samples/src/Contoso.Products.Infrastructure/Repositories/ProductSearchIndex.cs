namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>Maintains and queries the rebuildable, exact pgvector search projection.</summary>
/// <param name="ef">The Products database accessor.</param>
/// <param name="embeddings">The compatible document/query embedding provider.</param>
/// <param name="referenceData">The reference-data labels used by the canonical document.</param>
/// <param name="unitOfWork">The shared PostgreSQL transaction coordinator.</param>
/// <param name="logger">The indexing diagnostics logger.</param>
/// <param name="lexicalIndex">The independently committed, model-free text projection.</param>
[ScopedService<ProductSearchIndex>]
public sealed class ProductSearchIndex(ProductsEfDb ef, IProductEmbeddingAdapter embeddings, ReferenceDataOrchestrator referenceData, PostgresUnitOfWork unitOfWork, ILogger<ProductSearchIndex> logger, ProductLexicalSearchIndex lexicalIndex)
{
    private static readonly SqlStatement _isReady = SqlStatement.FromResource<ProductSearchIndex>("ProductSearchIsReady.pgsql");
    private static readonly SqlStatement _sourceHash = SqlStatement.FromResource<ProductSearchIndex>("ProductSearchSourceHash.pgsql");
    private static readonly SqlStatement _lockSource = SqlStatement.FromResource<ProductSearchIndex>("ProductSearchLockSource.pgsql");
    private static readonly SqlStatement _deleteProduct = SqlStatement.FromResource<ProductSearchIndex>("ProductSearchDeleteProduct.pgsql");
    private static readonly SqlStatement _deleteProfile = SqlStatement.FromResource<ProductSearchIndex>("ProductSearchDeleteProfile.pgsql");
    private static readonly SqlStatement _markReady = SqlStatement.FromResource<ProductSearchIndex>("ProductSearchMarkReady.pgsql");
    private static readonly SqlStatement _upsert = SqlStatement.FromResource<ProductSearchIndex>("ProductSearchUpsert.pgsql");
    private static readonly SqlStatement _matches = SqlStatement.FromResource<ProductSearchIndex>("ProductSearchMatches.pgsql");

    private readonly ProductsEfDb _ef = ef.ThrowIfNull();
    private readonly IProductEmbeddingAdapter _embeddings = embeddings.ThrowIfNull();
    private readonly ReferenceDataOrchestrator _referenceData = referenceData.ThrowIfNull();
    private readonly PostgresUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly ILogger<ProductSearchIndex> _logger = logger.ThrowIfNull();
    private readonly ProductLexicalSearchIndex _lexicalIndex = lexicalIndex.ThrowIfNull();

    /// <summary>Embeds search text before opening a database snapshot.</summary>
    public async Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var vector = (await _embeddings.EmbedQueryAsync(text, cancellationToken).ConfigureAwait(false)).ThrowOnError().Value;
            ValidateVector(vector);
            return vector;
        }
        catch (Exception ex) when (ex is HttpRequestException or Polly.Timeout.TimeoutRejectedException || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new TransientException("Product search is temporarily unavailable. Please try again.", ex);
        }
    }

    /// <summary>Returns composable exact distances for the selected model profile and document recipe.</summary>
    /// <remarks>Kept as composable EF SQL so live filters, ranking, paging and count execute together in PostgreSQL. Vector text is a bound parameter.</remarks>
    public IQueryable<SearchMatch> Matches(float[] vector)
    {
        var serialized = SerializeVector(vector);
        return _ef.SqlQuery<SearchMatch>(_matches, serialized, _embeddings.Profile, ProductSearchDocument.TemplateVersion);
    }

    /// <summary>Distinguishes a completed, possibly empty index from one never prepared for this profile.</summary>
    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        var ready = await _ef.Database.Statement(_isReady)
            .Param("Profile", _embeddings.Profile, dbType: DbType.String)
            .Param("Recipe", ProductSearchDocument.TemplateVersion, dbType: DbType.String)
            .ScalarAsync<bool>(cancellationToken).ConfigureAwait(false);

        if (!ready)
            throw new TransientException("Product search is not initialized for the configured model and document recipe. Complete a search backfill before searching.");
    }

    /// <summary>Reconciles current product state, avoiding inference for unchanged documents and removing logical deletions.</summary>
    public async Task SynchronizeAsync(string id, CancellationToken cancellationToken = default)
    {
        id = id.ThrowIfNullOrEmpty();
        if (_unitOfWork.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Search synchronization must run outside a parent transaction so inference does not hold database locks.");

        await _lexicalIndex.SynchronizeAsync(id, cancellationToken).ConfigureAwait(false);
        await LoadLabelsAsync(cancellationToken).ConfigureAwait(false);
        var product = await _ef.Products.Model.Query().AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
        var document = product is null ? null : Document(product);
        string? vector = null;

        if (document is not null)
        {
            var hash = await ReadSourceHashAsync(id, cancellationToken).ConfigureAwait(false);
            if (hash != document.SourceHash)
                vector = await EmbedDocumentAsync(document.Text, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.TransactionAsync(async token =>
        {
            // Shares the rebuild's publish lock; source writes cannot race the final check and publication.
            await _ef.Database.Statement(_lockSource).NonQueryAsync(token).ConfigureAwait(false);
            var current = await _ef.Products.Model.Query().AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, token).ConfigureAwait(false);

            if (current is null)
            {
                await _ef.Database.Statement(_deleteProduct).Param("ProductId", id, dbType: DbType.String).NonQueryAsync(token).ConfigureAwait(false);

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Removed search projections for deleted or missing product {ProductId}.", id);
                return;
            }

            if (document is null || Document(current).SourceHash != document.SourceHash)
                throw new TransientException("The product changed during search indexing. Retry using its current state.");

            if (vector is not null)
            {
                await UpsertAsync(id, document, vector, token).ConfigureAwait(false);
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("Indexed product {ProductId} for search profile {Profile} and recipe {Recipe}.", id, _embeddings.Profile, ProductSearchDocument.TemplateVersion);
            }
            else
            {
                // A concurrent rebuild may have removed the row after the initial hash comparison.
                var hash = await ReadSourceHashAsync(id, token).ConfigureAwait(false);
                if (hash != document.SourceHash)
                    throw new TransientException("The search projection changed during indexing. Retry using its current state.");

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Search document for product {ProductId} is already current.", id);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Builds embeddings outside transactions and atomically replaces this profile's projection.</summary>
    /// <remarks>A short publish-time lock rejects concurrent catalog edits instead of installing stale documents.</remarks>
    public Task<int> RebuildAsync(CancellationToken cancellationToken = default) => RebuildAsync(null, cancellationToken);

    /// <summary>Rebuilds with synchronous, ordered progress notifications; counts represent validated embeddings, not committed index rows.</summary>
    public async Task<int> RebuildAsync(Action<RebuildProgress>? progress, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Invoke(new("Rebuilding lexical index"));
        await _lexicalIndex.RebuildAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Invoke(new("Rebuilding lexical index", IsComplete: true));
        progress?.Invoke(new("Loading search documents"));
        await LoadLabelsAsync(cancellationToken).ConfigureAwait(false);
        var products = await _ef.Products.Model.Query().AsNoTracking().OrderBy(p => p.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var documents = products.Select(product => (Product: product, Document: Document(product))).ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Invoke(new("Loading search documents", IsComplete: true));
        var vectors = new List<string>(documents.Length);
        progress?.Invoke(new("Embedding products", 0, documents.Length));
        foreach (var item in documents)
        {
            vectors.Add(await EmbedDocumentAsync(item.Document.Text, cancellationToken).ConfigureAwait(false));
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Invoke(new("Embedding products", vectors.Count, documents.Length));
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Invoke(new("Embedding products", vectors.Count, documents.Length, IsComplete: true));
        progress?.Invoke(new("Publishing semantic index"));
        var count = await _unitOfWork.TransactionAsync(async token =>
        {
            await _ef.Database.Statement(_lockSource).NonQueryAsync(token).ConfigureAwait(false);
            var current = await _ef.Products.Model.Query().AsNoTracking().OrderBy(p => p.Id).ToListAsync(token).ConfigureAwait(false);
            if (!current.Select(p => (p.Id, Document(p).SourceHash)).SequenceEqual(documents.Select(d => (d.Product.Id, d.Document.SourceHash))))
                throw new ConcurrencyException("The product catalog changed during embedding. Rebuild the search projection again.");

            await _ef.Database.Statement(_deleteProfile)
                .Param("Profile", _embeddings.Profile, dbType: DbType.String)
                .Param("Recipe", ProductSearchDocument.TemplateVersion, dbType: DbType.String)
                .NonQueryAsync(token).ConfigureAwait(false);

            for (var i = 0; i < documents.Length; i++)
            {
                var item = documents[i];
                await UpsertAsync(item.Product.Id, item.Document, vectors[i], token).ConfigureAwait(false);
            }

            await _ef.Database.Statement(_markReady)
                .Param("Profile", _embeddings.Profile, dbType: DbType.String)
                .Param("Recipe", ProductSearchDocument.TemplateVersion, dbType: DbType.String)
                .Param("ReadyOn", Runtime.UtcNow, dbType: DbType.DateTimeOffset)
                .NonQueryAsync(token).ConfigureAwait(false);

            return documents.Length;
        }, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Invoke(new("Publishing semantic index", IsComplete: true));
        return count;
    }

    /// <summary>Describes actual rebuild work; completion applies only to the named stage.</summary>
    public sealed record RebuildProgress(string Stage, int? Completed = null, int? Total = null, bool IsComplete = false);

    /// <summary>Reads the stored fingerprint, or null when this product has no compatible projection.</summary>
    private Task<string?> ReadSourceHashAsync(string id, CancellationToken cancellationToken)
        => _ef.Database.Statement(_sourceHash)
            .Param("ProductId", id, dbType: DbType.String)
            .Param("Profile", _embeddings.Profile, dbType: DbType.String)
            .Param("Recipe", ProductSearchDocument.TemplateVersion, dbType: DbType.String)
            .ScalarAsync<string?>(cancellationToken);

    /// <summary>Publishes a validated vector and its exact source document for this profile and recipe.</summary>
    private Task<int> UpsertAsync(string id, ProductSearchDocument document, string vector, CancellationToken cancellationToken)
        => _ef.Database.Statement(_upsert)
            .Param("ProductId", id, dbType: DbType.String)
            .Param("Profile", _embeddings.Profile, dbType: DbType.String)
            .Param("Recipe", ProductSearchDocument.TemplateVersion, dbType: DbType.String)
            .Param("SourceHash", document.SourceHash, dbType: DbType.String)
            .Param("Document", document.Text, dbType: DbType.String)
            .Param("Embedding", vector, dbType: DbType.String)
            .Param("IndexedOn", Runtime.UtcNow, dbType: DbType.DateTimeOffset)
            .NonQueryAsync(cancellationToken);

    /// <summary>Preserves model diagnostics while making dependency failures retriable for indexing.</summary>
    private async Task<string> EmbedDocumentAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            return SerializeVector((await _embeddings.EmbedDocumentAsync(text, cancellationToken).ConfigureAwait(false)).ThrowOnError().Value);
        }
        catch (Exception ex) when (ex is HttpRequestException or Polly.Timeout.TimeoutRejectedException || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new TransientException("Product search indexing is temporarily unavailable. Please retry.", ex);
        }
    }

    /// <summary>Loads the labels before synchronous generated navigation getters are used.</summary>
    private async Task LoadLabelsAsync(CancellationToken cancellationToken)
        => await _referenceData.PrefetchAsync([typeof(Contracts.Brand), typeof(Contracts.Category), typeof(Contracts.SubCategory)], cancellationToken).ConfigureAwait(false);

    /// <summary>Uses the same category derivation as the application contract.</summary>
    private static ProductSearchDocument Document(Persistence.Product product)
        => ProductSearchDocument.Create(ProductMapper.From.Map(product));

    /// <summary>Validates vectors at the persistence and query boundaries.</summary>
    private void ValidateVector(float[] vector)
    {
        if (_embeddings.Dimensions != OllamaHttpClient.Dimensions || vector.Length != OllamaHttpClient.Dimensions || vector.Any(x => !float.IsFinite(x)) || !vector.Any(x => x != 0))
            throw new InvalidOperationException("Product search requires a finite, non-zero 768-dimensional embedding.");
    }

    /// <summary>Formats validated vectors independently of locale.</summary>
    private string SerializeVector(float[] vector)
    {
        ValidateVector(vector);
        return $"[{string.Join(",", vector.Select(x => x.ToString("R", CultureInfo.InvariantCulture)))}]";
    }

    /// <summary>Represents an unmapped, scalar SQL projection; the vector stays in PostgreSQL.</summary>
    public sealed class SearchMatch
    {
        /// <summary>Gets or sets the product identifier.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets cosine distance; lower is closer.</summary>
        public double Distance { get; set; }
    }
}
