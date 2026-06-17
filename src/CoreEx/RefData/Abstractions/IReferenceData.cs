namespace CoreEx.RefData.Abstractions;

/// <summary>
/// Enables the core <i>Reference Data</i> properties.
/// </summary>
/// <remarks>A reference data instance should be considered largely immutable.</remarks>
public interface IReferenceData : IReadOnlyIdentifier, IReadOnlyETag
{
    /// <summary>
    /// Gets or initializes the unique identifier.
    /// </summary>
    new object? Id { get; init; }

    /// <summary>
    /// Gets or initializes the unique code.
    /// </summary>
    string? Code { get; init; }

    /// <summary>
    /// Gets or initializes the text.
    /// </summary>
    /// <remarks>The get should return a localized text where possible; see also <see cref="GetText"/>.</remarks>
    string? Text { get; init; }

    /// <summary>
    /// Gets or initializes the description.
    /// </summary>
    /// <remarks>The get should return a localized description where possible; see also <see cref="GetDescription"/>.</remarks>
    string? Description { get; init; }

    /// <summary>
    /// Gets or initializes the sort order.
    /// </summary>
    int SortOrder { get; init; }

    /// <summary>
    /// Indicates whether the <see cref="IReferenceData"/> is inactive.
    /// </summary>
    /// <value><see langword="true"/> where inactive; otherwise, <see langword="false"/> is active.</value>
    bool IsInactive { get; init; }

    /// <summary>
    /// Indicates whether the <see cref="IReferenceData"/> is active (opposite of <see cref="IsInactive"/>).
    /// </summary>
    [JsonIgnore]
    bool IsActive { get; }

    /// <summary>
    /// Gets or initializes the validity start <see cref="DateTimeOffset"/>.
    /// </summary>
    DateTimeOffset? StartsOn { get; init; }

    /// <summary>
    /// Gets or initializes the validity end <see cref="DateTimeOffset"/>.
    /// </summary>
    DateTimeOffset? EndsOn { get; init; }

    /// <summary>
    /// Indicates whether the <see cref="IReferenceData"/> is known and in a valid state (it may be active or inactive depending).
    /// </summary>
    /// <remarks>This is typically a result of casting a <see cref="IReferenceData.Code"/> to an <see cref="IReferenceData"/> that is not known by the <see cref="ReferenceDataOrchestrator"/>.</remarks>
    [JsonIgnore]
    bool IsValid { get; }

    /// <summary>
    /// Overrides the standard <see cref="IsValid"/> check and flags the <see cref="IReferenceData"/> as <b>Invalid</b>.
    /// </summary>
    /// <remarks>Will result in <see cref="IsInactive"/> set to <see langword="true"/>. Once set to invalid it can not be changed; i.e. there is not a means to set back to valid.</remarks>
    void SetInvalid();

    /// <summary>
    /// Gets the original (non-localized) <see cref="Text"/> value.
    /// </summary>
    string? GetText();

    /// <summary>
    /// Gets the original (non-localized) <see cref="Description"/> value.
    /// </summary>
    string? GetDescription();

    /// <summary>
    /// Indicates whether any mapping values have been configured.
    /// </summary>
    [JsonIgnore]
    bool HasMappings { get; }

    /// <summary>
    /// Gets the underlying mapping dictionary as read-only.
    /// </summary>
    /// <remarks>The <see cref="SetMapping"/> is intended to be the means in which the mappings are mutated.</remarks>
    IReadOnlyDictionary<string, object?>? Mappings { get; }

    /// <summary>
    /// Sets the mapping <paramref name="value"/> for the specified <paramref name="name"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="value">The mapping value.</param>
    /// <remarks>A <paramref name="value"/> with the default value will not be set; assumed in this case that no mapping exists.</remarks>
    void SetMapping<T>(string name, T? value) where T : IComparable<T?>, IEquatable<T?>;

    /// <summary>
    /// Gets the mapping <paramref name="value"/> for the specified <paramref name="name"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="value">The mapping value.</param>
    /// <returns><see langword="true"/> indicates that the name exists; otherwise, <see langword="false"/>.</returns>
    bool TryGetMapping<T>(string name, [NotNullWhen(true)] out T? value) where T : IComparable<T?>, IEquatable<T?>;
}