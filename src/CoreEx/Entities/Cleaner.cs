namespace CoreEx.Entities;

/// <summary>
/// Provides capabilities to clean a specified value.
/// </summary>
public static class Cleaner
{
    private static StringTrim? _stringTrim;
    private static StringTransform? _stringTransform;
    private static StringCase? _stringCase;
    private static DateTimeTransform? _dateTimeTransform;
    private static CleanOption? _cleanOption;

    /// <summary>
    /// Resets the <see cref="DefaultStringTrim"/>, <see cref="DefaultStringTransform"/>, and <see cref="DefaultStringCase"/> to their respective default values.
    /// </summary>
    public static void ResetDefaults()
    {
        _stringTrim = null;
        _stringTransform = null;
        _stringCase = null;
        _dateTimeTransform = null;
        _cleanOption = null;
    }

    /// <summary>
    /// Gets or sets the default <see cref="StringTrim"/> for all values unless explicitly overridden. Defaults to <see cref="StringTrim.End"/>.
    /// </summary>
    public static StringTrim DefaultStringTrim
    {
        get => _stringTrim ??= Internal.GetConfigurationValue("CoreEx:Entities:Cleaner:DefaultStringTrim", StringTrim.End);
        set => _stringTrim = value == StringTrim.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultStringTrim)) : value;
    }

    /// <summary>
    /// Gets or sets the default <see cref="StringTransform"/> for all values unless explicitly overridden. Defaults to <see cref="StringTransform.EmptyToNull"/>.
    /// </summary>
    public static StringTransform DefaultStringTransform
    {
        get => _stringTransform ??= Internal.GetConfigurationValue("CoreEx:Entities:Cleaner:DefaultStringTransform", StringTransform.EmptyToNull);
        set => _stringTransform = value == StringTransform.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultStringTransform)) : value;
    }

    /// <summary>
    /// Gets or sets the default <see cref="StringCase"/> for all values unless explicitly overridden. Defaults to <see cref="StringCase.None"/>.
    /// </summary>
    public static StringCase DefaultStringCase
    {
        get => _stringCase ??= Internal.GetConfigurationValue("CoreEx:Entities:Cleaner:DefaultStringCase", StringCase.None);
        set => _stringCase = value == StringCase.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultStringCase)) : value;
    }

    /// <summary>
    /// Gets or sets the default <see cref="DateTimeTransform"/> for all values unless explicitly overridden. Defaults to <see cref="DateTimeTransform.DateTimeUtc"/>.
    /// </summary>
    public static DateTimeTransform DefaultDateTimeTransform
    {
        get => _dateTimeTransform ??= Internal.GetConfigurationValue("CoreEx:Entities:Cleaner:DefaultDateTimeTransform", DateTimeTransform.DateTimeUtc);
        set => _dateTimeTransform = value == DateTimeTransform.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultDateTimeTransform)) : value;
    }

    /// <summary>
    /// Gets or sets the default <see cref="CleanOption"/> for all values unless explicitly overridden. Defaults to <see cref="CleanOption.CleanAndDefault"/>.
    /// </summary>
    public static CleanOption DefaultCleanOption
    {
        get => _cleanOption ??= Internal.GetConfigurationValue("CoreEx:Entities:Cleaner:DefaultCleanOption", CleanOption.CleanAndDefault);
        set => _cleanOption = value == CleanOption.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultCleanOption)) : value;
    }

    /// <summary>
    /// Cleans a <see cref="string"/> using the specified <paramref name="trim"/> and <paramref name="transform"/>.
    /// </summary>
    /// <param name="value">The value to clean.</param>
    /// <param name="trim">The <see cref="StringTrim"/> (defaults to <see cref="DefaultStringTrim"/>).</param>
    /// <param name="transform">The <see cref="StringTransform"/> (defaults to <see cref="DefaultStringTransform"/>).</param>
    /// <param name="casing">The <see cref="StringCase"/> (defaults to <see cref="DefaultStringCase"/>).</param>
    /// <returns>The cleaned value.</returns>
    public static string? Clean(string? value, StringTrim trim = StringTrim.UseDefault, StringTransform transform = StringTransform.UseDefault, StringCase casing = StringCase.UseDefault)
    {
        if (transform == StringTransform.UseDefault)
            transform = DefaultStringTransform;

        // Handle a null string.
        if (value is null)
        {
            if (transform == StringTransform.NullToEmpty)
                return string.Empty;
            else
                return null;
        }

        if (trim == StringTrim.UseDefault)
            trim = DefaultStringTrim;

        // Trim the string.
        var tmp = trim switch
        {
            StringTrim.End => value.TrimEnd(),
            StringTrim.Both => value.Trim(),
            StringTrim.Start => value.TrimStart(),
            _ => value,
        };

        // Transform the string.
        tmp = transform switch
        {
            StringTransform.EmptyToNull => string.IsNullOrEmpty(tmp) ? null : tmp,
            StringTransform.NullToEmpty => tmp ?? string.Empty,
            _ => tmp,
        };

        if (string.IsNullOrEmpty(tmp))
            return tmp;

        // Apply casing to the string.
        if (casing == StringCase.UseDefault)
            casing = DefaultStringCase;

        return casing switch
        {
            StringCase.None => tmp,
            StringCase.Lower => CultureInfo.CurrentCulture.TextInfo.ToCasing(tmp, TextInfoCasing.Lower),
            StringCase.Upper => CultureInfo.CurrentCulture.TextInfo.ToCasing(tmp, TextInfoCasing.Upper),
            StringCase.Title => CultureInfo.CurrentCulture.TextInfo.ToCasing(tmp, TextInfoCasing.Title),
            _ => tmp
        };
    }

    /// <summary>
    /// Cleans a <see cref="DateTime"/> value.
    /// </summary>
    /// <param name="value">The value to clean.</param>
    /// <param name="transform">The <see cref="DateTimeTransform"/> to be applied.</param>
    /// <returns>The cleaned value.</returns>
    public static DateTime Clean(DateTime value, DateTimeTransform transform)
    {
        if (transform == DateTimeTransform.UseDefault)
            transform = DefaultDateTimeTransform;

        switch (transform)
        {
            case DateTimeTransform.DateTimeUtc:
                if (value.Kind != DateTimeKind.Utc)
                {
                    if (value == DateTime.MinValue || value == DateTime.MaxValue || value.Kind == DateTimeKind.Unspecified)
                        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
                    else
                        return (value.Kind == DateTimeKind.Utc) ? value : TimeZoneInfo.ConvertTime(value, TimeZoneInfo.Utc);
                }

                break;

            case DateTimeTransform.DateTimeLocal:
                if (value.Kind != DateTimeKind.Local)
                {
                    if (value == DateTime.MinValue || value == DateTime.MaxValue || value.Kind == DateTimeKind.Unspecified)
                        return DateTime.SpecifyKind(value, DateTimeKind.Local);
                    else
                        return (value.Kind == DateTimeKind.Local) ? value : TimeZoneInfo.ConvertTime(value, TimeZoneInfo.Local);
                }

                break;

            case DateTimeTransform.DateOnly:
                if (value.Kind == DateTimeKind.Unspecified && value.TimeOfDay == TimeSpan.Zero)
                    return value;
                else
                    return DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);

            case DateTimeTransform.DateTimeUnspecified:
                if (value.Kind != DateTimeKind.Unspecified)
                    return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

                break;
        }

        return value;
    }

    /// <summary>
    /// Cleans a <see cref="Nullable{DateTime}"/> value.
    /// </summary>
    /// <param name="value">The value to clean.</param>
    /// <param name="transform">The <see cref="DateTimeTransform"/> to be applied.</param>
    /// <returns>The cleaned value.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static DateTime? Clean(DateTime? value, DateTimeTransform transform) => value is null || !value.HasValue ? value : Clean(value.Value, transform);

    /// <summary>
    /// Cleans a value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to clean.</param>
    /// <returns>The cleaned <paramref name="value"/>.</returns>
    public static T? Clean<T>(T value) => RuntimeMetadata.Clean<T>(value);
}