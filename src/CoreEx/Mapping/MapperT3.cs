namespace CoreEx.Mapping;

/// <summary>
/// Provides mapping (<see cref="IMapper"/>) from a <typeparamref name="TSource"/> value to a new <typeparamref name="TDestination"/> value.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The mapper <see cref="Type"/> itself.</typeparam>
/// <remarks>The <see cref="Map(TSource?)"/> method provides a convenient way to map a source value to a new destination value using the singleton instance of the underlying mapper.
/// <para>Automatically leverages the <see cref="Mapper.MapStandardFrom{TSource, TDestination}(TDestination, TSource, bool)"/> (where <see cref="Mapper{TSource, TDestination}.UseMapStandardFrom"/>) to map standard properties where applicable.</para></remarks>
public abstract class Mapper<TSource, TDestination, TSelf> : Mapper<TSource, TDestination> where TSource : class where TDestination : class where TSelf : Mapper<TSource, TDestination, TSelf>, new()
{
    /// <summary>
    /// Gets the default singleton instance of the mapper.
    /// </summary>
    public static TSelf Default { get; } = new TSelf();

    /// <summary>
    /// Maps the <paramref name="source"/> value to a new destination value.
    /// </summary>
    /// <param name="source">The source value .</param>
    /// <returns>The destination value.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public static new TDestination? Map(TSource? source) => source is null ? null : Default.OnMap(source).AdjustWhen(_ => Default.UseMapStandardFrom, d => Mapper.MapStandardFrom(d, source));
}