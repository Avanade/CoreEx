namespace CoreEx.IO.Json;

/// <summary>Configures System.Text.Json source names and scalar converters for a nullable wire graph.</summary>
/// <typeparam name="TRecord">The wire object type.</typeparam>
public sealed class JsonUnitMapping<TRecord> : IJsonUnitMapping where TRecord : class, new()
{
    private readonly List<Field> _fields = [];
    private readonly Dictionary<(int Depth, JsonUnknownMemberPolicy Unknown, string? Structural), JsonSerializerOptions> _options = [];

    /// <summary>Configures a nullable scalar using a standard System.Text.Json converter.</summary>
    /// <typeparam name="TValue">The scalar type.</typeparam>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">The writable wire property.</param>
    /// <param name="converter">The scalar converter.</param>
    /// <returns>This contract configuration.</returns>
    public JsonUnitMapping<TRecord> MapValue<TValue>(string name, Expression<Func<TRecord, TValue?>> property, JsonConverter<TValue> converter) where TValue : struct
        => Add(name, property, ReferenceEquals(converter.ThrowIfNull(), JsonFieldConverters.Default<TValue>()) ? null : new NullableConverter<TValue>(converter));

    /// <summary>Configures a string property without blank-to-null normalization.</summary>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">The writable wire property.</param>
    /// <returns>This contract configuration.</returns>
    public JsonUnitMapping<TRecord> MapString(string name, Expression<Func<TRecord, string?>> property) => Add(name, property);

    /// <summary>Configures a nested object using its standard serializer contract.</summary>
    /// <typeparam name="TChild">The nested wire type.</typeparam>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">The writable wire property.</param>
    /// <param name="mapping">The nested serializer contract configuration.</param>
    /// <returns>This contract configuration.</returns>
    public JsonUnitMapping<TRecord> MapObject<TChild>(string name, Expression<Func<TRecord, TChild?>> property, JsonUnitMapping<TChild> mapping) where TChild : class, new()
        => Add(name, property, child: mapping.ThrowIfNull());

    /// <summary>Configures an object collection; count and item rules belong in the wire validator.</summary>
    /// <typeparam name="TChild">The collection element type.</typeparam>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">The writable wire property.</param>
    /// <param name="mapping">The element serializer contract configuration.</param>
    /// <returns>This contract configuration.</returns>
    public JsonUnitMapping<TRecord> MapObjects<TChild>(string name, Expression<Func<TRecord, List<TChild>?>> property, JsonUnitMapping<TChild> mapping) where TChild : class, new()
        => Add(name, property, child: mapping.ThrowIfNull());

    /// <summary>Configures a scalar collection using standard serializer collection handling.</summary>
    /// <typeparam name="TValue">The collection element type.</typeparam>
    /// <param name="name">The exact source member name.</param>
    /// <param name="property">The writable wire property.</param>
    /// <param name="converter">The element scalar converter.</param>
    /// <returns>This contract configuration.</returns>
    public JsonUnitMapping<TRecord> MapValues<TValue>(string name, Expression<Func<TRecord, List<TValue>?>> property, JsonConverter<TValue> converter)
        => Add(name, property, ReferenceEquals(converter.ThrowIfNull(), JsonFieldConverters.Default<TValue>()) ? null : new ScalarCollectionConverter<TValue>(converter));

    /// <summary>Records property metadata, leaving object creation and member assignment to System.Text.Json.</summary>
    private JsonUnitMapping<TRecord> Add<TValue>(string name, Expression<Func<TRecord, TValue>> property, JsonConverter? converter = null, IJsonUnitMapping? child = null)
    {
        name.ThrowIfNull();
        property.ThrowIfNull();
        if (property.Body is not MemberExpression { Expression: ParameterExpression, Member: System.Reflection.PropertyInfo { CanWrite: true } member })
            throw new ArgumentException("Select a writable direct property.", nameof(property));

        if (_fields.Any(x => x.Name == name || x.Property == member.Name))
            throw new ArgumentException("Source members and destination properties must be unique.", nameof(name));

        var target = Expression.Parameter(typeof(TRecord));
        var clear = Expression.Lambda<Action<TRecord>>(Expression.Assign(Expression.Property(target, member), Expression.Default(typeof(TValue))), target).Compile();
        _fields.Add(new(name, member.Name, CoreEx.Metadata.RuntimeMetadata.GetForExpression(property).GetJsonName(), converter, child, clear));
        _options.Clear();
        return this;
    }

