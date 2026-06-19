namespace CoreEx.Events;

public partial class EventData
{
    /// <summary>
    /// Sets the <paramref name="entity"/> as the <see cref="Entity"/>.
    /// </summary>
    /// <param name="entity">The entity name.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithEntity(string entity) => this.Adjust(x => x.Entity = entity.ThrowIfNullOrEmpty());

    /// <summary>
    /// Sets the <see cref="Entity"/> based on the specified <typeparamref name="TEntity"/> (using <see cref="Schema.TryGetMetadata{TEntity}(out SchemaAttribute)"/>).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithEntity<TEntity>()
    {
        Schema.TryGetMetadata<TEntity>(out var metadata);
        return WithEntity(metadata.Name ?? typeof(TEntity).Name);
    }

    /// <summary>
    /// Sets the <paramref name="action"/> as the <see cref="Action"/>.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithAction(string action) => this.Adjust(x => x.Action = action.ThrowIfNullOrEmpty());

    /// <summary>
    /// Sets the <paramref name="enum"/> as the <see cref="Action"/>.
    /// </summary>
    /// <param name="enum">The <see cref="Enum"/> value that represents the action.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithAction(Enum @enum) => WithAction(@enum.ThrowIfNull().ToString());

    /// <summary>
    /// Sets the <paramref name="key"/> as the <see cref="Key"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="key"/> <b>must</b> be a universal, deterministic, and culture-independent <see cref="string"/>; where in doubt use <see cref="WithKey(CompositeKey)"/> which will enable.</remarks>
    public EventData WithKey(string key)
    {
        Key = key.ThrowIfNullOrEmpty();
        return this;
    }

    /// <summary>
    /// Sets the <paramref name="key"/> as the <see cref="Key"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithKey(CompositeKey key) => WithKey(key.ToString().ThrowIfNull());

    /// <summary>
    /// Sets the <paramref name="partitionKey"/> as the <see cref="PartitionKey"/>.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    /// <remarks>A <see langword="null"/> or empty <paramref name="partitionKey"/> will result in a <see cref="Guid.NewGuid"/> being set as the <see cref="PartitionKey"/>. This will ensure that at least a
    /// partition key is set with a somewhat randomized value to ensure a level of distributed processing. Noting that there will be no guarantees of order; where order is required then a common/deterministic
    /// value should be used.
    /// <para>When using <see cref="WithValue{T}(T, IEnumerable{string})"/> this will be set automatically from the <see cref="IReadOnlyPartitionKey.PartitionKey"/> where applicable. Otherwise, the
    /// <see cref="EventFormatter"/></para> will attempt to set.</remarks>
    public EventData WithPartitionKey(string? partitionKey = null) => this.Adjust(x => x.PartitionKey = string.IsNullOrEmpty(partitionKey) ? Guid.NewGuid().ToString() : partitionKey);

    /// <summary>
    /// Sets the <paramref name="domainName"/> as the <see cref="DomainName"/>.
    /// </summary>
    /// <param name="domainName">The domain (DDD) name.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithDomain(string domainName) => this.Adjust(x => x.DomainName = domainName.ThrowIfNullOrEmpty());

    /// <summary>
    /// Sets the schema <paramref name="version"/> to be included.
    /// </summary>
    /// <param name="version">The schema <see cref="Version"/>.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithVersion(Version version) => this.Adjust(x => x.DataSchemaVersion = version.ThrowIfNull());

    /// <summary>
    /// Sets the <paramref name="user"/> to be included.
    /// </summary>
    /// <param name="user">The <see cref="AuthenticationUser"/>.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithUser(AuthenticationUser user)
    {
        UserType = user.ThrowIfNull().Type;
        UserId = user.Id;
        return this;
    }

    /// <summary>
    /// Sets the <paramref name="schema"/> <see cref="Uri"/> to be included.
    /// </summary>
    /// <param name="schema">The schema <see cref="Uri"/>.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    public EventData WithSchema(Uri schema) => this.Adjust(x => x.DataSchema = schema.ThrowIfNull());

    /// <summary>
    /// Sets the <paramref name="title"/> as the <see cref="Title"/> override.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="Title"/> documentation for more details on its intended usage.</remarks>
    public EventData WithTitle(string title) => this.Adjust(x => x.Title = title.ThrowIfNull());

    /// <summary>
    /// Sets the <paramref name="source"/> as the <see cref="Source"/> override.
    /// </summary>
    /// <param name="source">The source <see cref="Uri"/>.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="Source"/> documentation for more details on its intended usage.</remarks>
    public EventData WithSource(Uri source) => this.Adjust(x => x.Source = source.ThrowIfNull());

