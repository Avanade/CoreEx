namespace CoreEx.Data;

public static partial class DataExtensions
{
    /// <summary>
    /// Filters a <paramref name="source"/> sequence according to the specified <paramref name="paging"/> or <see cref="PagingArgs.Create"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <returns>An <see cref="IQueryable{T}"/> that contains the elements from the <paramref name="source"/> sequence after applying the <paramref name="paging"/>.</returns>
    /// <remarks>The <paramref name="paging"/> where <see langword="null"/> will default to <see cref="PagingArgs.Create"/>.</remarks>
    public static IQueryable<TSource> WithPaging<TSource>(this IQueryable<TSource> source, PagingArgs? paging = null)
    {
        if (paging?.IsNone ?? false)
            return source;

        paging ??= PagingArgs.Create();
        return source.Skip(paging.Skip).Take(paging.Take);
    }
}