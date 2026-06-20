namespace CoreEx.RefData.Abstractions;

/// <summary>
/// Represents the core <see cref="IReferenceData{TId}"/> implementation.
/// </summary>
/// <typeparam name="TId">The <see cref="IIdentifier{TId}"/> <see cref="Type"/>.</typeparam>
/// <remarks>The <see cref="object.Equals(object?)"/> and <see cref="object.GetHashCode"/> overrides only use the <see cref="Id"/> and <see cref="Code"/> properties. The other properties are considered
/// superfluous from an equality perspective. The <see cref="Id"/> and <see cref="Code"/> properties should be both unique within their owning collection; the <see cref="ReferenceDataCollectionCore{TId, TRef}"/>
/// ensures this.</remarks>
[DebuggerDisplay("Id = {Id}, Code = {Code}, IsActive = {IsActive}, IsValid = {IsValid}")]
public abstract class ReferenceDataCore<TId> : IReferenceData<TId>
{
    private string? _text;
    private string? _description;
    private bool _isActive = true;
    private bool _isValid = true;
    private ConcurrentDictionary<string, object?>? _mappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataCore{TId}"/> class.
    /// </summary>
    internal ReferenceDataCore() => Id = default!;

    /// <inheritdoc/>
    object? IReferenceData.Id { get => Id; init => Id = (TId)value!; }

    /// <inheritdoc/>
    [JsonPropertyOrder(-999)]
    public TId Id { get; init; }

    /// <inheritdoc/>
    [JsonPropertyOrder(-998)]
    public string? Code { get; init; }

    /// <inheritdoc/>
    /// <remarks>The text is localized on get using an <see cref="LText"/>. The <see cref="LText.KeyAndOrText"/> is automatically set to the <see cref="Type.FullName"/> + '.' + <see cref="Code"/>; eg. '<c>Contoso.Products.Contracts.Brand.YETI</c>'.
    /// <para>Use <see cref="GetText"/> to get the original (non-localized) text.</para></remarks>
    [JsonPropertyOrder(-997)]
    public string? Text { get => new LText($"{GetType().FullName}:{Code ?? "?"}", _text).ToString(); init => _text = value; }

    /// <inheritdoc/>
    /// <remarks>The description is localized on get using an <see cref="LText"/>. The <see cref="LText.KeyAndOrText"/> is automatically set to the <see cref="Type.FullName"/> + '.' + <see cref="Code"/> + 'Description'; eg. '<c>Contoso.Products.Contracts.Brand.YETI.Description</c>'.
    /// <para>Use <see cref="GetDescription"/> to get the original (non-localized) description.</para></remarks>
    [JsonPropertyOrder(-996)]
    public string? Description { get => new LText($"{GetType().FullName}:{Code ?? "?"}.{nameof(Description)}", _description).ToString(); init => _description = value; }

    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyOrder(-995)]
    public int SortOrder { get; init; }

    /// <inheritdoc/>
    [JsonPropertyOrder(-994)]
    public virtual bool IsInactive
    {
        get
        {
            if (!_isValid || !_isActive)
                return true;

            if (StartsOn is not null || EndsOn is not null)
            {
                var ctx = ExecutionContext.GetService<IReferenceDataContext>();
                var date = ctx is null ? Runtime.UtcNow : ctx[GetType()];

                if (StartsOn is not null && date < StartsOn)
                    return true;

                if (EndsOn is not null && date > EndsOn)
                    return true;
            }

            return !_isActive;
        }

        init => _isActive = !value;
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsActive => !IsInactive;

    /// <inheritdoc/>
    [JsonPropertyOrder(-993)]
    public DateTimeOffset? StartsOn { get; init; }

    /// <inheritdoc/>
    [JsonPropertyOrder(-992)]
    public DateTimeOffset? EndsOn { get; init; }

    /// <inheritdoc/>
    public string? ETag { get; init; }

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsValid => _isValid;

    /// <inheritdoc/>
    void IReferenceData.SetInvalid() => _isValid = false;

    /// <inheritdoc/>
    public override string ToString() => Text ?? Code ?? Id?.ToString() ?? base.ToString()!;

    /// <inheritdoc/>
    public string? GetText() => _text;

    /// <inheritdoc/>
    public string? GetDescription() => _description;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool HasMappings => _mappings is not null && !_mappings.IsEmpty;

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyDictionary<string, object?>? Mappings => _mappings is null ? null : new ReadOnlyDictionary<string, object?>(_mappings);

    /// <inheritdoc/>
    public void SetMapping<T>(string name, T? value) where T : IComparable<T?>, IEquatable<T?>
        => (_mappings ??= new()).AddOrUpdate(name, _ => value, (_, _) => value);

    /// <inheritdoc/>
    public bool TryGetMapping<T>(string name, [NotNullWhen(true)] out T? value) where T : IComparable<T?>, IEquatable<T?>
    {
        value = default!;
        if (!HasMappings || !_mappings!.TryGetValue(name, out var val))
            return false;

        value = (T?)val!;
        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Id, Code);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not IReferenceData rd) return false;

        return Equals(Id, rd.Id) && Equals(Code, rd.Code);
    }
}