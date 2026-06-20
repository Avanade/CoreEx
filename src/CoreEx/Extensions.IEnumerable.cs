namespace CoreEx;

public static partial class Extensions
{
    /// <summary>
    /// Creates a <see cref="DataMap{TValue}"/> from an <see cref="IEnumerable{TSource}"/> according to a specified key selector and element selector functions.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TElement">The element <see cref="Type"/>.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="keySelector">A function to extract the key from each element.</param>
    /// <param name="elementSelector">A function to map each element to the value.</param>
    /// <param name="comparer">An optional equality comparer for the keys.</param>
    /// <returns>A <see cref="DataMap{TValue}"/> containing the mapped elements.</returns>
    public static DataMap<TElement> ToDataMap<TSource, TElement>(this IEnumerable<TSource> source, Func<TSource, string> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<string>? comparer = null)
    {
        source.ThrowIfNull();
        keySelector.ThrowIfNull();
        elementSelector.ThrowIfNull();

        if (source is ICollection<TSource> collection)
        {
            if (collection.Count == 0)
                return new(comparer);

            var dataMap = new DataMap<TElement>(collection.Count);
            foreach (var item in collection)
                dataMap.Add(keySelector(item), elementSelector(item));

            return dataMap;
        }
        else
        {
            var dataMap = new DataMap<TElement>(comparer);
            foreach (var item in source)
                dataMap.Add(keySelector(item), elementSelector(item));

            return dataMap;
        }
    }
}