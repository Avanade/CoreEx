namespace CoreEx.Metadata;

/// <summary>
/// Provides the runtime metadata definition for a property within an entity.
/// </summary>
/// <typeparam name="TEntity">The owning entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="name">The property name.</param>
/// <param name="getValue">The function to get the value.</param>
/// <param name="setValue">The action to set the value.</param>
/// <param name="text">The optional <see cref="LText"/>.</param>
/// <param name="defaultValue">The optional default value.</param>
/// <param name="clean">The <see cref="Entities.CleanOption"/> used for <see cref="Clean(TEntity)">cleaning</see>.</param>
/// <param name="jsonName">The optional explicit JSON name.</param>
/// <param name="format">The optional format string.</param>
/// <remarks>The underlying implementation does not store mutable state for an entity property; therefore, an instance can be cached and reused where applicable to improve performance, etc.</remarks>
public readonly struct PropertyRuntimeMetadata<TEntity, TProperty>(string name, Func<TEntity, TProperty> getValue, Action<TEntity, TProperty>? setValue = null, Func<LText>? text = null, TProperty defaultValue = default!, CleanOption clean = CleanOption.UseDefault, string? jsonName = null, string? format = null) : IPropertyRuntimeMetadata
{
    private static readonly string? _nullObject = null;

    private readonly Func<TEntity, TProperty> _getValue = getValue.ThrowIfNull();
    private readonly Action<TEntity, TProperty>? _setValue = setValue;
    private readonly Lazy<LText> _text = new(() => text?.Invoke() ?? new LText(name, name.ToSentenceCase()));

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyRuntimeMetadata{TEntity, T}"/> struct.
    /// </summary>
    /// <remarks>The parameterless constructor is not supported; and as such, throws a <see cref="NotSupportedException"/>.</remarks>
    [Obsolete("Parameterless constructor is not supported.", true)]
    public PropertyRuntimeMetadata() : this(_nullObject ?? throw new NotSupportedException("The parameterless constructor is not supported."), null!) { }

    /// <inheritdoc/>
    public Type Owner => typeof(TEntity);

    /// <inheritdoc/>
    public Type Type => typeof(TProperty);

    /// <inheritdoc/>
    public string Name { get; } = name.ThrowIfNullOrEmpty();

    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public LText Text => _text.Value;

    /// <inheritdoc/>
    public string? JsonName { get; } = jsonName;

    /// <inheritdoc/>
    object? IPropertyRuntimeMetadata.DefaultValue => DefaultValue;

    /// <summary>
    /// Gets the default value.
    /// </summary>
    public TProperty? DefaultValue { get; } = defaultValue;

    /// <inheritdoc/>
    public CleanOption CleanOption { get; } = clean;

    /// <inheritdoc/>
    public bool IsReadOnly => _setValue is null;

    /// <inheritdoc/>
    public string? Format { get; } = format;

    /// <inheritdoc/>
    bool IPropertyRuntimeMetadata.IsDefault(object entity) => IsDefault((TEntity)entity.ThrowIfNull());

    /// <summary>
    /// Indicates whether the property value is considered in its default state.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    public bool IsDefault(TEntity entity) => RuntimeMetadata.IsDefault(GetValue(entity), DefaultValue);

    /// <inheritdoc/>
    void IPropertyRuntimeMetadata.Clean(object entity)
    {
        if (entity is not null)
            Clean((TEntity)entity);
    }

    /// <summary>
    /// Cleans the property.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    public void Clean(TEntity entity)
    {
        if (entity is null)
            return;

        var clean = CleanOption == CleanOption.UseDefault ? Cleaner.DefaultCleanOption : CleanOption;
        if (clean == CleanOption.None)
            return;

        var val = Cleaner.Clean(GetValue(entity));
        if (IsReadOnly)
            return;

        if (clean == CleanOption.CleanAndDefault && RuntimeMetadata.AreEqual(val, DefaultValue))
            SetValue(entity, DefaultValue!);
    }

    /// <inheritdoc/>
    object? IPropertyRuntimeMetadata.GetValue(object entity) => GetValue((TEntity)entity);

    /// <inheritdoc/>
    T IPropertyRuntimeMetadata.GetValue<T>(object entity) => Internal.Cast<TProperty, T>(GetValue((TEntity)entity));

    /// <summary>
    /// Gets the property value.
    /// </summary>
    public TProperty GetValue(TEntity entity) => _getValue(entity.ThrowIfNull());

    /// <inheritdoc/>
    void IPropertyRuntimeMetadata.SetValue(object entity, object? value) => SetValue((TEntity)entity.ThrowIfNull(), (TProperty)value!);

    /// <inheritdoc/>
    void IPropertyRuntimeMetadata.SetValue<T>(object entity, T value) => SetValue((TEntity)entity, Internal.Cast<T, TProperty>(value));

    /// <summary>
    /// Sets (overrides) the property value.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="value">The property value.</param>
    public void SetValue(TEntity entity, TProperty value)
    {
        if (_setValue is null)
            throw new InvalidOperationException($"Property '{Name}' is read-only and cannot be set.");

        _setValue(entity.ThrowIfNull(), value);
    }

    /// <inheritdoc/>
    public string GetJsonName(JsonSerializerOptions? options = null) => JsonName is not null 
        ? JsonName 
        : (options ?? JsonDefaults.SerializerOptions).PropertyNamingPolicy?.ConvertName(Name) ?? Name;
}