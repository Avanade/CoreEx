namespace CoreEx.Cosmos;

public partial class CosmosDbContainer<TModel>
{
    /// <summary>
    /// Creates a <see cref="CosmosDbQuery{TModel}"/> to compose (via <paramref name="query"/>) and materialize a query against the underlying <see cref="Container"/>.
    /// </summary>
    /// <param name="query">The optional function to further compose the underlying <see cref="IQueryable{TModel}"/> (e.g. <c>Where</c>/<c>OrderBy</c>) prior to any <see cref="CosmosDbModelOptions{TModel}"/>-configured
    /// filters being applied (see <see cref="CosmosDbQuery{TModel}.AsQueryable(CosmosDbArgs?)"/>).</param>
    /// <param name="args">The optional <see cref="CosmosDbArgs"/>.</param>
    /// <returns>The <see cref="CosmosDbQuery{TModel}"/>.</returns>
    /// <remarks>Paging uses <c>Skip</c>/<c>Take</c> (translated by the Cosmos DB LINQ provider to <c>OFFSET…LIMIT</c>) via <see cref="CosmosDbQuery{TModel}.WithPaging(PagingArgs?)"/>; continuation-token-based
    /// paging is not currently supported.
    /// <para>Every <see cref="CosmosDbQuery{TModel}"/> materializer (e.g. <c>ToListAsync</c>, <c>ToItemsResultAsync</c>) routes through <see cref="ICosmosDb.Invoker"/> for structured logging and
    /// <see cref="CosmosException"/> to CoreEx exception mapping, matching every other <see cref="CosmosDbContainer{TModel}"/> operation.</para></remarks>
    public CosmosDbQuery<TModel> Query(Func<IQueryable<TModel>, IQueryable<TModel>>? query = null, CosmosDbArgs? args = null) => new(this, args ?? Args, query);
}
