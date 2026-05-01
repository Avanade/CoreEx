namespace CoreEx.Mapping;

/// <summary>
/// Enables mapping from a source value into an existing destination value.
/// </summary>
public interface IIntoMapper : IMapperBase
{
    /// <summary>
    /// Maps (merges) the <paramref name="source"/> value into an existing <paramref name="destination"/> value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    void MapInto(object source, object destination);
}