    /// <inheritdoc/>
    void IJsonUnitMapping.RegisterContracts(Dictionary<Type, IJsonUnitMapping> contracts, HashSet<IJsonUnitMapping> visited)
    {
        if (!visited.Add(this))
            return;

        if (contracts.TryGetValue(typeof(TRecord), out var existing))
        {
            if (!((IJsonUnitMapping)this).HasSameShape(existing))
                throw new InvalidOperationException("Each wire CLR type must have one consistent serializer contract within a definition.");
        }
        else
            contracts.Add(typeof(TRecord), this);

        foreach (var field in _fields)
            field.Child?.RegisterContracts(contracts, visited);
    }

    /// <inheritdoc/>
    bool IJsonUnitMapping.HasSameShape(IJsonUnitMapping other)
        => other is JsonUnitMapping<TRecord> mapping && _fields.Select(x => (x.Name, x.Property, x.Converter)).SequenceEqual(mapping._fields.Select(x => (x.Name, x.Property, x.Converter)));

    /// <inheritdoc/>
    void IJsonUnitMapping.Configure(JsonTypeInfo typeInfo, string? structuralMember)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            throw new InvalidOperationException("A file wire type must use the standard System.Text.Json object contract.");

        var properties = typeInfo.Properties.ToArray();
        typeInfo.Properties.Clear();
        foreach (var field in _fields)
        {
            var property = properties.Single(x => (x.AttributeProvider as System.Reflection.PropertyInfo)?.Name == field.Property);
            property.Name = field.Name;
            property.CustomConverter = field.Converter;
            property.NumberHandling = JsonNumberHandling.Strict;
            typeInfo.Properties.Add(property);
        }

        if (structuralMember is not null && !_fields.Any(x => x.Name == structuralMember))
        {
            var discriminator = typeInfo.CreateJsonPropertyInfo(typeof(JsonElement), structuralMember);
            discriminator.Set = (_, _) => { };
            typeInfo.Properties.Add(discriminator);
        }

