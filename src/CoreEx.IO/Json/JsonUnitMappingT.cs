namespace CoreEx.IO.Json;

/// <summary>Maps JSON direct members to a natural nullable wire graph in declaration order.</summary>
/// <typeparam name="TRecord">The wire object type.</typeparam>
public sealed class JsonUnitMapping<TRecord> : IJsonUnitMapping where TRecord : class, new()
{
    private readonly List<Field> _fields = [];

    /// <summary>Maps a nullable value-type scalar; absent/null tokens leave the property null.</summary>
    /// <typeparam name="TValue">The converted scalar type.</typeparam>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">A direct writable property on the wire object.</param>
    /// <param name="converter">The strict JSON token converter.</param>
    /// <returns>This mapping for fluent configuration.</returns>
    public JsonUnitMapping<TRecord> MapValue<TValue>(string name, Expression<Func<TRecord, TValue?>> property, JsonValueConverter<TValue> converter) where TValue : struct
    {
        converter.ThrowIfNull();
        var setter = CreateSetter(property);
        return Add(name, property, async (record, element, context, model, source, cancellationToken) =>
        {
            if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                setter(record, null);
            else if (converter(element, out var value))
                setter(record, value);
            else
                await context.FailAsync("json.field.type", model, source, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>Maps text without blank-to-null normalization.</summary>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">A direct writable string property on the wire object.</param>
    /// <returns>This mapping for fluent configuration.</returns>
    public JsonUnitMapping<TRecord> MapString(string name, Expression<Func<TRecord, string?>> property)
    {
        var setter = CreateSetter(property);
        return Add(name, property, async (record, element, context, model, source, cancellationToken) =>
        {
            if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                setter(record, null);
            else if (JsonFieldConverters.String(element, out var value))
                setter(record, value);
            else
                await context.FailAsync("json.field.type", model, source, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>Maps an optional object occurrence; requiredness is enforced by its pure validator.</summary>
    /// <typeparam name="TChild">The nested wire object type.</typeparam>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">A direct writable nested-object property on the wire object.</param>
    /// <param name="mapping">The nested object's field mapping.</param>
    /// <returns>This mapping for fluent configuration.</returns>
    public JsonUnitMapping<TRecord> MapObject<TChild>(string name, Expression<Func<TRecord, TChild?>> property, JsonUnitMapping<TChild> mapping) where TChild : class, new()
    {
        mapping.ThrowIfNull();
        var setter = CreateSetter(property);
        return Add(name, property, async (record, element, context, model, source, cancellationToken) =>
        {
            if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                setter(record, null);
            else if (element.ValueKind != JsonValueKind.Object)
                await context.FailAsync("json.field.type", model, source, cancellationToken).ConfigureAwait(false);
            else
                setter(record, (TChild)await ((IJsonUnitMapping)mapping).MapAsync(element, context, model, source, null, cancellationToken).ConfigureAwait(false));
        }, mapping);
    }

    /// <summary>Maps a collection of non-null object elements.</summary>
    /// <typeparam name="TChild">The nested wire object type.</typeparam>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">A direct writable collection property on the wire object.</param>
    /// <param name="mapping">The nested object's field mapping.</param>
    /// <param name="cardinality">The required number of collection elements.</param>
    /// <param name="maxItems">An optional business constraint on the number of elements.</param>
    /// <returns>This mapping for fluent configuration.</returns>
    public JsonUnitMapping<TRecord> MapObjects<TChild>(string name, Expression<Func<TRecord, List<TChild>?>> property, JsonUnitMapping<TChild> mapping,
        FileCardinality cardinality = FileCardinality.Many, int? maxItems = null) where TChild : class, new()
        => AddCollection(name, property, async (element, context, model, source, cancellationToken) =>
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                await context.FailAsync("json.field.type", model, source, cancellationToken).ConfigureAwait(false);
                return null;
            }

            return (TChild)await ((IJsonUnitMapping)mapping).MapAsync(element, context, model, source, null, cancellationToken).ConfigureAwait(false);
        }, cardinality, maxItems, mapping.ThrowIfNull());

    /// <summary>Maps a collection of non-null scalar elements using strict token conversion.</summary>
    /// <typeparam name="TValue">The converted scalar element type.</typeparam>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">A direct writable collection property on the wire object.</param>
    /// <param name="converter">The strict JSON token converter for each element.</param>
    /// <param name="cardinality">The required number of collection elements.</param>
    /// <param name="maxItems">An optional business constraint on the number of elements.</param>
    /// <returns>This mapping for fluent configuration.</returns>
    public JsonUnitMapping<TRecord> MapValues<TValue>(string name, Expression<Func<TRecord, List<TValue>?>> property, JsonValueConverter<TValue> converter,
        FileCardinality cardinality = FileCardinality.Many, int? maxItems = null)
    {
        converter.ThrowIfNull();
        return AddCollection(name, property, async (element, context, model, source, cancellationToken) =>
        {
            if (element.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) && converter(element, out var value))
                return value;

            await context.FailAsync("json.field.type", model, source, cancellationToken).ConfigureAwait(false);
            return default;
        }, cardinality, maxItems, null);
    }

    /// <summary>Checks collection shape/count before allocating or mapping its elements.</summary>
    private JsonUnitMapping<TRecord> AddCollection<TValue>(string name, Expression<Func<TRecord, List<TValue>?>> property,
        Func<JsonElement, JsonMappingContext, string, string, CancellationToken, Task<TValue?>> map, FileCardinality cardinality, int? maxItems, IJsonUnitMapping? child)
    {
        if (maxItems <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxItems));
        if (!Enum.IsDefined(cardinality))
            throw new ArgumentOutOfRangeException(nameof(cardinality));

        var setter = CreateSetter(property);
        return Add(name, property, async (record, element, context, model, source, cancellationToken) =>
        {
            var absent = element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;
            if (!absent && element.ValueKind != JsonValueKind.Array)
            {
                await context.FailAsync("json.field.type", model, source, cancellationToken).ConfigureAwait(false);
                return;
            }

            var count = absent ? 0 : element.GetArrayLength();
            if (count > maxItems || !JsonMappingContext.Allows(cardinality, count))
            {
                await context.FailAsync("file.structure.cardinality", model, source, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (absent)
            {
                setter(record, null);
                return;
            }

            var values = new List<TValue>(count);
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (context.Stopped)
                    break;

                var value = await map(item, context, model + "[" + index + "]", source + "/" + index, cancellationToken).ConfigureAwait(false);
                if (value is not null)
                    values.Add(value);

                index++;
            }

            setter(record, values);
        }, child);
    }

    /// <summary>Adds one unique direct-member occurrence and compiled setter.</summary>
    private JsonUnitMapping<TRecord> Add<TValue>(string name, Expression<Func<TRecord, TValue>> property,
        Func<TRecord, JsonElement, JsonMappingContext, string, string, CancellationToken, Task> map, IJsonUnitMapping? child = null)
    {
        name.ThrowIfNull();
        var member = (property.Body as MemberExpression)?.Member as System.Reflection.PropertyInfo
            ?? throw new ArgumentException("Select a writable direct property.", nameof(property));
        if (_fields.Any(x => x.Name == name || x.Property == member.Name))
            throw new ArgumentException("Source members and destination properties must be unique.", nameof(name));

        var metadata = CoreEx.Metadata.RuntimeMetadata.GetForExpression(property);
        _fields.Add(new(name, member.Name, metadata.GetJsonName(), map, child));
        return this;
    }

    /// <summary>Compiles a direct writable property assignment without reflection during processing.</summary>
    private static Action<TRecord, TValue> CreateSetter<TValue>(Expression<Func<TRecord, TValue>> property)
    {
        property.ThrowIfNull();
        if (property.Body is not MemberExpression { Expression: ParameterExpression, Member: System.Reflection.PropertyInfo { CanWrite: true } })
            throw new ArgumentException("Select a writable direct property.", nameof(property));

        var value = Expression.Parameter(typeof(TValue));
        return Expression.Lambda<Action<TRecord, TValue>>(Expression.Assign(property.Body, value), property.Parameters[0], value).Compile();
    }

    /// <inheritdoc/>
    async Task<object> IJsonUnitMapping.MapAsync(JsonElement element, JsonMappingContext context, string propertyPath, string sourcePath, string? structuralMember, CancellationToken cancellationToken)
    {
        var record = new TRecord();
        if (context.Options.UnknownMemberPolicy == JsonUnknownMemberPolicy.Reject)
            foreach (var member in element.EnumerateObject())
            {
                if (context.Stopped)
                    return record;

                if (member.Name != structuralMember && !_fields.Any(x => x.Name == member.Name))
                    await context.FailAsync("json.member.unexpected", propertyPath, sourcePath, cancellationToken).ConfigureAwait(false);
            }

        foreach (var field in _fields)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context.Stopped)
                break;

            element.TryGetProperty(field.Name, out var value);
            await field.Map(record, value, context, JsonMappingContext.Property(propertyPath, field.Property), JsonMappingContext.Source(sourcePath, field.Name), cancellationToken).ConfigureAwait(false);
        }

        return record;
    }

    /// <inheritdoc/>
    string IJsonUnitMapping.ResolveSourcePath(string propertyPath) => ResolvePath(propertyPath, true);

    /// <inheritdoc/>
    string IJsonUnitMapping.ResolvePropertyPath(string propertyPath) => ResolvePath(propertyPath, false);

    /// <summary>Resolves model/serializer segments through declared occurrences without guessing source names.</summary>
    private string ResolvePath(string propertyPath, bool source)
    {
        var end = propertyPath.IndexOfAny(['.', '[']);
        var name = end < 0 ? propertyPath : propertyPath[..end];
        var field = _fields.SingleOrDefault(x => x.Property == name) ?? _fields.SingleOrDefault(x => x.JsonName == name);
        if (field is null)
            return source ? string.Empty : propertyPath;

        var path = source ? JsonMappingContext.Source(string.Empty, field.Name) : field.Property;
        var rest = end < 0 ? string.Empty : propertyPath[end..];
        if (rest.StartsWith('['))
        {
            var close = rest.IndexOf(']');
            if (close < 0)
                return path;

            path += source ? "/" + rest[1..close] : rest[..(close + 1)];
            rest = rest[(close + 1)..];
        }

        return path + (rest.StartsWith('.') && field.Child is not null
            ? source ? field.Child.ResolveSourcePath(rest[1..]) : "." + field.Child.ResolvePropertyPath(rest[1..])
            : source ? string.Empty : rest);
    }

    /// <summary>Describes one reusable type's independently named occurrence.</summary>
    private sealed record Field(string Name, string Property, string JsonName,
        Func<TRecord, JsonElement, JsonMappingContext, string, string, CancellationToken, Task> Map, IJsonUnitMapping? Child);
}