    /// <summary>
    /// Sets the <paramref name="value"/> as serialized JSON (<see cref="MediaTypeNames.Application.Json"/>) to the <see cref="Data"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="excludePaths">The list of JSON paths to exclude from the serialized JSON.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Automatically sets the following:
    /// <list type="bullet">
    /// <item><see cref="Data"/> as JSON-serialized <paramref name="value"/> (excluding any <paramref name="excludePaths"/>).</item>
    /// <item><see cref="DataSchemaVersion"/> as <see cref="SchemaAttribute.Version"/> (where specified); otherwise, <see cref="Schema.DefaultVersion"/>.</item>
    /// <item><see cref="DataSchema"/> as <see cref="SchemaAttribute.SchemaUri"/> (where specified).</item>
    /// <item><see cref="Entity"/> as <see cref="SchemaAttribute.Name"/> (where specified); otherwise, <paramref name="value"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>.</item>
    /// <item><see cref="Key"/> as <paramref name="value"/> <see cref="IEntityKey.EntityKey"/> (where not previously set).</item>
    /// <item><see cref="TenantId"/> as <paramref name="value"/> <see cref="IReadOnlyTenantId.TenantId"/> (where not previously set).</item>
    /// <item><see cref="PartitionKey"/> as <paramref name="value"/> <see cref="IReadOnlyPartitionKey.PartitionKey"/> (where not previously set).</item>
    /// </list>
    /// </remarks>
    public EventData WithValue<T>(T? value, IEnumerable<string>? excludePaths, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        // Where entity and related has not yet been specified see if it can be inferred from the value type.
        Schema.TryGetMetadata<T>(out var metadata);
        Entity ??= metadata.Name;
        DataSchema ??= metadata.SchemaUri is null ? null : new Uri(metadata.SchemaUri, UriKind.RelativeOrAbsolute);
        DataSchemaVersion ??= metadata.Version;

        // Infer from value where possible.
        if (Key is null && value is IEntityKey key)
            Key = key.EntityKey.ToString();
        
        if (TenantId is null && value is IReadOnlyTenantId tenantId)
            TenantId = tenantId.TenantId;

        if (PartitionKey is null && value is IReadOnlyPartitionKey partitionKey)
            PartitionKey = partitionKey.PartitionKey;

        // Exit quickly where the value is null.
        if (value is null)
        {
            Data = null;
            return this;
        }

        // Serialize the value data.
        jsonSerializerOptions ??= JsonDefaults.SerializerOptions;
        if (!excludePaths?.Any() ?? false)
            Data = BinaryData.FromObjectAsJson(value, jsonSerializerOptions);
        else
        {
            JsonFilter.TryFilter(value, excludePaths, out JsonNode node, JsonFilterOption.Exclude, jsonSerializerOptions);
            using var ms = new MemoryStream();
            var jw = new Utf8JsonWriter(ms);
            node.WriteTo(jw, jsonSerializerOptions);
            jw.Flush();
            ms.Position = 0;
            Data = BinaryData.FromStream(ms, MediaTypeNames.Application.Json);
        }

        return this;
    }

    /// <summary>
    /// Sets the <paramref name="value"/> as serialized JSON (<see cref="MediaTypeNames.Application.Json"/>) to the <see cref="Data"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="excludePaths">The list of JSON paths to exclude from the serialized JSON.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Automatically sets the following:
    /// <list type="bullet">
    /// <item><see cref="Data"/> as JSON-serialized <paramref name="value"/> (excluding any <paramref name="excludePaths"/>).</item>
    /// <item><see cref="DataSchemaVersion"/> as <see cref="SchemaAttribute.Version"/> (where specified); otherwise, <see cref="Schema.DefaultVersion"/>.</item>
    /// <item><see cref="DataSchema"/> as <see cref="SchemaAttribute.SchemaUri"/> (where specified).</item>
    /// <item><see cref="Entity"/> as <see cref="SchemaAttribute.Name"/> (where specified); otherwise, <paramref name="value"/> <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>.</item>
    /// <item><see cref="TenantId"/> as <paramref name="value"/> <see cref="IReadOnlyTenantId.TenantId"/> (where not previously set).</item>
    /// <item><see cref="PartitionKey"/> as <paramref name="value"/> <see cref="IReadOnlyPartitionKey.PartitionKey"/> (where not previously set).</item>
    /// </list>
    /// </remarks>
    public EventData WithValue<T>(T? value = default, params IEnumerable<string> excludePaths) => WithValue(value, excludePaths, null);
}