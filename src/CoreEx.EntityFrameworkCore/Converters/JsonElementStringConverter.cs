namespace CoreEx.EntityFrameworkCore.Converters;

/// <summary>
/// Provides a <see cref="JsonElement"/> and <see cref="string"/> <see cref="ValueConverter{TModel, TProvider}"/>.
/// </summary>
public sealed class JsonElementStringConverter() : ValueConverterBridge<JsonElement?, string?>(Mapping.Converters.JsonElementStringConverter.Default)
{
    /// <summary>
    /// Gets the default <see cref="JsonElementStringConverter"/>.
    /// </summary>
    public static JsonElementStringConverter Default { get; } = new();
}