namespace CoreEx.EntityFrameworkCore.Converters;

/// <summary>
/// Provides <see cref="ValueConverterBridge{TModel, TProvider}"/> convenience methods.
/// </summary>
public static class ValueConverterBridge
{
    /// <summary>
    /// Creates a new instance of the <see cref="ValueConverterBridge{TModel, TProvider}"/> class using the specified converter.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProvider">The provider <see cref="Type"/>.</typeparam>
    /// <param name="converter">The <see cref="Mapping.Converters.IConverter{TModel, TProvider}"/>.</param>
    /// <returns>The <see cref="ValueConverterBridge{TModel, TProvider}"/> instance.</returns>
    public static ValueConverterBridge<TModel, TProvider> Create<TModel, TProvider>(Mapping.Converters.IConverter<TModel, TProvider> converter) => new(converter);

    /// <summary>
    /// Creates a new instance of the <see cref="ValueConverterBridge{TModel, TProvider}"/> class using the specified converter.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProvider">The provider <see cref="Type"/>.</typeparam>
    /// <param name="converter">The <see cref="Mapping.Converters.IConverter{TModel, TProvider}"/>.</param>
    /// <returns>The <see cref="ValueConverterBridge{TModel, TProvider}"/> instance.</returns>
    public static ValueConverterBridge<TModel, TProvider> Create<TModel, TProvider>(CoreEx.Mapping.Converters.IConverter converter)
        => new(converter is Mapping.Converters.IConverter<TModel, TProvider> vc ? vc : throw new InvalidCastException("Invalid converter type."));
}