namespace CoreEx.EntityFrameworkCore.Converters;

/// <summary>
/// Provides a <see cref="JsonElement"/> and <see cref="string"/> entity-framework (EF) <see cref="ValueConverter{TModel, TProvider}"/>.
/// </summary>
public sealed class JsonElementStringEfConverter() : ValueConverterBridge<JsonElement?, string?>(Mapping.Converters.JsonElementStringConverter.Default)
{
    /// <summary>
    /// Gets the default <see cref="JsonElementStringEfConverter"/>.
    /// </summary>
    public static JsonElementStringEfConverter Default { get; } = new();
}