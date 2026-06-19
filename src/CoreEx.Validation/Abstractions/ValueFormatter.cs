namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Provides value formatter configuration and corresponding <see cref="ToLText{T}(T)"/>.
/// </summary>
/// <param name="format">The <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> format to use when localizing the property value within an error message.</param>
/// <param name="formatProvider">The optional <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> <see cref="IFormatProvider"/> to use when localizing the property value within an error message.</param>
/// <param name="quotingCharacter">The optional quoting character so it appears as a literal string.</param>
public readonly struct ValueFormatter(string? format, IFormatProvider? formatProvider = null, char? quotingCharacter = '\'')
{
    private readonly bool _useStringFormat = !string.IsNullOrEmpty(format) && format.Contains("{0");

    /// <summary>
    /// Gets the default <see cref="ValueFormatter"/>.
    /// </summary>
    public static ValueFormatter Default { get; } = new ValueFormatter(null, null, '\'');

    /// <summary>
    /// Gets the <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> format to use when localizing the property value within an error message.
    /// </summary>
    /// <remarks>Also supports <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting">composite formatting</see>.</remarks>
    public string? Format { get; } = format;

    /// <summary>
    /// Gets the optional <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> <see cref="IFormatProvider"/> to use when localizing the property value within an error message.
    /// </summary>
    public IFormatProvider? FormatProvider { get; } = formatProvider;

    /// <summary>
    /// Gets the optional quoting character so it appears as a literal string.
    /// </summary>
    public char? QuotingCharacter { get; } = quotingCharacter;

    /// <summary>
    /// Formats the specified <paramref name="value"/> as a <see cref="LText"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The formatted <see cref="LText"/>.</returns>
    public LText ToLText<T>(T? value)
    {
        var text = _useStringFormat 
            ? string.Format(Format!, value) 
            : value is IFormattable f
                ? f.ToString(Format, FormatProvider)
                : value?.ToString();

        if (text is null)
            return ValidatorStrings.NullText;

        if (QuotingCharacter is null)
            return new LText(text);

        return new LText(string.Create(text.Length + 2, (text, QuotingCharacter.Value),
            (span, state) => 
            {
                span[0] = state.Value;
                state.text.AsSpan().CopyTo(span[1..^1]);
                span[^1] = state.Value;
            }));
    }
}