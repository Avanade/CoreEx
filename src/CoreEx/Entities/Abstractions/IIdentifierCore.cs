namespace CoreEx.Entities.Abstractions;

/// <summary>
/// Enables the core read-only <see cref="Id"/> and related <see cref="IEntityKey"/> capabilities.
/// </summary>
public interface IIdentifierCore : IEntityKey
{
    /// <summary>
    /// Gets the identifier.
    /// </summary>
    object? Id { get; }

    /// <summary>
    /// Gets the <see cref="Id"/> <see cref="Type"/>.
    /// </summary>
    [JsonIgnore]
    Type IdType { get; }

    /// <summary>
    /// Indicates whether the <see cref="Id"/> is read-only.
    /// </summary>
    [JsonIgnore]
    bool IsIdReadOnly { get; }

    /// <summary>
    /// Sets (overrides) the identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <remarks>Must not be <see cref="IsIdReadOnly"/>.</remarks>
    void SetIdentifier(object? id);
}