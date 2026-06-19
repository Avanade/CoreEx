namespace CoreEx.RefData;

/// <summary>
/// Provides the contextual validation <see cref="Date"/> for a <see cref="IReferenceData.StartsOn"/> and <see cref="IReferenceData.EndsOn"/> <see cref="IReferenceData.IsValid"/> verification.
/// </summary>
public class ReferenceDataContext : IReferenceDataContext
{
    private DateTimeOffset? _date;
    private readonly ConcurrentDictionary<Type, DateTimeOffset?> _coll = new();

    /// <summary>
    /// Gets or sets the <see cref="IReferenceData"/> <see cref="IReferenceData.StartsOn"/> and <see cref="IReferenceData.EndsOn"/> contextual validation date.
    /// </summary>
    /// <remarks>Defaults to <see cref="Runtime.UtcNow"/>.</remarks>
    public DateTimeOffset? Date
    {
        get => _date ??= Runtime.UtcNow;
        set => _date = value;
    }

    /// <summary>
    /// Gets or sets a contextual validation date for a specific <see cref="IReferenceData"/> <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <returns>The contextual validation date.</returns>
    public DateTimeOffset? this[Type type]
    {
        get => (_coll.TryGetValue(type.ThrowIfNull(), out var date) ? date : Date) ?? Date;
        set => _coll.AddOrUpdate(type, _ => value, (_, _) => value);
    }

    /// <summary>
    /// Resets all dates.
    /// </summary>
    public void Reset()
    {
        _date = null;
        _coll.Clear();
    }
}
