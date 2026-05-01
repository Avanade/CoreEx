namespace CoreEx.Validation;

/// <summary>
/// Provides standard validation-related capabilities.
/// </summary>
public static class Validation
{
    /// <summary>
    /// Gets or sets the format string for the <i>mandatory</i> error message.
    /// </summary>
    /// <remarks>Defaults to: '<c>{0} is required.</c>'.</remarks>
    public static LText MandatoryFormat { get; set; } = new("CoreEx.Validation.MandatoryFormat", "{0} is required.");

    /// <summary>
    /// Gets or sets the format string for an <i>invalid value</i> error message.
    /// </summary>
    /// <remarks>Defaults to: '<c>{0} is invalid: {1}.</c>'.</remarks>
    public static LText InvalidValueFormat { get; set; } = new("CoreEx.Validation.InvalidValueFormat", "{0} is invalid: {1}.");

    /// <summary>
    /// Gets or sets the value name.
    /// </summary>
    /// <remarks>Defaults to: '<c>value</c>'.</remarks>
    public static string ValueName { get; set; } = "value";

    /// <summary>
    /// Gets or sets the value <see cref="LText"/>.
    /// </summary>
    public static LText ValueText { get; set; } = new("CoreEx.Validation.ValueText", "Value");

    /// <summary>
    /// Gets or sets the <see cref="LText"/> representation of <see langword="null"/>.
    /// </summary>
    /// <remarks>Defaults to: '<c>(null)</c>'.</remarks>
    public static LText NullText { get; set; } = new("CoreEx.Validation.NullText", "(null)");

    /// <summary>
    /// Gets or sets the key name.
    /// </summary>
    /// <remarks>Defaults to: '<c>key</c>'.</remarks>
    public static string KeyName { get; set; } = "key";

    /// <summary>
    /// Creates a required value <see cref="ValidationException"/> <see cref="Result"/>.
    /// </summary>
    /// <param name="name">The value name.</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    /// <param name="configure">An optional action to configure the <see cref="ValidationException"/>.</param>
    /// <returns>The resulting <see cref="ValidationException"/> <see cref="Result"/>.</returns>
    public static TResult CreateRequiredValueResult<TResult>(string? name = null, LText? text = null, Action<ValidationException>? configure = null) where TResult : IResult, new()
    {
        var ex = new ValidationException(MessageItem.CreateErrorMessage(name ?? ValueName, MandatoryFormat, text ?? name?.ToSentenceCase() ?? ValueText));
        configure?.Invoke(ex);
        return (TResult)new TResult().ToFailure(ex);
    }

    /// <summary>
    /// Creates an invalid value <see cref="ValidationException"/> <see cref="Result"/>.
    /// </summary>
    /// <param name="message">The context message.</param>
    /// <param name="name">The value name.</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    /// <param name="configure">An optional action to configure the <see cref="ValidationException"/>.</param>
    /// <returns>The resulting <see cref="ValidationException"/> <see cref="Result"/>.</returns>
    public static TResult CreateInvalidValueResult<TResult>(string message, string? name = null, LText? text = null, Action<ValidationException>? configure = null) where TResult : IResult, new()
    {
        var ex = new ValidationException(MessageItem.CreateErrorMessage(name ?? ValueName, InvalidValueFormat, text ?? name?.ToSentenceCase() ?? ValueText, message.ThrowIfNullOrEmpty().Trim('.')));
        configure?.Invoke(ex);
        return (TResult)new TResult().ToFailure(ex);
    }
}