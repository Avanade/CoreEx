namespace CoreEx.Cosmos;

/// <summary>
/// Represents a composable query against a <see cref="CosmosDbContainer{TModel}"/>, together with its materializers.
/// </summary>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
/// <remarks>Instances are created internally by <see cref="CosmosDbContainer{TModel}.Query(Func{IQueryable{TModel}, IQueryable{TModel}}?, CosmosDbArgs?)"/>; additional filtering/ordering is composed
/// using standard LINQ (<see cref="Queryable"/>) via that method's <c>query</c> delegate parameter, or via <see cref="AsQueryable(CosmosDbArgs?)"/> for ad-hoc composition.
/// <para>Every materializer on this type (<see cref="ToListAsync(CancellationToken)"/>, <see cref="ToItemsResultAsync(bool, CancellationToken)"/>, etc.) routes through <see cref="ICosmosDb.Invoker"/> for
/// structured logging and <see cref="CosmosException"/> to CoreEx exception mapping — the same as every <see cref="CosmosDbContainer{TModel}"/> CRUD operation. Being instance methods on this dedicated
/// wrapper type (rather than <see cref="IQueryable{T}"/> extensions), they also structurally cannot collide with another package's identically-named <see cref="IQueryable{T}"/> extensions
/// (e.g. <c>CoreEx.EntityFrameworkCore.EfDbExtensions</c>).</para></remarks>
public class CosmosDbQuery<TModel> where TModel : class, IEntityKey, new()
{
    private readonly Func<IQueryable<TModel>, IQueryable<TModel>>? _query;
    private PagingArgs? _paging;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbQuery{TModel}"/> class.
    /// </summary>
    /// <param name="container">The owning <see cref="CosmosDbContainer{TModel}"/>.</param>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="query">The optional query composition function.</param>
    internal CosmosDbQuery(CosmosDbContainer<TModel> container, CosmosDbArgs args, Func<IQueryable<TModel>, IQueryable<TModel>>? query)
    {
        Container = container.ThrowIfNull();
        Args = args.ThrowIfNull();
        _query = query;
    }

    /// <summary>
    /// Gets the owning <see cref="CosmosDbContainer{TModel}"/>.
    /// </summary>
    public CosmosDbContainer<TModel> Container { get; }

    /// <summary>
    /// Gets the <see cref="CosmosDbArgs"/> (as specified at, or defaulted during, <see cref="CosmosDbContainer{TModel}.Query(Func{IQueryable{TModel}, IQueryable{TModel}}?, CosmosDbArgs?)"/> construction).
    /// </summary>
    public CosmosDbArgs Args { get; }

    /// <summary>
    /// Sets (overrides) the <see cref="PagingArgs"/> to be applied by the <see cref="ToListAsync(CancellationToken)"/>/<see cref="ToItemsResultAsync(bool, CancellationToken)"/>-family materializers.
    /// </summary>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <returns>The <see cref="CosmosDbQuery{TModel}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Must <b>not</b> be set prior to calling a <c>Single</c>/<c>First</c>-style materializer (see remarks there), which apply their own internally-limited paging; doing so results in an
    /// <see cref="InvalidOperationException"/>.</remarks>
    public CosmosDbQuery<TModel> WithPaging(PagingArgs? paging)
    {
        _paging = paging;
        return this;
    }

    /// <summary>
    /// Gets the composed, filtered <see cref="IQueryable{TModel}"/>.
    /// </summary>
    /// <param name="args">The optional <see cref="CosmosDbArgs"/> (defaults to the <see cref="Args"/> specified at construction).</param>
    /// <returns>The <see cref="IQueryable{TModel}"/>.</returns>
    /// <remarks>Builds the base <see cref="IQueryable{TModel}"/> (using <paramref name="args"/>' <see cref="CosmosDbArgs.QueryRequestOptions"/>), applies the query composition function supplied to
    /// <see cref="CosmosDbContainer{TModel}.Query(Func{IQueryable{TModel}, IQueryable{TModel}}?, CosmosDbArgs?)"/> (if any), and then applies the <see cref="CosmosDbModelOptions{TModel}"/>-configured
    /// filters (see <see cref="CosmosDbModelOptions{TModel}.ApplyFilters(CosmosDbArgs, IQueryable{TModel}, ExecutionContext)"/>) unless <paramref name="args"/>' <see cref="CosmosDbArgs.BypassFilters"/>
    /// is <see langword="true"/>.</remarks>
    public IQueryable<TModel> AsQueryable(CosmosDbArgs? args = null)
    {
        args ??= Args;

        IQueryable<TModel> query = Container.Container.GetItemLinqQueryable<TModel>(requestOptions: args.QueryRequestOptions);
        query = _query is null ? query : _query(query);

        return args.BypassFilters ? query : Container.Options.ApplyFilters(args, query, Container.CosmosDb.ExecutionContext);
    }

