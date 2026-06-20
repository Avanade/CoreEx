namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    /// <summary>
    /// Gets the <see cref="IEnumerable"/> element type.
    /// </summary>
    private static Type? GetEnumerableElementType(object o) => GetEnumerableElementType(o.GetType());

    /// <summary>
    /// Gets the <see cref="IEnumerable"/> element type.
    /// </summary>
    private static Type? GetEnumerableElementType(Type type)
    {
        // Arrays are easy!
        if (type.IsArray)
            return type.GetElementType();

        // Ok, now we need some reflection magic - must cache.
        return Internal.MemoryCache.GetOrCreate<Type?>($"RuntimeMetadata_ElementType_{type.FullName}", entry =>
        {
            entry.SlidingExpiration = SlidingExpirationTimespan;

            // The type itself might be IEnumerable<T>.
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // Or it implements IEnumerable<T>.
            var enumIface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumIface?.GetGenericArguments()[0];
        });
    }

    /// <summary>
    /// Gets (or creates) the cached delegate that compares two sequences of the specified element type for equality.
    /// </summary>
    private static Func<object, object, bool> GetEnumerableDispatcher(Type elementType) => Internal.MemoryCache.GetOrCreate<Func<object, object, bool>>($"RuntimeMetadata_Dispatcher_{elementType.FullName}", entry =>
    {
        // Create the generic: TypedEnumerateAreEqual<T>(IEnumerable<T>, IEnumerable<T>)
        var method = typeof(RuntimeMetadata).GetMethod(nameof(TypedEnumerateAreEqual), BindingFlags.NonPublic | BindingFlags.Static)!;
        var generic = method.MakeGenericMethod(elementType);

        // Build a delegate: (object left, object right) => TypedEnumerateAreEqual<T>((IEnumerable<T>)left, (IEnumerable<T>)right)
        var leftParam = Expression.Parameter(typeof(object), "left");
        var rightParam = Expression.Parameter(typeof(object), "right");

        var ienumerable = typeof(IEnumerable<>).MakeGenericType(elementType);
        var leftCast = Expression.Convert(leftParam, ienumerable);
        var rightCast = Expression.Convert(rightParam, ienumerable);

        var call = Expression.Call(generic, leftCast, rightCast);
        var lambda = Expression.Lambda<Func<object, object, bool>>(call, leftParam, rightParam);

        // Paid once, then cache.
        entry.SlidingExpiration = SlidingExpirationTimespan;
        return lambda.Compile();
    })!;

    /// <summary>
    /// Perform an equality comparison of two typed <see cref="IEnumerable{T}"/> values.
    /// </summary>
    private static bool TypedEnumerateAreEqual<T>(IEnumerable<T> l, IEnumerable<T> r)
    {
        var el = l.GetEnumerator();
        var er = r.GetEnumerator();

        while (el.MoveNext())
        {
            if (!(er.MoveNext() && AreEqual(el.Current, er.Current)))
                return false;
        }

        return true;
    }

}