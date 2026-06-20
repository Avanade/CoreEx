namespace CoreEx.Entities;

/// <summary>
/// Enables base entity key support using a <see cref="CompositeKey"/>.
/// </summary>
/// <remarks>To enable key-based support in a consistent and standardized manner then this interface must be implemented; for example, see <see cref="IIdentifierCore"/>.</remarks>
public interface IEntityKey
{
    /// <summary>
    /// Gets the key for the entity as a <see cref="CompositeKey"/>.
    /// </summary>
    /// <returns>The key represented as a <see cref="CompositeKey"/>.</returns>
    [JsonIgnore]
    CompositeKey EntityKey { get; }
}