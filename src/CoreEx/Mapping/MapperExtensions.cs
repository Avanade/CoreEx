namespace CoreEx.Mapping;

/// <summary>
/// Provides extension methods for <see cref="Mapper"/> capabilities.
/// </summary>
public static class MapperExtensions
{
    /// <summary>
    /// Maps the <paramref name="source"/> value to a new <typeparamref name="TDestination"/> value.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="IIntoMapper{TSource, TDestination}"/>.</param>
    /// <param name="source">The source value to map from.</param>
    /// <returns>The <typeparamref name="TDestination"/> value that was created and mapped into.</returns>
    public static TDestination MapNew<TSource, TDestination>(this IIntoMapper<TSource, TDestination> mapper, TSource source)
        where TSource : class
        where TDestination : class, new()
    {
        var destination = new TDestination();
        mapper.MapInto(source, destination);
        return destination;
    }

    /// <summary>
    /// Maps the <paramref name="source"/> value to a new <typeparamref name="TDestination"/> value or returns <see langword="null"/> if the <paramref name="source"/> value is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <param name="mapper">The <see cref="IIntoMapper{TSource, TDestination}"/>.</param>
    /// <param name="source">The source value to map from.</param>
    /// <returns>The <typeparamref name="TDestination"/> value that was created and mapped into, or <see langword="null"/> where the source was also <see langword="null"/>.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public static TDestination? MapNewOrNull<TSource, TDestination>(this IIntoMapper<TSource, TDestination> mapper, TSource? source)
        where TSource : class
        where TDestination : class, new()
    {
        if (source is null)
            return null;

        var destination = new TDestination();
        mapper.MapInto(source, destination);
        return destination;
    }
}