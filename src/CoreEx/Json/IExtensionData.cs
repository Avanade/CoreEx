namespace CoreEx.Json;

/// <summary>
/// Enables an entity/model to carry (round-trip) any JSON properties it does not explicitly declare, typically via <see cref="JsonExtensionDataAttribute"/>.
/// </summary>
/// <remarks>Intended for replace-style persistence (e.g. a document store replace overwrites the whole document) so that data is never lost simply because the model is unaware of it. Values
/// deserialized into the bag are typically <see cref="JsonElement"/>.</remarks>
public interface IExtensionData
{
    /// <summary>
    /// Gets or sets the extension data (the properties not explicitly declared by the implementing type).
    /// </summary>
    Dictionary<string, object?>? ExtensionData { get; set; }
}
