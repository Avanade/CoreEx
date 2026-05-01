namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    /// <summary>
    /// Compare two <typeparamref name="T"/> values for equality.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="left">The left-side value.</param>
    /// <param name="right">The right-side value.</param>
    /// <returns><see langword="true"/> indicates they are equal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This improves upon the standard <see cref="object.Equals(object?, object?)"/> which for a <see langword="class"/> generally only performs a reference equality. The following additional checks
    /// are performed: <see cref="IEquatable{T}.Equals(T)"/> comparison, <see cref="ICollection.Count"/> comparison, <see cref="IDictionary"/> per item <see cref="IDictionaryEnumerator.Key"/> and <see cref="IDictionaryEnumerator.Value"/> comparisons, 
    /// <see cref="IEnumerable"/> item comparisons, and nested <see cref="IRuntimeMetadataCore"/> and <see cref="IPropertyRuntimeMetadata"/> comparisons. This is to achieve a best attempt deep-equals where a contract-style 
    /// class (such as a <see href="https://en.wikipedia.org/wiki/Data_transfer_object"/>) constrains itself to simple and known types such as those described above, and/or overrides <see cref="object.Equals(object?)"/> accordingly.</remarks>
    public static bool AreEqual<T>(T? left, T? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        // Fast-path string comparison.
        if (left is string ls && right is string rs)
            return ls == rs;

        // Where metadata, then matchy-matchy each property one-by-one.
        if (left is IRuntimeMetadataCore lrm)
        {
            var epl = lrm.GetPropertyRuntimeMetadata().GetEnumerator();
            var epr = ((IRuntimeMetadataCore)right).GetPropertyRuntimeMetadata().GetEnumerator();
            while (epl.MoveNext())
            {
                if (!epr.MoveNext() || !AreEqual(epl.Current.GetValue(left), epr.Current.GetValue(right)))
                    return false;
            }

            return true;
        }

        // Fast-path explicit equality implementation.
        if (left is IEquatable<T> leq)
            return leq.Equals(right);

        // Short circuit arrays, collections, lists, and dictionaries based on count difference.
        if (left is ICollection lc)
        {
            if (lc.Count != ((ICollection)right).Count)
                return false;
        }

        // Per item-based comparisons.
        if (left is IDictionary ld)
            return IDictionaryAreEqual(ld, (IDictionary)right);

        if (left is IEnumerable le)
            return IEnumerableAreEqual(le, (IEnumerable)right);

        // Special handling for JsonElement comparison.
        if (left is JsonElement lje && right is JsonElement rje)
#if NET8_0
            return CoreEx.Json.JsonMergePatch.DeepEquals(lje, rje);
#else
            return JsonElement.DeepEquals(lje, rje);
#endif

        // Default value type comparison.
        var type = left.GetType();
        if (type.IsValueType)
            return Equals(left, right);

        // Must be a class so use reflection-based runtime-metadata to compare each property.
        foreach (var p in GetCachedProperties(type).Values)
        {
            if (!AreEqual(p.GetValue(left), p.GetValue(right)))
                return false;
        }

        // Well, if we got this far, then they must be equal - good job (https://www.youtube.com/watch?v=BSmliwh7D30).
        return true;
    }

    /// <summary>
    /// Perform an equality comparison of two <see cref="IEnumerable"/> values.
    /// </summary>
    private static bool IEnumerableAreEqual(IEnumerable left, IEnumerable right)
    {
        // Slow path, possibly with boxing
        static bool EnumerateObjectAreEqual(IEnumerable l, IEnumerable r)
        {
            // Determine the element type and use cached dispatcher where possible.
            var elementType = GetEnumerableElementType(l);
            if (elementType is not null && elementType.IsValueType)
            {
                var dispatcher = GetEnumerableDispatcher(elementType);
                return dispatcher(l, r);
            }

            // Fallback to object comparison; which is ok where there is no boxing involved.
            var el = l.GetEnumerator();
            var er = r.GetEnumerator();
            while (el.MoveNext())
            {
                if (!(er.MoveNext() && AreEqual(el.Current, er.Current)))
                    return false;
            }

            return true;
        }

        return (left, right) switch
        {
            // Fast paths for common/hot types.
            (IEnumerable<string> ls, IEnumerable<string> rs) => TypedEnumerateAreEqual(ls, rs),
            (IEnumerable<Guid> lg, IEnumerable<Guid> rg) => TypedEnumerateAreEqual(lg, rg),
            (IEnumerable<Guid?> lgn, IEnumerable<Guid?> rgn) => TypedEnumerateAreEqual(lgn, rgn),
            (IEnumerable<int> li, IEnumerable<int> ri) => TypedEnumerateAreEqual(li, ri),
            (IEnumerable<int?> lin, IEnumerable<int?> rin) => TypedEnumerateAreEqual(lin, rin),
            (IEnumerable<long> ll, IEnumerable<long> rl) => TypedEnumerateAreEqual(ll, rl),
            (IEnumerable<long?> ll, IEnumerable<long?> rl) => TypedEnumerateAreEqual(ll, rl),
            (IEnumerable<DateTimeOffset> ld, IEnumerable<DateTimeOffset> rd) => TypedEnumerateAreEqual(ld, rd),
            (IEnumerable<DateTimeOffset?> ldg, IEnumerable<DateTimeOffset?> rdg) => TypedEnumerateAreEqual(ldg, rdg),

            // Use cached dispatcher for other types.
            _ => EnumerateObjectAreEqual(left, right)
        };
    }

    /// <summary>
    /// Perform an equality comparison of two <see cref="IDictionary"/> values.
    /// </summary>
    private static bool IDictionaryAreEqual(IDictionary left, IDictionary right)
    {
        var el = left.GetEnumerator();

        while (el.MoveNext())
        {
            if (!right.Contains(el.Key))
                return false;

            if (!AreEqual(el.Value, right[el.Key]))
                return false;
        }

        return true;
    }
}