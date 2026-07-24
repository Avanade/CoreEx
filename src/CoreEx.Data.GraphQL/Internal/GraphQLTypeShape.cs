namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Reflects over a DTO <see cref="Type"/> to build a lazily-cached, JSON-property-name-keyed shape used for both GraphQL selection-set validation/flattening and schema/discovery generation.
/// </summary>
/// <remarks>A property is considered <i>complex</i> (and therefore selectable/expandable via a nested GraphQL selection set) where its type (or, for collections, its element type) is a class or
/// struct other than <see cref="string"/> and the well-known scalar-like BCL types (<see cref="DateTime"/>, <see cref="Guid"/>, <see cref="decimal"/>, etc.) and reference data code types.</remarks>
internal static class GraphQLTypeShape
{
    private const int MaxDepth = 8;
    private static readonly ConcurrentDictionary<(Type Type, JsonSerializerOptions Options), IReadOnlyDictionary<string, GraphQLFieldNode>> _cache = new();

    private static readonly HashSet<Type> _scalarTypes =
    [
        typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid), typeof(Uri), typeof(char)
    ];

    /// <summary>
    /// Gets the field map for the specified <paramref name="type"/>.
    /// </summary>
    /// <remarks>Cached per <c>(<paramref name="type"/>, <paramref name="jsonOptions"/>)</c> pair (using <paramref name="jsonOptions"/> reference equality) so two engines registered with different
    /// <see cref="JsonSerializerOptions"/> (e.g. different naming policies) do not share a stale field map for the same DTO <see cref="Type"/>.</remarks>
    public static IReadOnlyDictionary<string, GraphQLFieldNode> GetFieldMap(Type type, JsonSerializerOptions jsonOptions) => _cache.GetOrAdd((type, jsonOptions), k => BuildFieldMap(k.Type, k.Options, 0));

    /// <summary>
    /// Builds the field map for the specified <paramref name="type"/>, recursing into complex properties up to <see cref="MaxDepth"/>.
    /// </summary>
    private static IReadOnlyDictionary<string, GraphQLFieldNode> BuildFieldMap(Type type, JsonSerializerOptions jsonOptions, int depth)
    {
        var map = new Dictionary<string, GraphQLFieldNode>(StringComparer.OrdinalIgnoreCase);
        if (depth >= MaxDepth)
            return map;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            var jsonIgnore = property.GetCustomAttribute<JsonIgnoreAttribute>();
            if (jsonIgnore is not null && jsonIgnore.Condition == JsonIgnoreCondition.Always)
                continue;

            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? jsonOptions.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            var (isComplex, elementType) = Classify(property.PropertyType);

            map[jsonName] = new GraphQLFieldNode(jsonName, property, property.PropertyType, isComplex, elementType,
                isComplex ? new Lazy<IReadOnlyDictionary<string, GraphQLFieldNode>>(() => BuildFieldMap(elementType ?? property.PropertyType, jsonOptions, depth + 1)) : null);
        }

        return map;
    }

    /// <summary>
    /// Classifies the specified <paramref name="type"/> as complex (selectable via nested selection set) or scalar, resolving any collection element type.
    /// </summary>
    private static (bool IsComplex, Type? ElementType) Classify(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsEnum || _scalarTypes.Contains(underlying) || typeof(IReferenceData).IsAssignableFrom(underlying))
            return (false, null);

        if (underlying != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying))
        {
            var elementType = GetEnumerableElementType(underlying);
            if (elementType is null)
                return (false, null);

            var (elementIsComplex, _) = Classify(elementType);
            return elementIsComplex ? (true, elementType) : (false, null);
        }

        return (underlying.IsClass || (underlying.IsValueType && !underlying.IsPrimitive), null);
    }

    /// <summary>
    /// Gets the element <see cref="Type"/> of the specified enumerable <paramref name="type"/> (or <see langword="null"/> where it cannot be determined).
    /// </summary>
    private static Type? GetEnumerableElementType(Type type)
    {
        foreach (var i in type.GetInterfaces().Append(type))
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return i.GetGenericArguments()[0];
        }

        return null;
    }
}

/// <summary>
/// Represents a single reflected field within a <see cref="GraphQLTypeShape"/> field map.
/// </summary>
/// <param name="JsonName">The JSON property name.</param>
/// <param name="Property">The underlying <see cref="PropertyInfo"/>.</param>
/// <param name="PropertyType">The declared property <see cref="Type"/>.</param>
/// <param name="IsComplex">Indicates whether the field is complex (selectable via a nested GraphQL selection set).</param>
/// <param name="ElementType">Where <see cref="IsComplex"/> and the property is a collection, the collection's element <see cref="Type"/>; otherwise, <see langword="null"/>.</param>
/// <param name="Children">The lazily-built nested field map (only populated where <see cref="IsComplex"/>).</param>
internal sealed record GraphQLFieldNode(string JsonName, PropertyInfo Property, Type PropertyType, bool IsComplex, Type? ElementType, Lazy<IReadOnlyDictionary<string, GraphQLFieldNode>>? Children);