    /// <summary>
    /// Creates a <see cref="List{TModel}"/> by fully draining the underlying <see cref="FeedIterator{T}"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="List{TModel}"/>.</returns>
    public async Task<List<TModel>> ToListAsync(CancellationToken cancellationToken = default) => (await ToListWithResultInternalAsync(nameof(ToListAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Creates a <see cref="List{TModel}"/> by fully draining the underlying <see cref="FeedIterator{T}"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <see cref="List{TModel}"/>.</returns>
    public Task<Result<List<TModel>>> ToListWithResultAsync(CancellationToken cancellationToken = default) => ToListWithResultInternalAsync(nameof(ToListWithResultAsync), cancellationToken);

    /// <summary>
    /// Creates the list (internal).
    /// </summary>
    private Task<Result<List<TModel>>> ToListWithResultInternalAsync(string memberName, CancellationToken cancellationToken)
        => Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) => Result.Ok(await DrainAsync(ApplyPagingIfSet(AsQueryable()), ct).ConfigureAwait(false)), cancellationToken, memberName);

    /// <summary>
    /// Creates a <typeparamref name="TColl"/> by fully draining the underlying <see cref="FeedIterator{T}"/>.
    /// </summary>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TColl"/>.</returns>
    public async Task<TColl> ToCollectionAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<TModel>, new()
        => (await ToCollectionWithResultInternalAsync<TColl>(nameof(ToCollectionAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Creates a <typeparamref name="TColl"/> by fully draining the underlying <see cref="FeedIterator{T}"/>.
    /// </summary>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <typeparamref name="TColl"/>.</returns>
    public Task<Result<TColl>> ToCollectionWithResultAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<TModel>, new()
        => ToCollectionWithResultInternalAsync<TColl>(nameof(ToCollectionWithResultAsync), cancellationToken);

    /// <summary>
    /// Creates the collection (internal).
    /// </summary>
    private Task<Result<TColl>> ToCollectionWithResultInternalAsync<TColl>(string memberName, CancellationToken cancellationToken) where TColl : ICollection<TModel>, new()
        => Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) =>
        {
            var coll = new TColl();
            foreach (var item in await DrainAsync(ApplyPagingIfSet(AsQueryable()), ct).ConfigureAwait(false))
                coll.Add(item);

            return Result.Ok(coll);
        }, cancellationToken, memberName);

    /// <summary>
    /// Creates an <see cref="ItemsResult{TModel}"/> applying the <see cref="WithPaging(PagingArgs?)"/> state (including with <see cref="PagingResult.TotalCount"/> where requested).
    /// </summary>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="ItemsResult{TModel}"/>.</returns>
    /// <remarks>Where <see cref="WithPaging(PagingArgs?)"/> was never called, defaults to <see cref="PagingArgs.None"/> (i.e. no paging is applied and the resulting <see cref="IItemsResult.Paging"/>
    /// is <see langword="null"/>, per the standard <see cref="ItemsResult{TItem}"/> "no paging specified" convention).
    /// <para>The <paramref name="autoCount"/> query executes a separate <c>SELECT VALUE COUNT(1)</c>-equivalent request unit cost (before paging is applied) and is opt-in given the additional RU cost;
    /// it has no effect unless paging with <see cref="PagingArgs.IsCountRequested"/> has been requested.</para></remarks>
    public async Task<ItemsResult<TModel>> ToItemsResultAsync(bool autoCount = true, CancellationToken cancellationToken = default)
        => (await ToItemsResultWithResultInternalAsync(autoCount, nameof(ToItemsResultAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Creates an <see cref="ItemsResult{TModel}"/> applying the <see cref="WithPaging(PagingArgs?)"/> state (including with <see cref="PagingResult.TotalCount"/> where requested).
    /// </summary>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <see cref="ItemsResult{TModel}"/>.</returns>
    public Task<Result<ItemsResult<TModel>>> ToItemsResultWithResultAsync(bool autoCount = true, CancellationToken cancellationToken = default)
        => ToItemsResultWithResultInternalAsync(autoCount, nameof(ToItemsResultWithResultAsync), cancellationToken);

    /// <summary>
    /// Creates the <see cref="ItemsResult{TModel}"/> (internal).
    /// </summary>
    private Task<Result<ItemsResult<TModel>>> ToItemsResultWithResultInternalAsync(bool autoCount, string memberName, CancellationToken cancellationToken)
        => Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) =>
        {
            var paging = _paging ?? PagingArgs.None;
            var baseQuery = AsQueryable();
            var ir = new ItemsResult<TModel>(paging) { Items = await DrainAsync(baseQuery.WithPaging(paging), ct).ConfigureAwait(false) };

            if (autoCount)
                await ir.WithTotalCountAsync(async ct2 => (long?)(await baseQuery.CountAsync(ct2).ConfigureAwait(false)).Resource, ct).ConfigureAwait(false);

            return Result.Ok(ir);
        }, cancellationToken, memberName);

    /// <summary>
    /// Returns the only element, throwing an exception if there is not exactly one.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The single resulting element.</returns>
    /// <remarks>Internally applies <c>Skip(0).Take(2)</c> before draining so that at most two items are ever fetched from Cosmos DB, rather than the whole result set, before the in-memory
    /// <see cref="Enumerable.Single{TSource}(IEnumerable{TSource})"/> check. <see cref="WithPaging(PagingArgs?)"/> must not have been set; doing so results in an <see cref="InvalidOperationException"/>
    /// as the internally-applied paging is required to limit unnecessary data retrieval.</remarks>
    public async Task<TModel> SingleAsync(CancellationToken cancellationToken = default) => (await SingleWithResultInternalAsync(nameof(SingleAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Returns the only element, throwing an exception if there is not exactly one.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the single resulting element.</returns>
    /// <remarks>See <see cref="SingleAsync(CancellationToken)"/> for the internally-applied paging behavior.</remarks>
    public Task<Result<TModel>> SingleWithResultAsync(CancellationToken cancellationToken = default) => SingleWithResultInternalAsync(nameof(SingleWithResultAsync), cancellationToken);

    /// <summary>
    /// Gets the single element (internal).
    /// </summary>
    private Task<Result<TModel>> SingleWithResultInternalAsync(string memberName, CancellationToken cancellationToken)
    {
        ThrowIfPagingSet(memberName);
        return Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) => Result.Ok((await DrainAsync(AsQueryable().Skip(0).Take(2), ct).ConfigureAwait(false)).Single()), cancellationToken, memberName);
    }

    /// <summary>
    /// Returns the only element, or <see langword="null"/> if there are no elements; throws an exception if there is more than one element.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The single resulting element, or <see langword="null"/>.</returns>
    /// <remarks>See <see cref="SingleAsync(CancellationToken)"/> for the internally-applied paging behavior.</remarks>
    public async Task<TModel?> SingleOrDefaultAsync(CancellationToken cancellationToken = default) => (await SingleOrDefaultWithResultInternalAsync(nameof(SingleOrDefaultAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Returns the only element, or <see langword="null"/> if there are no elements; throws an exception if there is more than one element.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the single resulting element, or <see langword="null"/>.</returns>
    /// <remarks>See <see cref="SingleAsync(CancellationToken)"/> for the internally-applied paging behavior.</remarks>
    public Task<Result<TModel?>> SingleOrDefaultWithResultAsync(CancellationToken cancellationToken = default) => SingleOrDefaultWithResultInternalAsync(nameof(SingleOrDefaultWithResultAsync), cancellationToken);

    /// <summary>
    /// Gets the single-or-default element (internal).
    /// </summary>
    private Task<Result<TModel?>> SingleOrDefaultWithResultInternalAsync(string memberName, CancellationToken cancellationToken)
    {
        ThrowIfPagingSet(memberName);
        return Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) => Result.Ok<TModel?>((await DrainAsync(AsQueryable().Skip(0).Take(2), ct).ConfigureAwait(false)).SingleOrDefault()), cancellationToken, memberName);
    }

    /// <summary>
    /// Returns the first element.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The first resulting element.</returns>
    /// <remarks>Internally applies <c>Take(1)</c> before draining so that at most one item is ever fetched from Cosmos DB, rather than the whole result set. <see cref="WithPaging(PagingArgs?)"/> must
    /// not have been set; doing so results in an <see cref="InvalidOperationException"/> as the internally-applied paging is required to limit unnecessary data retrieval.</remarks>
    public async Task<TModel> FirstAsync(CancellationToken cancellationToken = default) => (await FirstWithResultInternalAsync(nameof(FirstAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Returns the first element.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the first resulting element.</returns>
    /// <remarks>See <see cref="FirstAsync(CancellationToken)"/> for the internally-applied paging behavior.</remarks>
    public Task<Result<TModel>> FirstWithResultAsync(CancellationToken cancellationToken = default) => FirstWithResultInternalAsync(nameof(FirstWithResultAsync), cancellationToken);

    /// <summary>
    /// Gets the first element (internal).
    /// </summary>
    private Task<Result<TModel>> FirstWithResultInternalAsync(string memberName, CancellationToken cancellationToken)
    {
        ThrowIfPagingSet(memberName);
        return Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) => Result.Ok((await DrainAsync(AsQueryable().Take(1), ct).ConfigureAwait(false)).First()), cancellationToken, memberName);
    }

    /// <summary>
    /// Returns the first element, or <see langword="null"/> if there are no elements.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The first resulting element, or <see langword="null"/>.</returns>
    /// <remarks>See <see cref="FirstAsync(CancellationToken)"/> for the internally-applied paging behavior.</remarks>
    public async Task<TModel?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) => (await FirstOrDefaultWithResultInternalAsync(nameof(FirstOrDefaultAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Returns the first element, or <see langword="null"/> if there are no elements.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the first resulting element, or <see langword="null"/>.</returns>
    /// <remarks>See <see cref="FirstAsync(CancellationToken)"/> for the internally-applied paging behavior.</remarks>
    public Task<Result<TModel?>> FirstOrDefaultWithResultAsync(CancellationToken cancellationToken = default) => FirstOrDefaultWithResultInternalAsync(nameof(FirstOrDefaultWithResultAsync), cancellationToken);

    /// <summary>
    /// Gets the first-or-default element (internal).
    /// </summary>
    private Task<Result<TModel?>> FirstOrDefaultWithResultInternalAsync(string memberName, CancellationToken cancellationToken)
    {
        ThrowIfPagingSet(memberName);
        return Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) => Result.Ok<TModel?>((await DrainAsync(AsQueryable().Take(1), ct).ConfigureAwait(false)).FirstOrDefault()), cancellationToken, memberName);
    }

    /// <summary>
    /// Creates a <see cref="List{T}"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TModel, T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="List{T}"/>.</returns>
    public async Task<List<T>> ToMappedItemsAsync<T>(Func<TModel, T> mapper, CancellationToken cancellationToken = default)
        => (await ToMappedItemsWithResultInternalAsync(mapper, nameof(ToMappedItemsAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Creates a <see cref="List{T}"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TModel, T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <see cref="List{T}"/>.</returns>
    public Task<Result<List<T>>> ToMappedItemsWithResultAsync<T>(Func<TModel, T> mapper, CancellationToken cancellationToken = default)
        => ToMappedItemsWithResultInternalAsync(mapper, nameof(ToMappedItemsWithResultAsync), cancellationToken);

    /// <summary>
    /// Creates a <see cref="List{T}"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="IMapper{TModel, T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="List{T}"/>.</returns>
    public Task<List<T>> ToMappedItemsAsync<T>(IMapper<TModel, T> mapper, CancellationToken cancellationToken = default) where T : class
        => ToMappedItemsAsync(source => mapper.ThrowIfNull().Map(source)!, cancellationToken);

    /// <summary>
    /// Creates a <see cref="List{T}"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="IMapper{TModel, T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <see cref="List{T}"/>.</returns>
    public Task<Result<List<T>>> ToMappedItemsWithResultAsync<T>(IMapper<TModel, T> mapper, CancellationToken cancellationToken = default) where T : class
        => ToMappedItemsWithResultAsync(source => mapper.ThrowIfNull().Map(source)!, cancellationToken);

    /// <summary>
    /// Creates the mapped list (internal).
    /// </summary>
    private Task<Result<List<T>>> ToMappedItemsWithResultInternalAsync<T>(Func<TModel, T> mapper, string memberName, CancellationToken cancellationToken)
    {
        mapper.ThrowIfNull();
        return Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct)
            => Result.Ok((await DrainAsync(ApplyPagingIfSet(AsQueryable()), ct).ConfigureAwait(false)).ConvertAll(item => mapper(item))), cancellationToken, memberName);
    }

    /// <summary>
    /// Creates a <typeparamref name="TColl"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TColl">The item collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TModel, T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TColl"/>.</returns>
    public async Task<TColl> ToMappedItemsAsync<TColl, T>(Func<TModel, T> mapper, CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
        => (await ToMappedItemsWithResultInternalAsync<TColl, T>(mapper, nameof(ToMappedItemsAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Creates a <typeparamref name="TColl"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TColl">The item collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TModel, T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <typeparamref name="TColl"/>.</returns>
    public Task<Result<TColl>> ToMappedItemsWithResultAsync<TColl, T>(Func<TModel, T> mapper, CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
        => ToMappedItemsWithResultInternalAsync<TColl, T>(mapper, nameof(ToMappedItemsWithResultAsync), cancellationToken);

    /// <summary>
    /// Creates a <typeparamref name="TColl"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TColl">The item collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="IMapper{TModel, T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <typeparamref name="TColl"/>.</returns>
    public Task<TColl> ToMappedItemsAsync<TColl, T>(IMapper<TModel, T> mapper, CancellationToken cancellationToken = default) where TColl : ICollection<T>, new() where T : class
        => ToMappedItemsAsync<TColl, T>(source => mapper.ThrowIfNull().Map(source)!, cancellationToken);

    /// <summary>
    /// Creates a <typeparamref name="TColl"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TColl">The item collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="IMapper{TModel, T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <typeparamref name="TColl"/>.</returns>
    public Task<Result<TColl>> ToMappedItemsWithResultAsync<TColl, T>(IMapper<TModel, T> mapper, CancellationToken cancellationToken = default) where TColl : ICollection<T>, new() where T : class
        => ToMappedItemsWithResultAsync<TColl, T>(source => mapper.ThrowIfNull().Map(source)!, cancellationToken);

    /// <summary>
    /// Creates the mapped collection (internal).
    /// </summary>
    private Task<Result<TColl>> ToMappedItemsWithResultInternalAsync<TColl, T>(Func<TModel, T> mapper, string memberName, CancellationToken cancellationToken) where TColl : ICollection<T>, new()
    {
        mapper.ThrowIfNull();
        return Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) =>
        {
            var coll = new TColl();
            foreach (var item in await DrainAsync(ApplyPagingIfSet(AsQueryable()), ct).ConfigureAwait(false))
                coll.Add(mapper(item));

            return Result.Ok(coll);
        }, cancellationToken, memberName);
    }

    /// <summary>
    /// Creates an <see cref="ItemsResult{T}"/> using the specified <paramref name="mapper"/>, applying the <see cref="WithPaging(PagingArgs?)"/> state (including with <see cref="PagingResult.TotalCount"/>
    /// where requested).
    /// </summary>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TModel, T}"/>.</param>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="ItemsResult{T}"/>.</returns>
    /// <remarks>See <see cref="ToItemsResultAsync(bool, CancellationToken)"/> for the "no paging specified" and <paramref name="autoCount"/> behavior.</remarks>
    public async Task<ItemsResult<T>> ToMappedItemsResultAsync<T>(Func<TModel, T> mapper, bool autoCount = true, CancellationToken cancellationToken = default)
        => (await ToMappedItemsResultWithResultInternalAsync(mapper, autoCount, nameof(ToMappedItemsResultAsync), cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Creates an <see cref="ItemsResult{T}"/> using the specified <paramref name="mapper"/>, applying the <see cref="WithPaging(PagingArgs?)"/> state (including with <see cref="PagingResult.TotalCount"/>
    /// where requested).
    /// </summary>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="Func{TModel, T}"/>.</param>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <see cref="ItemsResult{T}"/>.</returns>
    public Task<Result<ItemsResult<T>>> ToMappedItemsResultWithResultAsync<T>(Func<TModel, T> mapper, bool autoCount = true, CancellationToken cancellationToken = default)
        => ToMappedItemsResultWithResultInternalAsync(mapper, autoCount, nameof(ToMappedItemsResultWithResultAsync), cancellationToken);

    /// <summary>
    /// Creates an <see cref="ItemsResult{T}"/> using the specified <paramref name="mapper"/>, applying the <see cref="WithPaging(PagingArgs?)"/> state (including with <see cref="PagingResult.TotalCount"/>
    /// where requested).
    /// </summary>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="IMapper{TModel, T}"/>.</param>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="ItemsResult{T}"/>.</returns>
    public Task<ItemsResult<T>> ToMappedItemsResultAsync<T>(IMapper<TModel, T> mapper, bool autoCount = true, CancellationToken cancellationToken = default) where T : class
        => ToMappedItemsResultAsync(source => mapper.ThrowIfNull().Map(source)!, autoCount, cancellationToken);

    /// <summary>
    /// Creates an <see cref="ItemsResult{T}"/> using the specified <paramref name="mapper"/>, applying the <see cref="WithPaging(PagingArgs?)"/> state (including with <see cref="PagingResult.TotalCount"/>
    /// where requested).
    /// </summary>
    /// <typeparam name="T">The mapped item <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The mapping <see cref="IMapper{TModel, T}"/>.</param>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result{T}"/> containing the resulting <see cref="ItemsResult{T}"/>.</returns>
    public Task<Result<ItemsResult<T>>> ToMappedItemsResultWithResultAsync<T>(IMapper<TModel, T> mapper, bool autoCount = true, CancellationToken cancellationToken = default) where T : class
        => ToMappedItemsResultWithResultAsync(source => mapper.ThrowIfNull().Map(source)!, autoCount, cancellationToken);

    /// <summary>
    /// Creates the mapped <see cref="ItemsResult{T}"/> (internal).
    /// </summary>
    private Task<Result<ItemsResult<T>>> ToMappedItemsResultWithResultInternalAsync<T>(Func<TModel, T> mapper, bool autoCount, string memberName, CancellationToken cancellationToken)
    {
        mapper.ThrowIfNull();
        return Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, Args, async (_, _, ct) =>
        {
            var paging = _paging ?? PagingArgs.None;
            var baseQuery = AsQueryable();
            var ir = new ItemsResult<T>(paging) { Items = (await DrainAsync(baseQuery.WithPaging(paging), ct).ConfigureAwait(false)).ConvertAll(item => mapper(item)) };

            if (autoCount)
                await ir.WithTotalCountAsync(async ct2 => (long?)(await baseQuery.CountAsync(ct2).ConfigureAwait(false)).Resource, ct).ConfigureAwait(false);

            return Result.Ok(ir);
        }, cancellationToken, memberName);
    }

    /// <summary>
    /// Applies the <see cref="WithPaging(PagingArgs?)"/> state (where set) to the <paramref name="queryable"/>.
    /// </summary>
    private IQueryable<TModel> ApplyPagingIfSet(IQueryable<TModel> queryable) => _paging is null ? queryable : queryable.WithPaging(_paging);

    /// <summary>
    /// Guards against <see cref="WithPaging(PagingArgs?)"/> having been explicitly set prior to a <c>Single</c>/<c>First</c>-style materializer.
    /// </summary>
    private void ThrowIfPagingSet(string memberName)
    {
        if (_paging is not null)
            throw new InvalidOperationException($"{nameof(PagingArgs)} must be null (see {nameof(WithPaging)}) before calling '{memberName}'; internally applied paging is used to limit unnecessary data retrieval.");
    }

    /// <summary>
    /// Creates a <see cref="List{TModel}"/> from a <see cref="IQueryable{TModel}"/> by fully draining the underlying <see cref="FeedIterator{T}"/>.
    /// </summary>
    private static async Task<List<TModel>> DrainAsync(IQueryable<TModel> queryable, CancellationToken cancellationToken)
    {
        var items = new List<TModel>();
        using var iterator = queryable.ToFeedIterator();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            items.AddRange(response);
        }

        return items;
    }
}
