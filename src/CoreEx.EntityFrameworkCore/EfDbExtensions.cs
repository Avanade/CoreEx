namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides <see href="https://learn.microsoft.com/en-us/ef/core/">Entity Framework Core</see> extensions.
/// </summary>
public static partial class EfDbExtensions
{
    /// <summary>
    /// Creates a <typeparamref name="TColl"/> from a <typeparamref name="TSource"/> <see cref="IQueryable{TSource}"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TColl">The item collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="query">The <see cref="IQueryable{TSource}"/>.</param>
    /// <param name="mapper">The mapping <see cref="Func{TSource, TItem}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public static async Task<TColl> ToMappedItemsAsync<TSource, TColl, TItem>(this IQueryable<TSource> query, Func<TSource, TItem> mapper, CancellationToken cancellationToken = default) where TColl : ICollection<TItem>, new()
    {
        mapper.ThrowIfNull();

        var q = query.ThrowIfNull();
        var items = new TColl();

        await foreach (var source in q.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            items.Add(mapper(source));
        }

        return items;
    }

    /// <summary>
    /// Creates a <typeparamref name="TColl"/> from a <typeparamref name="TSource"/> <see cref="IQueryable{TSource}"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TColl">The item collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="query">The <see cref="IQueryable{TSource}"/>.</param>
    /// <param name="mapper">The mapping <see cref="IMapper{TSource, TItem}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public static async Task<TColl> ToMappedItemsAsync<TSource, TColl, TItem>(this IQueryable<TSource> query, IMapper<TSource, TItem> mapper, CancellationToken cancellationToken = default) where TSource : class where TColl : ICollection<TItem>, new() where TItem : class
        => await ToMappedItemsAsync<TSource, TColl, TItem>(query, source => mapper.Map(source)!, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Creates a <see cref="List{TItem}"/> from a <typeparamref name="TSource"/> <see cref="IQueryable{TSource}"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="query">The <see cref="IQueryable{TSource}"/>.</param>
    /// <param name="mapper">The mapping <see cref="Func{TSource, TItem}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public static async Task<List<TItem>> ToMappedItemsAsync<TSource, TItem>(this IQueryable<TSource> query, Func<TSource, TItem> mapper, CancellationToken cancellationToken = default)
    {
        mapper.ThrowIfNull();

        var q = query.ThrowIfNull();
        var items = new List<TItem>();

        await foreach (var source in q.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            items.Add(mapper(source));
        }

        return items;
    }

    /// <summary>
    /// Creates a <see cref="List{TItem}"/> from a <typeparamref name="TSource"/> <see cref="IQueryable{TSource}"/> using the specified <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="query">The <see cref="IQueryable{TSource}"/>.</param>
    /// <param name="mapper">The mapping <see cref="IMapper{TSource, TItem}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public static async Task<List<TItem>> ToMappedItemsAsync<TSource, TItem>(this IQueryable<TSource> query, IMapper<TSource, TItem> mapper, CancellationToken cancellationToken = default) where TSource : class where TItem : class
        => await ToMappedItemsAsync(query, source => mapper.Map(source)!, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Creates a <see cref="ItemsResult{TItem}"/> from an <see cref="IQueryable{TItem}"/> applying <paramref name="paging"/> (including with <see cref="PagingResult.TotalCount"/> where requested).
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="query">The <see cref="IQueryable{TItem}"/>.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="ItemsResult{TItem}"/>.</returns>
    /// <remarks>The <paramref name="autoCount"/> indicates whether the <see cref="PagingResult.TotalCount"/> query should be automatically executed using the <paramref name="query"/> before the <paramref name="paging"/>
    /// is applied and <see cref="PagingArgs.IsCountRequested"/>. This is opt-in as not all LINQ implementations support the reuse of the query, or allow counthing where ordering has previously been applied.</remarks>
    public static async Task<ItemsResult<TItem>> ToItemsResultAsync<TItem>(this IQueryable<TItem> query, PagingArgs? paging = null, bool autoCount = true, CancellationToken cancellationToken = default)
    {
        var q = query.ThrowIfNull();
        var ir = new ItemsResult<TItem>(paging)
        {
            Items = await q.WithPaging(paging).ToArrayAsync(cancellationToken).ConfigureAwait(false)
        };

        // When auto-counting and requested to do so, then execute a count.
        if (autoCount)
            await ir.WithTotalCountAsync(async cancellationToken => await query.LongCountAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

        return ir;
    }

    /// <summary>
    /// Creates a <see cref="ItemsResult{TItem}"/> from a <typeparamref name="TSource"/> <see cref="IQueryable{TSource}"/> applying <paramref name="paging"/> (including with <see cref="PagingResult.TotalCount"/> where requested).
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="query">The <see cref="IQueryable{TSource}"/>.</param>
    /// <param name="mapper">The mapping <see cref="Func{TSource, TItem}"/>.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="ItemsResult{TItem}"/>.</returns>
    /// <remarks>The <paramref name="autoCount"/> indicates whether the <see cref="PagingResult.TotalCount"/> query should be automatically executed using the <paramref name="query"/> before the <paramref name="paging"/>
    /// is applied and <see cref="PagingArgs.IsCountRequested"/>. This is opt-in as not all LINQ implementations support the reuse of the query, or allow counthing where ordering has previously been applied.</remarks>
    public static async Task<ItemsResult<TItem>> ToMappedItemsResultAsync<TSource, TItem>(this IQueryable<TSource> query, Func<TSource, TItem> mapper, PagingArgs? paging = null, bool autoCount = true, CancellationToken cancellationToken = default)
    {
        var q = query.ThrowIfNull();
        var ir = new ItemsResult<TItem>(paging)
        {
            Items = await ToMappedItemsAsync(q.WithPaging(paging), mapper, cancellationToken).ConfigureAwait(false)
        };

        // When auto-counting and requested to do so, then execute a count.
        if (autoCount)
            await ir.WithTotalCountAsync(async cancellationToken => await query.LongCountAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

        return ir;
    }

    /// <summary>
    /// Creates a <see cref="ItemsResult{TItem}"/> from a <typeparamref name="TSource"/> <see cref="IQueryable{TSource}"/> applying <paramref name="paging"/> (including with <see cref="PagingResult.TotalCount"/> where requested).
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="query">The <see cref="IQueryable{TSource}"/>.</param>
    /// <param name="mapper">The mapping <see cref="IMapper{TSource, TItem}"/>.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <param name="autoCount">Indicates whether to perform the <see cref="PagingResult.TotalCount"/> query automatically.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="ItemsResult{TItem}"/>.</returns>
    /// <remarks>The <paramref name="autoCount"/> indicates whether the <see cref="PagingResult.TotalCount"/> query should be automatically executed using the <paramref name="query"/> before the <paramref name="paging"/>
    /// is applied and <see cref="PagingArgs.IsCountRequested"/>. This is opt-in as not all LINQ implementations support the reuse of the query, or allow counthing where ordering has previously been applied.</remarks>
    public static async Task<ItemsResult<TItem>> ToMappedItemsResultAsync<TSource, TItem>(this IQueryable<TSource> query, IMapper<TSource, TItem> mapper, PagingArgs? paging = null, bool autoCount = true, CancellationToken cancellationToken = default) where TSource : class where TItem : class
        => await query.ToMappedItemsResultAsync(source => mapper.Map(source)!, paging, autoCount, cancellationToken).ConfigureAwait(false);
}