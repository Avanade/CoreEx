namespace CoreEx.Json;

/// <summary>
/// The <see cref="JsonMergePatch"/> result.
/// </summary>
public class JsonMergePatchResult<T>
{
    /// <summary>
    /// Indicates whether changes were identified whilst merging into the <see cref="Merged"/> value.
    /// </summary>
    public bool HasChanges { get; internal set; }

    /// <summary>
    /// Gets the resulting <see cref="Merged"/> value.
    /// </summary>
    public T? Merged { get; internal set; }
}