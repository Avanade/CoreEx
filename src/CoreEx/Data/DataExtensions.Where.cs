namespace CoreEx.Data;

public static partial class DataExtensions
{
    /// <summary>
    /// Filters a <paramref name="source"/> sequence of values based on a <paramref name="predicate"/> only <paramref name="when"/> <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="when">Indicates to perform an underlying <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> <i>only</i> when <see langword="true"/>; otherwise, no <i>Where</i> is invoked.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>An <see cref="IQueryable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
    public static IQueryable<TSource> WhereWhen<TSource>(this IQueryable<TSource> source, bool when, Expression<Func<TSource, bool>> predicate) => when ? source.Where(predicate) : source;

    /// <summary>
    /// Filters a <paramref name="source"/> sequence based on a <paramref name="predicate"/> only when the <paramref name="with"/> is not the default value for the <typeparamref name="TWith"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <typeparam name="TWith">The with value <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="with">Indicates to perform an underlying <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> only when the with is not the default value; otherwise, no <b>Where</b> is invoked.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>An <see cref="IQueryable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
    /// <remarks>Where the <paramref name="with"/> is an <see cref="IEnumerable"/> it will also ensure there is at least a single item.</remarks>
    public static IQueryable<TSource> WhereWith<TSource, TWith>(this IQueryable<TSource> source, TWith with, Expression<Func<TSource, bool>> predicate)
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
    /// <returns>An <see cref="IQueryable{T}"/> that contains the elements from the <paramref name="source"/> sequence after applying the wildcard <paramref name="pattern"/>.</returns>
    public static IQueryable<TSource> WhereWildcard<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, string?>> selector, string? pattern, bool ignoreCase = true, bool checkForNull = true, Wildcard? wildcard = null) where TSource : class
    {
        selector.ThrowIfNull();

        var wc = wildcard ?? Wildcard.Default ?? Wildcard.MultiBasic;
        var wr = wc.Parse(pattern).ThrowOnError();

        // Exit stage left where nothing to do.
        if (wr.Selection.HasFlag(WildcardSelection.None) || wr.Selection.HasFlag(WildcardSelection.Single))
            return source;

        // Check the expression.
        if (selector.Body is not MemberExpression me)
            throw new ArgumentException("Selector expression must be of Type MemberExpression.", nameof(selector));

        Expression exp = me;
        var s = wr.GetTextWithoutWildcards();
        if (ignoreCase)
        {
            s = s?.ToUpper(CultureInfo.CurrentCulture);
            exp = Expression.Call(me, typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!)!;
        }

        if (wr.Selection.HasFlag(WildcardSelection.Equal))
            exp = Expression.Equal(exp, Expression.Constant(s));
        else if (wr.Selection.HasFlag(WildcardSelection.EndsWith))
            exp = Expression.Call(exp, "EndsWith", null, Expression.Constant(s));
        else if (wr.Selection.HasFlag(WildcardSelection.StartsWith))
            exp = Expression.Call(exp, "StartsWith", null, Expression.Constant(s));
        else if (wr.Selection.HasFlag(WildcardSelection.Contains))
            exp = Expression.Call(exp, "Contains", null, Expression.Constant(s));
        else
            throw new ArgumentException("Wildcard selection pattern is not supported for an IQueryable; must result in an Equal, StartsWith, EndsWith or Contains only.", nameof(pattern));

        // Add check for not null.
        if (checkForNull)
        {
            var ee = Expression.NotEqual(me, Expression.Constant(null));
            exp = Expression.AndAlso(ee, exp);
        }

        // Create the final lambda expression.
        return source.Where(Expression.Lambda<Func<TSource, bool>>(exp, selector.Parameters));
    }
}