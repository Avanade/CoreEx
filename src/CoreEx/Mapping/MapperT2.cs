namespace CoreEx.Mapping;

/// <summary>
/// Provides mapping (<see cref="IMapper"/>) from a <typeparamref name="TSource"/> value to a new <typeparamref name="TDestination"/> value.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
/// <remarks>Automatically leverages the <see cref="Mapper.MapStandardFrom{TSource, TDestination}(TDestination, TSource, bool)"/> (where <see cref="UseMapStandardFrom"/>) to map standard properties where applicable.</remarks>
public abstract class Mapper<TSource, TDestination> : IMapper<TSource, TDestination> where TSource : class where TDestination : class
{
    /// <summary>
    /// Indicates whether to use the <see cref="Mapper.MapStandardFrom"/> automatically.
    /// </summary>
    /// <remarks>This occurs after the <see cref="OnMap(TSource)"/>.</remarks>
    protected virtual bool UseMapStandardFrom => true;

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(source))]
    public TDestination? Map(TSource? source) => source is null ? null : OnMap(source).AdjustWhen(_ => UseMapStandardFrom, d => Mapper.MapStandardFrom(d, source));

    /// <summary>
    /// Maps the <paramref name="source"/> value to a new destination value.
    /// </summary>
    /// <param name="source">The source value .</param>
    /// <returns>The destination value.</returns>
    /// <remarks>The source will never be <see langword="null"/>; i.e. this is only invoked where not <see langword="null"/>.</remarks>
    protected abstract TDestination OnMap(TSource source);
}