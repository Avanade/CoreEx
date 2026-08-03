namespace CoreEx.EntityFrameworkCore.Converters;

/// <summary>
/// Provides a <typeparamref name="T"/> and JSON <see cref="string"/> entity-framework (EF) <see cref="ValueConverter{TModel, TProvider}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class TypeToJsonStringEfConverter<T>() : ValueConverterBridge<T, string?>(Mapping.Converters.TypeToJsonStringConverter<T>.Default)
{
    /// <summary>
    /// Gets the default <see cref="TypeToJsonStringEfConverter{T}"/>.
    /// </summary>
    public static TypeToJsonStringEfConverter<T> Default { get; } = new();
}
