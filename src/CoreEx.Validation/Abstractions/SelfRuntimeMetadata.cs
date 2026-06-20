namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Provides the runtime metadata definition for an entity where the property is acting as itself.
/// </summary>
internal readonly struct SelfRuntimeMetadata<TSelf>() : IPropertyRuntimeMetadata, ISelfRuntimeMetadata
{
    private const string _name = "$self";
    private readonly Lazy<LText> _text = new(() => new LText($"{Internal.GetNamespaceFormattedName(typeof(TSelf))}:{_name}", Validation.ValueName.ToSentenceCase()));

    /// <inheritdoc/>
    public Type Owner => typeof(TSelf);

    /// <inheritdoc/>
    public Type Type => typeof(TSelf);

    /// <inheritdoc/>
    public string Name => _name;

    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public LText Text => _text.Value;

    /// <inheritdoc/>
    public string? JsonName => null;

    /// <inheritdoc/>
    public object? DefaultValue => null;

    /// <inheritdoc/>
    public CleanOption CleanOption => CleanOption.None;

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public string? Format => null;

    /// <inheritdoc/>
    public void Clean(object entity) { }

    /// <inheritdoc/>
    public string GetJsonName(JsonSerializerOptions? options = null) => string.Empty;

    /// <inheritdoc/>
    public object? GetValue(object entity) => entity;

    /// <inheritdoc/>
    public T GetValue<T>(object entity) => (T)entity;

    /// <inheritdoc/>
    public bool IsDefault(object entity) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void SetValue(object entity, object? value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void SetValue<T>(object entity, T value) => throw new NotSupportedException();
}