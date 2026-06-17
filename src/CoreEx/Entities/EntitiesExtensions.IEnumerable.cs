namespace CoreEx.Entities;

/// <summary>
/// Provides entity-oriented extension methods.
/// </summary>
public static partial class EntitiesExtensions
{
    /// <summary>
    /// Filters a <paramref name="source"/> sequence of values based on a <paramref name="predicate"/> only <paramref name="when"/> <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="when">Indicates to perform an underlying <see cref="System.Linq.Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/> <i>only</i> when <see langword="true"/>; otherwise, no <i>Where</i> is invoked.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
    public static IEnumerable<TSource> WhereWhen<TSource>(this IEnumerable<TSource> source, bool when, Func<TSource, bool> predicate) => when ? source.Where(predicate) : source;

    /// <summary>
    /// Filters a <paramref name="source"/> sequence based on a <paramref name="predicate"/> only when the <paramref name="with"/> is not the default value for the <typeparamref name="TWith"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <typeparam name="TWith">The with value <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="with">Indicates to perform an underlying <see cref="Queryable.Where{TSource}(IQueryable{TSource}, System.Linq.Expressions.Expression{Func{TSource, bool}})"/> only when the with is not the default value; otherwise, no <b>Where</b> is invoked.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
    /// <remarks>Where the <paramref name="with"/> is an <see cref="IEnumerable"/> it will also ensure there is at least a single item.</remarks>
    public static IEnumerable<TSource> WhereWith<TSource, TWith>(this IEnumerable<TSource> source, TWith with, Func<TSource, bool> predicate)
    {
        if (Comparer<TWith>.Default.Compare(with, default!) != 0)
        {
            if (with is not string && with is IEnumerable ie && !ie.GetEnumerator().MoveNext())
                return source;

            return source.Where(predicate);
        }

        return source;
    }

    /// <summary>
    /// Filters a <paramref name="source"/> sequence using the specified <paramref name="selector"/> and <paramref name="pattern"/> containing supported wildcards.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="selector">The function to select the element value to <paramref name="pattern"/> match.</param>
    /// <param name="pattern">The pattern to <paramref name="wildcard"/> match the result of each element <paramref name="selector"/>.</param>
    /// <param name="ignoreCase">Indicates whether the comparison should ignore case (default) or not.</param>
    /// <param name="checkForNull">Indicates whether a <see langword="null"/> check should also be performed before the comparion occurs (defaults to <see langword="true"/>).</param>
    /// <param name="wildcard">The <see cref="Wildcard"/> configuration to use; where <see langword="null"/> it will use <see cref="Wildcard.Default"/>.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that contains the elements from the <paramref name="source"/> sequence after applying the wildcard <paramref name="pattern"/>.</returns>
    public static IEnumerable<TSource> WhereWildcard<TSource>(this IEnumerable<TSource> source, Func<TSource, string?> selector, string? pattern, bool ignoreCase = true, bool checkForNull = true, Wildcard? wildcard = null) where TSource : class
    {
        selector.ThrowIfNull();

        var wc = wildcard ?? Wildcard.Default ?? Wildcard.MultiBasic;
        var wr = wc.Parse(pattern).ThrowOnError();

        // Exit stage left where nothing to do.
        if (wr.Selection.HasFlag(WildcardSelection.None))
            return source;

        // Handle the Equal.
        var sc = ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
        if (wr.Selection.HasFlag(WildcardSelection.Equal))
            return source.Where(x =>
            {
                var v = selector(x);
                return checkForNull ? v is not null && v.Equals(pattern, sc) : v!.Equals(pattern, sc);
            });

        // Handle the easy Contains/StartsWith/Endswith.
        if (!wr.Selection.HasFlag(WildcardSelection.SingleWildcard) && !wr.Selection.HasFlag(WildcardSelection.Embedded))
        {
            if (wr.Selection.HasFlag(WildcardSelection.Single))
                return source;

            if (wr.Selection.HasFlag(WildcardSelection.Contains))
                return source.Where(x =>
                {
                    var v = selector(x);
                    return checkForNull ? v is not null && v.Contains(wr.GetTextWithoutWildcards()!, sc) : v!.Contains(wr.GetTextWithoutWildcards()!, sc);
                });
            else if (wr.Selection.HasFlag(WildcardSelection.StartsWith))
                return source.Where(x =>
                {
                    var v = selector(x);
                    return checkForNull ? v is not null && v.StartsWith(wr.GetTextWithoutWildcards()!, sc) : v!.StartsWith(wr.GetTextWithoutWildcards()!, sc);
                });
            else if (wr.Selection.HasFlag(WildcardSelection.EndsWith))
                return source.Where(x =>
                {
                    var v = selector(x);
                    return checkForNull ? v is not null && v.EndsWith(wr.GetTextWithoutWildcards()!, sc) : v!.EndsWith(wr.GetTextWithoutWildcards()!, sc);
                });
        }

        // Handle the remainder using a regex.
        var regex = wr.CreateRegex(ignoreCase);
        return source.Where(x =>
        {
            var v = selector(x);
            return v is not null && regex.IsMatch(v);
        });
    }

    /// <summary>
    /// Filters a <paramref name="source"/> sequence according to the specified <paramref name="paging"/> or <see cref="PagingArgs.Create"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that contains the elements from the <paramref name="source"/> sequence after applying the <paramref name="paging"/>.</returns>
    /// <remarks>The <paramref name="paging"/> where <see langword="null"/> will default to <see cref="PagingArgs.Create"/>.</remarks>
    public static IEnumerable<T> WithPaging<T>(this IEnumerable<T> source, PagingArgs? paging = null)
    {
        if (paging?.IsNone ?? false)
            return source;

        paging ??= PagingArgs.Create();
        return source.Skip(paging.Skip).Take(paging.Take);
    }
}