namespace CoreEx.Mapping.Converters;

/// <summary>
/// Enables bi-directional conversion from a source to a destination value and vice-versa.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
public interface IConverter<TSource, TDestination> : ISourceConverter<TSource>, IDestinationConverter<TDestination>
{
    /// <inheritdoc/>
    object? ISourceConverter<TSource>.ConvertToDestination(TSource source) => ConvertToDestination((TSource)source!);

    /// <inheritdoc/>
    TSource ISourceConverter<TSource>.ConvertToSource(object? destination) => ConvertToSource((TDestination)destination!);

    /// <inheritdoc/>
    TDestination IDestinationConverter<TDestination>.ConvertToDestination(object? source) => ConvertToDestination((TSource)source!);

    /// <inheritdoc/>
    object? IDestinationConverter<TDestination>.ConvertToSource(TDestination destination) => ConvertToSource((TDestination)destination!);

    /// <summary>
    /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
    /// </summary>
    IValueConverter<TSource, TDestination> ToDestination { get; }

    /// <summary>
    /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
    /// </summary>
    IValueConverter<TDestination, TSource> ToSource { get; }

    /// <summary>
    /// Converts the source to the destination value (converts to).
    /// </summary>
    /// <param name="source">The source value to convert.</param>
    /// <returns>The converted destination value.</returns>
    new TDestination ConvertToDestination(TSource source);

    /// <summary>
    /// Converts the destination to the source value (converts back from).
    /// </summary>
    /// <param name="destination">The destination value to convert.</param>
    /// <returns>The converted source value.</returns>
    new TSource ConvertToSource(TDestination destination);
}