        var callback = typeInfo.OnDeserializing;
        typeInfo.OnDeserializing = value =>
        {
            foreach (var field in _fields)
                field.Clear((TRecord)value);

            callback?.Invoke(value);
        };
    }

    /// <summary>Creates and caches the standard serializer options for this pass configuration.</summary>
    private JsonSerializerOptions GetOptions(JsonFileOptions options, string? structuralMember = null)
    {
        var key = (options.MaxDepth, options.UnknownMemberPolicy, structuralMember);
        if (_options.TryGetValue(key, out var existing))
            return existing;

        var contracts = new Dictionary<Type, IJsonUnitMapping>();
        ((IJsonUnitMapping)this).RegisterContracts(contracts, []);
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (contracts.TryGetValue(typeInfo.Type, out var contract))
                contract.Configure(typeInfo, typeInfo.Type == typeof(TRecord) ? structuralMember : null);
        });
        var serializerOptions = new JsonSerializerOptions
        {
            MaxDepth = options.MaxDepth,
            TypeInfoResolver = resolver,
            UnmappedMemberHandling = options.UnknownMemberPolicy == JsonUnknownMemberPolicy.Reject ? JsonUnmappedMemberHandling.Disallow : JsonUnmappedMemberHandling.Skip
        };
        _options.Add(key, serializerOptions);
        return serializerOptions;
    }

    /// <inheritdoc/>
    async Task<object?> IJsonUnitMapping.DeserializeAsync(JsonDocumentCursor cursor, JsonMappingContext context, string sourcePath, CancellationToken cancellationToken)
    {
        using var source = new JsonSelectedObjectStream(cursor);
        TRecord? value = null;
        JsonException? error = null;
        try
        {
            value = await JsonSerializer.DeserializeAsync<TRecord>(source, GetOptions(context.Options), cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception) when (!ReferenceEquals(exception, source.ReadFailure))
        {
            error = exception;
        }

        await source.DrainAsync(cancellationToken).ConfigureAwait(false);
        if (cursor.Failed || cursor.InvalidValue || context.Stopped)
            return null;

        if (error is not null)
            await ReportAsync(error, context, string.Empty, sourcePath, cancellationToken).ConfigureAwait(false);

        return value;
    }

    /// <inheritdoc/>
    async Task<object?> IJsonUnitMapping.MapAsync(JsonElement element, JsonMappingContext context, string propertyPath, string sourcePath, string? structuralMember, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TRecord? value = null;
        JsonException? error = null;
        try
        {
            value = element.Deserialize<TRecord>(GetOptions(context.Options, structuralMember));
        }
        catch (JsonException exception)
        {
            error = exception;
        }

        if (error is not null)
            await ReportAsync(error, context, propertyPath, sourcePath, cancellationToken).ConfigureAwait(false);

        return value;
    }

    /// <summary>Reports only the serializer's first conversion error without including raw messages or unknown keys.</summary>
    private Task ReportAsync(JsonException error, JsonMappingContext context, string propertyPath, string sourcePath, CancellationToken cancellationToken)
    {
        var path = error.Path ?? "$";
        if (error.InnerException is JsonException { Path: { } innerPath })
            path += innerPath.StartsWith('$') ? innerPath[1..] : innerPath;

        var location = ((IJsonUnitMapping)this).ResolveErrorPath(path);
        return context.FailAsync(location.Unknown ? "json.member.unexpected" : "json.field.type",
            JsonMappingContext.Property(propertyPath, location.Property), JsonPointer.Combine(sourcePath, location.Source), cancellationToken);
    }

    /// <inheritdoc/>
    (string Property, string Source, bool Unknown) IJsonUnitMapping.ResolveErrorPath(string path)
    {
        if (path.Length == 0 || path == "$")
            return (string.Empty, string.Empty, false);

        var rest = path.StartsWith('$') ? path[1..] : path;
        foreach (var field in _fields.OrderByDescending(x => x.Name.Length))
        {
            var member = rest.StartsWith("." + field.Name, StringComparison.Ordinal) ? "." + field.Name : "['" + field.Name + "']";
            if (!rest.StartsWith(member, StringComparison.Ordinal) || rest.Length > member.Length && rest[member.Length] is not ('.' or '['))
                continue;

            rest = rest[member.Length..];
            var property = field.Property;
            var source = JsonMappingContext.Source(string.Empty, field.Name);
            if (rest.StartsWith('['))
            {
                var close = rest.IndexOf(']');
                if (close > 0 && int.TryParse(rest.AsSpan(1, close - 1), NumberStyles.None, CultureInfo.InvariantCulture, out _))
                {
                    property += rest[..(close + 1)];
                    source += "/" + rest[1..close];
                    rest = rest[(close + 1)..];
                }
            }

            if (rest.Length == 0)
                return (property, source, false);

            if (field.Child is not null)
            {
                var child = field.Child.ResolveErrorPath(rest);
                return (child.Property.Length == 0 ? property : property + "." + child.Property, source + child.Source, child.Unknown);
            }

            return (property, source, false);
        }

        return (string.Empty, string.Empty, true);
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

    /// <summary>Holds declared serializer metadata without a token-to-object mapping delegate.</summary>
    private sealed record Field(string Name, string Property, string JsonName, JsonConverter? Converter, IJsonUnitMapping? Child, Action<TRecord> Clear);

    /// <summary>Adapts a scalar converter to the nullable wire-property type.</summary>
    private sealed class NullableConverter<TValue>(JsonConverter<TValue> converter) : JsonConverter<TValue?> where TValue : struct
    {
        private readonly JsonConverter<TValue> _converter = converter;

        /// <inheritdoc/>
        public override TValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null ? null : _converter.Read(ref reader, typeof(TValue), options);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TValue? value, JsonSerializerOptions options)
        {
            if (value is { } scalar)
                _converter.Write(writer, scalar, options);
            else
                writer.WriteNullValue();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is NullableConverter<TValue> other && _converter.Equals(other._converter);

        /// <inheritdoc/>
        public override int GetHashCode() => _converter.GetHashCode();
    }

    /// <summary>Delegates collection reading to System.Text.Json with a property-specific scalar format.</summary>
    private sealed class ScalarCollectionConverter<TValue>(JsonConverter<TValue> converter) : JsonConverter<List<TValue>>
    {
        private readonly JsonConverter<TValue> _converter = converter;
        private readonly JsonSerializerOptions _scalarOptions = new() { Converters = { converter } };

        /// <inheritdoc/>
        public override List<TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                return JsonSerializer.Deserialize<List<TValue>>(ref reader, _scalarOptions);
            }
            catch (JsonException exception)
            {
                throw new JsonException("The collection contains a value that does not match the declared contract.", exception);
            }
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, List<TValue> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, _scalarOptions);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is ScalarCollectionConverter<TValue> other && _converter.Equals(other._converter);

        /// <inheritdoc/>
        public override int GetHashCode() => _converter.GetHashCode();
    }
}
