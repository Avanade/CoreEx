namespace CoreEx.Validation;

/// <summary>
/// Provides a validation context for a property.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
public struct PropertyContext<TEntity, TProperty> : IPropertyContext<TEntity, TProperty> where TEntity : class
{
    private const string _dictionaryKeyParameterName = "__dictionaryKey";
    private const string _collectionIndexParameterName = "__collectionIndex";

    private readonly IRootPropertyRule<TEntity> _root;
    private readonly IPropertyRuntimeMetadata _metadata;
    private readonly ValidationContext<TEntity> _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyContext{TEntity, TProperty}"/> class.
    /// </summary>
    /// <param name="root">The <see cref="RootPropertyRule{TEntity, TProperty}"/>.</param>
    /// <param name="context">The validation context for the parent entity.</param>
    internal PropertyContext(RootPropertyRule<TEntity, TProperty> root, ValidationContext<TEntity> context)
    {
        _root = root.ThrowIfNull();
        _metadata = root.Metadata.ThrowIfNull();
        _owner = context.ThrowIfNull();

        JsonName = _metadata.GetJsonName(context.JsonSerializerOptions);
        Value = _metadata.GetValue<TProperty>(context.Value);
        IsValueNull = Value is null;
        ValueFormatter = root.ValueFormatter;
        Text = root.Text ?? _metadata.Text;

        if (_metadata is ISelfRuntimeMetadata)
        {
            // Self-referencing entity; so, use the owner qualified names where not null; otherwise, default.
            FullyQualifiedPropertyName = _owner.FullyQualifiedEntityName ?? Name;
            FullyQualifiedJsonPropertyName = _owner.FullyQualifiedJsonEntityName ?? JsonName;
        }
        else
        {
            // Standard property; so, append to the owner qualified names.
            FullyQualifiedPropertyName = CreateFullyQualifiedPropertyName(Name);
            FullyQualifiedJsonPropertyName = CreateFullyQualifiedJsonPropertyName(JsonName);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyContext{TEntity, TProperty}"/> class.
    /// </summary>
    /// <param name="context">The <see cref="IPropertyContext{TEntity}"/>.</param>
    /// <param name="value">The property value.</param>
    internal PropertyContext(IPropertyContext<TEntity> context, TProperty value)
    {
        _root = context.RootPropertyRule;
        _metadata = context.Metadata;
        _owner = (ValidationContext<TEntity>)context.Owner;

        JsonName = context.JsonName;
        Value = value;
        IsValueNull = context.IsValueNull;
        ValueFormatter = context.ValueFormatter;
        Text = context.Text;
        FullyQualifiedPropertyName = context.FullyQualifiedPropertyName;
        FullyQualifiedJsonPropertyName = context.FullyQualifiedJsonPropertyName;
    }

    /// <inheritdoc/>
    readonly IRootPropertyRule<TEntity> IPropertyContext<TEntity>.RootPropertyRule => _root;

    /// <inheritdoc/>
    readonly IValidationContext<TEntity> IPropertyContext<TEntity>.Owner => _owner;

    /// <inheritdoc/>
    readonly IPropertyRuntimeMetadata IPropertyContext.Metadata => _metadata;

    /// <summary>
    /// Gets the property <see cref="IPropertyRuntimeMetadata"/>.
    /// </summary>
    internal readonly IPropertyRuntimeMetadata Metadata => _metadata;

    /// <inheritdoc/>
    public readonly IDictionary<string, object?> Parameters => _owner.Parameters;

    /// <inheritdoc/>
    public readonly string Name => _metadata.Name;

    /// <inheritdoc/>
    public string JsonName { get; }

    /// <inheritdoc/>
    public string FullyQualifiedPropertyName { get; }

    /// <inheritdoc/>
    public string FullyQualifiedJsonPropertyName { get; }

    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public LText Text { get; }

    /// <inheritdoc/>
    public readonly TEntity Entity => _owner.Value;

    /// <inheritdoc/>
    public TProperty Value { get; private set; }

    /// <inheritdoc/>
    public bool IsValueNull { get; private set; }

    /// <inheritdoc/>
    public ValueFormatter ValueFormatter { get; }

    /// <inheritdoc/>
    public readonly bool IsInError => HasError(FullyQualifiedPropertyName);

    /// <summary>
    /// Determines whether the specified fully qualified property name has an error.
    /// </summary>
    /// <param name="fullyQualifiedPropertyName">The fully qualified property name.</param>
    /// <returns><see langword="true"/> where an error exists for the specified property; otherwise, <see langword="false"/>.</returns>
    public readonly bool HasError(string fullyQualifiedPropertyName) => _owner.HasError(fullyQualifiedPropertyName);

    /// <summary>
    /// Adds an <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified <paramref name="format"/> and additional <paramref name="values"/> to be included in the text.
    /// </summary>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>The <see cref="MessageItem"/>.</returns>
    /// <remarks>The property friendly text and value are automatically prepended to the <paramref name="values"/> as the first two arguments where the <paramref name="format"/> does not have already have <see cref="LText.HasArgs"/>.</remarks>
    public readonly MessageItem AddError(LText format, params object?[] values) => _owner.AddError<TProperty>(_metadata, Text, ValueFormatter, JsonName, format, values);

    /// <inheritdoc/>
    public readonly string CreateFullyQualifiedPropertyName(string name) => _owner.CreateFullyQualifiedPropertyName(name);

    /// <inheritdoc/>
    public readonly string CreateFullyQualifiedJsonPropertyName(string jsonName) => _owner.CreateFullyQualifiedJsonPropertyName(jsonName);

    /// <summary>
    /// Creates a new <see cref="ValidationArgs"/> from the <see cref="PropertyContext{TEntity, TProperty}"/>.
    /// </summary>
    /// <param name="useParentQualifiedNames">Indicates whether to alternatively use the qualified names from the parent <see cref="ValidationContext{TEntity}"/>.</param>
    /// <returns>The <see cref="ValidationArgs"/>.</returns>
    public readonly ValidationArgs CreateValidationArgs(bool useParentQualifiedNames = false) => new()
    {
        FullyQualifiedEntityName = useParentQualifiedNames ? _owner.FullyQualifiedEntityName : FullyQualifiedPropertyName,
        FullyQualifiedJsonEntityName = useParentQualifiedNames ? _owner.FullyQualifiedJsonEntityName : FullyQualifiedJsonPropertyName,
        UseJsonNames = _owner.UseJsonNames,
        JsonSerializerOptions = _owner.JsonSerializerOptions,
        ServiceProvider = _owner.ServiceProvider,
        Parameters = _owner.Parameters
    };

    /// <summary>
    /// Merges a validation result into this.
    /// </summary>
    /// <param name="validationResult">The <see cref="IValidationResult"/> to merge.</param>
    internal readonly void MergeResult(IValidationResult validationResult) => _owner.MergeResult(validationResult);

    /// <summary>
    /// Formats the property value as a <see cref="LText"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="LText"/> representation.</returns>
    /// <remarks>Leverages the <see cref="IRootPropertyRule{TEntity}.SetFormat(string?, IFormatProvider?, char?)"/> configuration.</remarks>
    public readonly LText FormatValue(TProperty? value) => ValueFormatter.ToLText(value);

    /// <summary>
    /// Indicates whether the originating property type is <see cref="Nullable{T}"/>.
    /// </summary>
    public readonly bool IsValueNullable => _root.IsValueNullable;

    /// <summary>
    /// Gets the originating property <see cref="Nullable{T}.Value"/> or <see langword="default"/> (where <see cref="IsValueNullable"/>).
    /// </summary>
    /// <typeparam name="T">The property <see cref="Nullable{T}.Value"/> <see cref="Type"/>.</typeparam>
    /// <returns>The originating property <see cref="Nullable{T}.Value"/> or <see langword="default"/>.</returns>
    public readonly T GetNullableValueOrDefault<T>() => _root.GetNullableValueOrDefault<T>(Entity);

    /// <summary>
    /// Indicates whether the originating property <see cref="Nullable{T}.Value"/> is <see langword="default"/> (where <see cref="IsValueNullable"/>).
    /// </summary>
    /// <returns><see langword="true"/> where <see langword="default"/>; otherwise, <see langword="false"/>.</returns>
    public readonly bool IsNullableValueDefault() => _root.IsNullableValueDefault(Entity);

    /// <inheritdoc/>
    /// <remarks>An <see cref="InvalidOperationException"/> will be thrown where the underlying property is read-only.</remarks>
    public void Override(TProperty value)
    {
        if (_metadata.IsReadOnly)
            throw new InvalidOperationException($"The property '{Name}' is read-only and cannot be overridden.");

        // Override the value on the actual entity.
        _metadata.SetValue(Entity, value);

        // Update the context to reflect the new value.
        Value = value;
        IsValueNull = value is null;
    }

    /// <summary>
    /// Gets the <i>last</i> dictionary key from the validation stack.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key <see cref="Type"/>.</typeparam>
    /// <returns>The dictionary key.</returns>
    /// <remarks>This is typically used where the property being validated is within a dictionary and the key is required as part of the validation. The dictionary key is added to the context parameters
    /// by the <see cref="CoreEx.Validation.Rules.DictionaryRule{TEntity, TProperty, TKey, TValue}"/>.
    /// <para>A <see cref="KeyNotFoundException"/> will be thrown where the context does not contain a dictionary key or it is not of type <typeparamref name="TKey"/>.</para></remarks>
    /// <exception cref="KeyNotFoundException"/>
    public readonly TKey GetDictionaryKey<TKey>() where TKey : notnull
    {
        if (Parameters?.TryGetValue("__dictionaryKey", out var obj) == true && obj is TKey typedKey)
            return typedKey;

        throw new KeyNotFoundException("The property context does not contain a dictionary key or was not the specified type.");
    }

    /// <summary>
    /// Gets the <i>last</i> dictionary key from the validation stack where available (without exception where not found or of incorrect type).
    /// </summary>
    /// <returns>The dictionary key where available; otherwise, <see langword="null"/>.</returns>
    internal readonly object? GetDictionaryKeySafe() => Parameters.TryGetValue(_dictionaryKeyParameterName, out var obj) ? obj : null;

    /// <summary>
    /// Sets the <i>last</i> dictionary key within the validation stack.
    /// </summary>
    /// <param name="key">The dictionary key.</param>
    internal readonly void SetDictionaryKey(object? key)
    {
        if (key is null)
            _owner.Parameters.Remove(_dictionaryKeyParameterName);
        else
            _owner.Parameters[_dictionaryKeyParameterName] = key;
    }

    /// <summary>
    /// Gets the <i>last</i> collection index from the validation stack.
    /// </summary>
    /// <returns>The collection index.</returns>
    /// <remarks>This is typically used where the property being validated is within a collection and the index is required as part of the validation. The collection index is added to the context parameters
    /// by the <see cref="CoreEx.Validation.Rules.CollectionRule{TEntity, TProperty, TItem}"/>.
    /// <para>An <see cref="IndexOutOfRangeException"/> will be thrown where the context does not contain a collection index or it is not of type <see cref="int"/>.</para></remarks>
    /// <exception cref="IndexOutOfRangeException"/>
    public readonly int GetCollectionIndex()
    {
        if (Parameters?.TryGetValue(_collectionIndexParameterName, out var obj) == true && obj is int typedIndex)
            return typedIndex;

        throw new IndexOutOfRangeException("The property context does not contain a collection index or was not the specified type.");
    }

    /// <summary>
    /// Gets the <i>last</i> collection index from the validation stack where available (without exception where not found or of incorrect type).
    /// </summary>
    /// <returns>The collection index.</returns>
    internal readonly int? GetCollectionIndexSafe() => Parameters.TryGetValue(_collectionIndexParameterName, out var obj) && obj is int typedIndex ? typedIndex : null;

    /// <summary>
    /// Sets the <i>last</i> collection index within the validation stack.
    /// </summary>
    /// <param name="index">The collection index.</param>
    internal readonly void SetCollectionIndex(int? index)
    {
        if (index is null)
            _owner.Parameters.Remove(_collectionIndexParameterName);
        else
            _owner.Parameters[_collectionIndexParameterName] = index;
    }
}