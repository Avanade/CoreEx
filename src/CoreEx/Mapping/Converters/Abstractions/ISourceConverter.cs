namespace CoreEx.Mapping.Converters.Abstractions;

/// <summary>
/// Defines the <see cref="IConverter"/> with typed <typeparamref name="TSource"/>.
/// </summary>
/// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
public interface ISourceConverter<TSource> : IConverter
{
    /// <inheritdoc/>
    Type IConverter.SourceType => typeof(TSource);

    /// <summary>
    /// Converts the source to the destination value (converts to).
    /// </summary>
    /// <param name="source">The source value to convert.</param>
    /// <returns>The converted destination value.</returns>
    object? ConvertToDestination(TSource source);

    /// <summary>
    /// Converts the destination to the source value (converts back from).
    /// </summary>
    /// <param name="destination">The destination value to convert.</param>
    /// <returns>The converted source value.</returns>
    new TSource ConvertToSource(object? destination);
}