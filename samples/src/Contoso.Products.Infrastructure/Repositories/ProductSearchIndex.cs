namespace Contoso.Products.Infrastructure.Repositories;

/// <summary>Maintains and queries the rebuildable, exact pgvector search projection.</summary>
/// <param name="ef">The Products database accessor.</param>
/// <param name="embeddings">The compatible document/query embedding provider.</param>
/// <param name="referenceData">The reference-data labels used by the canonical document.</param>
/// <param name="unitOfWork">The shared PostgreSQL transaction coordinator.</param>
[ScopedService<ProductSearchIndex>]
public sealed class ProductSearchIndex(ProductsEfDb ef, IProductEmbeddingAdapter embeddings, ReferenceDataOrchestrator referenceData, PostgresUnitOfWork unitOfWork)
{
    private readonly ProductsEfDb _ef = ef.ThrowIfNull();
    private readonly IProductEmbeddingAdapter _embeddings = embeddings.ThrowIfNull();
    private readonly ReferenceDataOrchestrator _referenceData = referenceData.ThrowIfNull();
    private readonly PostgresUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();

    /// <summary>Embeds search text before opening a database snapshot.</summary>
    public async Task<float[]> EmbedQueryAsync(string text, CancellationToken ct = default)
    {
        var vector = (await _embeddings.EmbedQueryAsync(text, ct).ConfigureAwait(false)).ThrowOnError().Value;
        ValidateVector(vector);
        return vector;
    }

    /// <summary>Returns composable exact distances for the selected model profile and document recipe.</summary>
    /// <remarks>Vector text is a bound SQL parameter, not interpolated SQL syntax; no provider-specific EF extension is required.</remarks>
    public IQueryable<SearchMatch> Matches(float[] vector)
    {
        var serialized = SerializeVector(vector);
        return _ef.DbContext.Database.SqlQuery<SearchMatch>($"""
            SELECT product_id AS "Id",
                   embedding <=> CAST({serialized} AS vector(768)) AS "Distance"
            FROM products.product_search
            WHERE profile = {_embeddings.Profile} AND recipe = {ProductSearchDocument.TemplateVersion}
            """);
    }

    /// <summary>Refuses incomplete or stale eligible results rather than silently dropping products.</summary>
    public async Task EnsureFreshAsync(IReadOnlyCollection<Persistence.Product> products, CancellationToken ct = default)
    {
        if (products.Count == 0)
            return;
        await LoadLabelsAsync(ct).ConfigureAwait(false);
        var hashes = await _ef.DbContext.Database.SqlQuery<SearchFingerprint>($"""
            SELECT product_id AS "Id", source_hash AS "SourceHash"
            FROM products.product_search
            WHERE profile = {_embeddings.Profile} AND recipe = {ProductSearchDocument.TemplateVersion}
            """).ToDictionaryAsync(x => x.Id, x => x.SourceHash, ct).ConfigureAwait(false);

        if (products.Any(product => !hashes.TryGetValue(product.Id, out var hash) || hash != Document(product).SourceHash))
            throw new TransientException("Product search is missing or stale. Rebuild the search projection before searching.");
    }

    /// <summary>Builds embeddings outside transactions and atomically replaces this profile's projection.</summary>
    /// <remarks>A short publish-time lock rejects concurrent catalog edits instead of installing stale documents.</remarks>
    public async Task<int> RebuildAsync(CancellationToken ct = default)
    {
        await LoadLabelsAsync(ct).ConfigureAwait(false);
        var products = await _ef.Products.Model.Query().AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct).ConfigureAwait(false);
        var documents = products.Select(product => (Product: product, Document: Document(product))).ToArray();
        var vectors = new List<string>(documents.Length);
        foreach (var item in documents)
        {
            var vector = (await _embeddings.EmbedDocumentAsync(item.Document.Text, ct).ConfigureAwait(false)).ThrowOnError().Value;
            vectors.Add(SerializeVector(vector));
        }

        return await _unitOfWork.TransactionAsync(async token =>
        {
            await _ef.DbContext.Database.ExecuteSqlRawAsync("LOCK TABLE products.product IN SHARE MODE", token).ConfigureAwait(false);
            var current = await _ef.Products.Model.Query().AsNoTracking().OrderBy(p => p.Id).ToListAsync(token).ConfigureAwait(false);
            if (!current.Select(p => (p.Id, Document(p).SourceHash)).SequenceEqual(documents.Select(d => (d.Product.Id, d.Document.SourceHash))))
                throw new ConcurrencyException("The product catalog changed during embedding. Rebuild the search projection again.");

            await _ef.DbContext.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM products.product_search WHERE profile = {_embeddings.Profile} AND recipe = {ProductSearchDocument.TemplateVersion}
                """, token).ConfigureAwait(false);
            for (var i = 0; i < documents.Length; i++)
            {
                var item = documents[i];
                await _ef.DbContext.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO products.product_search (product_id, profile, recipe, source_hash, document, embedding, indexed_on)
                    VALUES ({item.Product.Id}, {_embeddings.Profile}, {ProductSearchDocument.TemplateVersion},
                            {item.Document.SourceHash}, {item.Document.Text}, CAST({vectors[i]} AS vector(768)), {Runtime.UtcNow})
                    """, token).ConfigureAwait(false);
            }

            return documents.Length;
        }, ct).ConfigureAwait(false);
    }

    /// <summary>Loads the labels before synchronous generated navigation getters are used.</summary>
    private async Task LoadLabelsAsync(CancellationToken ct)
        => await _referenceData.PrefetchAsync([typeof(Contracts.Brand), typeof(Contracts.Category), typeof(Contracts.SubCategory)], ct).ConfigureAwait(false);

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

    /// <summary>Represents the indexed content fingerprint without loading vector values.</summary>
    public sealed class SearchFingerprint
    {
        /// <summary>Gets or sets the product identifier.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets the indexed document fingerprint.</summary>
        public string SourceHash { get; set; } = string.Empty;
    }
}
