namespace CoreEx.Mapping;

/// <summary>
/// Provides mapping (<see cref="IIntoMapper"/>) from a <typeparamref name="TSource"/> into an existing <typeparamref name="TDestination"/> value.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
/// <remarks>Automatically leverages the <see cref="Mapper.MapStandardInto{TSource, TDestination}(TSource, TDestination, bool)"/> (where <see cref="UseMapStandardInto"/>) to map standard properties where applicable.</remarks>
public abstract class IntoMapper<TSource, TDestination> : IIntoMapper<TSource, TDestination> where TSource : class where TDestination : class
{
    /// <summary>
    /// Indicates whether to use the <see cref="Mapper.MapStandardInto"/> automatically.
    /// </summary>
    /// <remarks>This occurs after the <see cref="OnMapInto(TSource, TDestination)"/>.</remarks>
    protected virtual bool UseMapStandardInto => true;

    /// <inheritdoc/>
    public void MapInto(TSource source, TDestination destination)
    {
        OnMapInto(source.ThrowIfNull(), destination.ThrowIfNull());
        if (UseMapStandardInto)
            Mapper.MapStandardInto(source, destination);
    }

    /// <summary>
    /// Maps the <paramref name="source"/> value into an existing destination value.
    /// </summary>
    /// <param name="source">The source value .</param>
    /// <param name="destination">The destination value.</param>
    protected abstract void OnMapInto(TSource source, TDestination destination);
}