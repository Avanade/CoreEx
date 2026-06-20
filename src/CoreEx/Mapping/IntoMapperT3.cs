namespace CoreEx.Mapping;

/// <summary>
/// Provides mapping (<see cref="IIntoMapper"/>) from a <typeparamref name="TSource"/> value into an existing <typeparamref name="TDestination"/> value.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The mapper <see cref="Type"/> itself.</typeparam>
/// <remarks>The <see cref="MapInto(TSource, TDestination)"/> method provides a convenient way to map a source value into an existing destination value using the singleton instance of the underlying mapper.
/// <para>Automatically leverages the <see cref="Mapper.MapStandardInto{TSource, TDestination}(TSource, TDestination, bool)"/> (where <see cref="IntoMapper{TSource, TDestination}.UseMapStandardInto"/>) to map standard properties where applicable.</para></remarks>
public abstract class IntoMapper<TSource, TDestination, TSelf> : IntoMapper<TSource, TDestination> where TSource : class where TDestination : class where TSelf : IntoMapper<TSource, TDestination, TSelf>, new()
{
    /// <summary>
    /// Gets the default singleton instance of the mapper.
    /// </summary>
    public static TSelf Default { get; } = new TSelf();

    /// <summary>
    /// Maps (merges) the <paramref name="source"/> value into an existing <paramref name="destination"/> value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    [return: NotNullIfNotNull(nameof(source))]
    public static new void MapInto(TSource source, TDestination destination)
    {
        Default.OnMapInto(source.ThrowIfNull(), destination.ThrowIfNull());
        if (Default.UseMapStandardInto)
            Mapper.MapStandardInto(source, destination);
    }
}