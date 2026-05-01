namespace CoreEx.Mapping.Converters.Abstractions;

/// <summary>
/// Defines the <see cref="IConverter"/> with typed <typeparamref name="TDestination"/>.
/// </summary>
/// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
public interface IDestinationConverter<TDestination> : IConverter
{
    /// <inheritdoc/>
    Type IConverter.DestinationType => typeof(TDestination);

    /// <summary>
    /// Converts the source to the destination value (converts to).
    /// </summary>
    /// <param name="source">The source value to convert.</param>
    /// <returns>The converted destination value.</returns>
    new TDestination ConvertToDestination(object? source);

    /// <summary>
    /// Converts the destination to the source value (converts back from).
    /// </summary>
    /// <param name="destination">The destination value to convert.</param>
    /// <returns>The converted source value.</returns>
    object? ConvertToSource(TDestination destination);
}