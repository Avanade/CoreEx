namespace CoreEx.Mapping;

/// <summary>
/// Enables mapping from a source value to a new destination value.
/// </summary>
public interface IMapper : IMapperBase
{
    /// <summary>
    /// Maps the <paramref name="source"/> value to a new destination value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <returns>The destination value.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    object? Map(object? source);
}