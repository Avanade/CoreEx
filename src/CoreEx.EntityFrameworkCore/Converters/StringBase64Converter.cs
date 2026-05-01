namespace CoreEx.EntityFrameworkCore.Converters;

/// <summary>
/// Provides a <see cref="string"/> to <see cref="byte"/> <see cref="Array"/> converter (uses <see cref="Convert.FromBase64String(string)"/> and <see cref="Convert.ToBase64String(byte[])"/>). <see cref="ValueConverter{TModel, TProvider}"/>.
/// </summary>
public sealed class StringBase64Converter() : ValueConverter<string?, byte[]?>(
    s => CoreEx.Mapping.Converters.StringBase64Converter.Default.ConvertToDestination(s),
    a => CoreEx.Mapping.Converters.StringBase64Converter.Default.ConvertToSource(a))
{
    /// <summary>
    /// Gets the default <see cref="StringBase64Converter"/>.
    /// </summary>
    public static StringBase64Converter Default { get; } = new();
}