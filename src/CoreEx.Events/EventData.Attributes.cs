namespace CoreEx.Events;

public partial class EventData
{
    /// <summary>
    /// Gets the additional attributes.
    /// </summary>
    /// <remarks>Any attribute key with an underscore ('<c>_</c>') prefix denotes that it is <i>not</i> intended to be published unless explicitly implemented.</remarks>
    [JsonIgnore]
    public ConcurrentDictionary<string, object?> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds or updates the attribute using the specified <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="key">The attribute key.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method chaining.</returns>
    public EventData SetAttribute<T>(string key, T value)
    {
        Attributes.AddOrUpdate(key.ThrowIfNullOrEmpty(), _ => value, (_, __) => value);
        return this;
    }

    /// <summary>
    /// Tries to get the attribute using the specified <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="key">The attribute key.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> indicates found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetAttribute<T>(string key, out T? value)
    {
        if (Attributes.TryGetValue(key, out var av))
        {
            value = (T?)av;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets or sets the tracked operation identifier.
    /// </summary>
    /// <remarks>Stored as the <c>operationid</c> message attribute.</remarks>
    public string? OperationId
    {
        get => TryGetAttribute("operationid", out string? value) ? value : null;
        set => SetOptionalAttribute("operationid", value);
    }

    /// <summary>
    /// Gets or sets the tracked operation name.
    /// </summary>
    /// <remarks>Stored as the <c>operationname</c> message attribute.</remarks>
    public string? OperationName
    {
        get => TryGetAttribute("operationname", out string? value) ? value : null;
        set => SetOptionalAttribute("operationname", value);
    }

    /// <summary>
    /// Gets or sets the tracked operation's first-start deadline.
    /// </summary>
    /// <remarks>Stored in round-trip format as the <c>operationstartby</c> message attribute.</remarks>
    public DateTimeOffset? OperationStartBy
    {
        get
        {
            if (!TryGetAttribute("operationstartby", out string? value) || value is null)
                return null;

            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }
        set => SetOptionalAttribute("operationstartby", value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Sets or removes the optional operation message attribute.
    /// </summary>
    private void SetOptionalAttribute(string key, string? value)
    {
        if (value is null)
            Attributes.TryRemove(key, out _);
        else
            SetAttribute(key, value);
    }
}
