namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    [ThreadStatic]
    private static HashSet<object>? _visitingForClean;

    /// <summary>
    /// Cleans (deep) the mutable properties of the <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="args">The optional <see cref="CleanArgs"/> (defaults to <see cref="CleanArgs.Default"/>).</param>
    /// <returns>The cleaned <paramref name="value"/>.</returns>
    /// <remarks>This will walk the fully object graph, including arrays, collections, and dictionaries cleaning all mutable properties. Note that where the entry for an array, collection, or dictionary is a value type
    /// this is unable to be cleaned/replaced. An empty array, collection, or dictionary will be set to <see langword="default"/>.</remarks>
    public static T? Clean<T>(T? value, CleanArgs args = default)
    {
        if (value is string str)
            return Internal.Cast<string, T>(Cleaner.Clean(str, Cleaner.DefaultStringTrim, Cleaner.DefaultStringTransform, Cleaner.DefaultStringCase)!);

        if (value is null)
            return value;

        if (value is DateTime dt)
            return Internal.Cast<DateTime, T>(Cleaner.Clean(dt, Cleaner.DefaultDateTimeTransform));

        // All reference-type branches below can form cycles — allocate the visited set once per thread and reuse it.
        var set = _visitingForClean ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        var isRoot = set.Count == 0;
        try
        {
            if (value is IRuntimeMetadataCore rm)
            {
                if (!set.Add(value))
                    return value; // cycle detected — return as-is

                foreach (var p in rm.GetPropertyRuntimeMetadata().Where(x => !x.IsReadOnly))
                    p.Clean(value, args);

                set.Remove(value); // allow re-visit from a different path (DAG support)
                return (isRoot ? args.CleanAndDefaultRoot : args.CleanAndDefaultNested) && Cleaner.GetCleanOption(value.GetType()) == CleanOption.CleanAndDefault && RuntimeMetadata.IsDefault(value) ? default : value;
            }

            // Zero-length collections are nulled out.
            if (value is ICollection ic && ic.Count == 0)
                return default;

            // Clean each dictionary item (does not replace/null entry, only contents thereof); key remains unchanged.
            if (value is IDictionary d)
            {
                foreach (DictionaryEntry de in d)
                    Clean(de.Value, args);

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
                    Clean(item, args);

                return value;
            }

            // Handle value or class types.
            var type = value.GetType();
            if (type.IsValueType)
                return value; // value types (other than string/DateTime, handled above) have no cleaning to perform

            if (!set.Add(value))
                return value; // cycle detected — return as-is

            foreach (var p in GetCachedProperties(type).Values.Where(x => !x.IsReadOnly))
                p.Clean(value, args);

            set.Remove(value);
            return (isRoot ? args.CleanAndDefaultRoot : args.CleanAndDefaultNested) && Cleaner.GetCleanOption(type) == CleanOption.CleanAndDefault && RuntimeMetadata.IsDefault(value) ? default : value;
        }
        finally
        {
            if (isRoot)
                set.Clear();
        }
    }
}
