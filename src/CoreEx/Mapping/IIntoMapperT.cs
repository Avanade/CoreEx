namespace CoreEx.Mapping;

/// <summary>
/// Enables mapping from a <typeparamref name="TSource"/> value into an existing <typeparamref name="TDestination"/> value.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
public interface IIntoMapper<TSource, TDestination> : IIntoMapper where TSource : class where TDestination : class
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
    void IIntoMapper.MapInto(object source, object destination) => MapInto((TSource)source.ThrowIfNull(), (TDestination)destination.ThrowIfNull());

    /// <summary>
    /// Maps (merges) the <paramref name="source"/> value into an existing <paramref name="destination"/> value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    void MapInto(TSource source, TDestination destination);
}