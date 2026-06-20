namespace CoreEx.Localization;

/// <summary>
/// Provides access to the global/static <see cref="Current"/> and <see cref="GetUICulture"/>, etc.
/// </summary>
public static class TextProvider
{
    private static ITextProvider? _textProvider;
    private static ITextProvider? _backupTextProvider;

    /// <summary>
    /// Sets the <see cref="Current"/> <see cref="ITextProvider"/> instance explicitly.
    /// </summary>
    /// <param name="textProvider">The concrete <see cref="ITextProvider"/> instance.</param>
    public static void SetTextProvider(ITextProvider? textProvider) => _textProvider = textProvider;

    /// <summary>
    /// Gets the current <see cref="ITextProvider"/> instance using in the following order: <see cref="ExecutionContext.GetService{T}"/>, the explicit <see cref="SetTextProvider(ITextProvider)"/>, otherwise, <see cref="NullTextProvider"/>. 
    /// </summary>
    public static ITextProvider Current
    {
        get
        {
            var tp = ExecutionContext.GetService<ITextProvider>();
            if (tp is not null)
                return tp;

            if (_textProvider is not null)
                return _textProvider;

            return _backupTextProvider ??= new NullTextProvider();
        }
    }

    /// <summary>
    /// Gets the current UI <see cref="CultureInfo"/> (i.e. <see cref="ExecutionContext.UICulture"/> or <see cref="CultureInfo.CurrentCulture"/>).
    /// </summary>
    public static CultureInfo GetUICulture() => ExecutionContext.TryGetCurrent(out var ec) ? ec.UICulture : CultureInfo.CurrentUICulture;

    /// <summary>
    /// Replaces the format items in a string with the string representations of the corresponding objects in the specified array.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The optional arguments.</param>
    /// <remarks>The <see cref="string.Format(System.IFormatProvider, string, object[])"/> is used with the <see cref="GetUICulture"/> as the <see cref="System.IFormatProvider"/>.</remarks>
    public static string? Format(string? format, object?[]? args)
    {
        if (format is null || args is null || args.Length == 0)
            return format;

        return string.Format(GetUICulture(), format, args);
    }
}