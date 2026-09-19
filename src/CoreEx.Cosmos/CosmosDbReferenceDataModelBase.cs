namespace CoreEx.Cosmos;

/// <summary>
/// Provides a convenience base class for reference data models implementing the common <c>IReferenceData</c> properties (extends <see cref="CosmosDbModelBase"/>).
/// </summary>
/// <remarks>Usage is purely optional; there is no other specific requirement for its use.
/// <para>Does not implement <c>IReferenceData</c> by design, as it is not intended to support the base functionality.</para></remarks>
public class CosmosDbReferenceDataModelBase : CosmosDbModelBase
{
    /// <summary>
    /// Gets or sets the unique code.
    /// </summary>
    [JsonPropertyOrder(-899)]
    public string Code { get; set; } = default!;

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    [JsonPropertyOrder(-898)]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [JsonPropertyOrder(-897)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    [JsonPropertyOrder(-896)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int SortOrder { get; set; }

    /// <summary>
    /// Indicates whether the reference data is active.
    /// </summary>
    [JsonPropertyOrder(-895)]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the validity start <see cref="DateTimeOffset"/>.
    /// </summary>
    [JsonPropertyOrder(-894)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTimeOffset? StartsOn { get; init; }

    /// <summary>
    /// Gets or sets the validity end <see cref="DateTimeOffset"/>.
    /// </summary>
    [JsonPropertyOrder(-893)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTimeOffset? EndsOn { get; init; }
}
