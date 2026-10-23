namespace CoreEx.Data.Querying;

/// <summary>
/// Defines the <see cref="QuerySearchParser"/> search mode; i.e. how the free-text <c>$search</c> is to be interpreted and executed.
/// </summary>
/// <remarks>Only <see cref="Keyword"/> is executed by <i>CoreEx</i> itself (as a provider-agnostic dynamic LINQ predicate). All other modes are <i>ranked</i> and are executed by the
/// repository using the underlying data source capabilities (e.g. PostgreSQL <c>tsvector</c>/<c>pgvector</c>, SQL Server full-text, Azure Cosmos DB <c>VectorDistance</c>/<c>FullTextScore</c>, Azure AI Search).
/// Where ranked, results are ordered by relevance and an explicit <c>$orderby</c> is not supported.</remarks>
public enum QuerySearchMode
{
    /// <summary>
    /// Indicates a keyword search: each term must match (AND) at least one of the configured fields (OR); executed by <i>CoreEx</i> as a dynamic LINQ predicate (unranked).
    /// </summary>
    Keyword = 0,

    /// <summary>
    /// Indicates a lexical full-text search (e.g. BM25); executed by the repository (ranked).
    /// </summary>
    FullText = 1,

    /// <summary>
    /// Indicates a semantic (vector/embedding similarity) search; executed by the repository (ranked).
    /// </summary>
    Semantic = 2,

    /// <summary>
    /// Indicates a hybrid search that fuses lexical and semantic results (e.g. reciprocal rank fusion); executed by the repository (ranked).
    /// </summary>
    Hybrid = 3
}
