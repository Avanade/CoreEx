namespace CoreEx.Mapping;

/// <summary>
/// Enables mapping from a <typeparamref name="TSource"/> value to a new <typeparamref name="TDestination"/> value.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
public interface IMapper<TSource, TDestination> : IMapper where TSource : class  where TDestination : class
{
    /// <summary>
    /// Gets the source <see cref="Type"/>.
    /// </summary>
    Type IMapperBase.SourceType => typeof(TSource);

    /// <summary>
    /// Gets the destination <see cref="Type"/>.
    /// </summary>
    Type IMapperBase.DestinationType => typeof(TDestination);

    /// <inheritdoc/>
    object? IMapper.Map(object? source) => Map((TSource?)source)!;

    /// <summary>
    /// Maps the <paramref name="source"/> value to a new destination value.
    /// </summary>
    /// <param name="source">The source value .</param>
    /// <returns>The destination value.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    TDestination? Map(TSource? source);
}