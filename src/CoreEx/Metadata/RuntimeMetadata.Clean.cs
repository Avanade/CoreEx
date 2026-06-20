namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    /// <summary>
    /// Cleans (deep) the mutable properties of the <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The cleaned <paramref name="value"/>.</returns>
    /// <remarks>This will walk the fully object graph, including arrays, collections, and dictionaries cleaning all mutable properties. Note that where the entry for an array, collection, or dictionary is a value type
    /// this is unable to be cleaned/replaced. An empty array, collection, or dictionary will be set to <see langword="default"/>.</remarks>
    public static T? Clean<T>(T value)
    {
        if (value is string str)
            return Internal.Cast<string, T>(Cleaner.Clean(str, Cleaner.DefaultStringTrim, Cleaner.DefaultStringTransform, Cleaner.DefaultStringCase)!);

        if (value is null)
            return value;

        if (value is DateTime dt)
            return Internal.Cast<DateTime, T>(Cleaner.Clean(dt, Cleaner.DefaultDateTimeTransform));

        if (value is IRuntimeMetadataCore rm)
        {
            foreach (var p in rm.GetPropertyRuntimeMetadata().Where(x => !x.IsReadOnly))
            {
                p.Clean(value);
            }

            return RuntimeMetadata.IsDefault(value) ? default : value;
        }

        // Zero-length collections are nulled out.
        if (value is ICollection ic && ic.Count == 0)
            return default;

        // Clean each dictionary item (does not replace/null entry, only contents thereof); key remains unchanged.
        if (value is IDictionary d)
        {
            foreach (DictionaryEntry de in d)
            {
                Clean(de.Value);
            }

            return value;
        }

        // Clean each enumerable item (does not replace/null entry, only contents thereof).
        if (value is IEnumerable e)
        {
            // Fast-path common/hot types to avoid boxing - can't clean anyway!
            if (value is ICollection<string> || value is ICollection<Guid> || value is ICollection<Guid?>
                || value is ICollection<int> || value is ICollection<int?> || value is ICollection<long> || value is ICollection<long?>)
                return value;

            // Get the element type to determine if boxing will occur and bail if so - can't clean anyway!
            var elementType = GetEnumerableElementType(value);
            if (elementType is not null && elementType.IsValueType)
                return value;

            foreach (var item in e)
            {
                Clean(item);
            }

            return value;
        }

        // Handle value or class types.
        var type = value.GetType();
        if (type.IsValueType)
            return Cleaner.Clean(value);

        foreach (var p in GetCachedProperties(value.GetType()).Values.Where(x => x.IsReadOnly))
        {
            p.Clean(value);
        }

        return RuntimeMetadata.IsDefault(value) ? default : value;
    }
}