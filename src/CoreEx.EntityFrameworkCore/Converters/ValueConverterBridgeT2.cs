namespace CoreEx.EntityFrameworkCore.Converters;

/// <summary>
/// Provides a <see cref="ValueConverter{TModel, TProvider}"/> implementation that uses the specified <see cref="Mapping.Converters.IConverter{TModel, TProvider}"/> bridging the conversion logic.
/// </summary>
/// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
/// <typeparam name="TProvider">The provider <see cref="Type"/>.</typeparam>
/// <param name="converter">The <see cref="Mapping.Converters.IConverter{TModel, TProvider}"/>.</param>
public class ValueConverterBridge<TModel, TProvider>(Mapping.Converters.IConverter<TModel, TProvider> converter) : ValueConverter<TModel, TProvider>(
    model => converter.ThrowIfNull().ConvertToDestination(model),
    provider => converter.ThrowIfNull().ConvertToSource(provider)) { }