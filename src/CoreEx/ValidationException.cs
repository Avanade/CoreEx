namespace CoreEx;

/// <summary>
/// Represents a <b>Validation</b> exception.
/// </summary>
/// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A data validation error occurred.</i></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class ValidationException(LText? message, Exception? innerException) 
    : ExtendedException<ValidationException>(message ?? new LText(typeof(ValidationException).FullName, _message), innerException)
{
    private const string _message = "A data validation error occurred.";

    /// <summary>
    /// Creates a new <see cref="ValidationException"/> using a <see cref="MessageItem"/> with the specified <paramref name="property"/> and <paramref name="text"/>.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="text">The message text.</param>
    /// <returns>The <see cref="ValidationException"/>.</returns>
    public static ValidationException Create(string? property, LText text) => new(MessageItem.CreateErrorMessage(property, text));

    /// <summary>
    /// Creates a new <see cref="ValidationException"/> using a <see cref="MessageItem"/> with the specified <paramref name="property"/>, text <paramref name="format"/> and additional <paramref name="values"/> included in the text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>The <see cref="ValidationException"/>.</returns>
    public static ValidationException Create(string? property, LText format, params IEnumerable<object?> values) => new(MessageItem.CreateErrorMessage(property, format, values));

    /// <summary>
    /// Creates a validation exception for a direct or nested property using its JSON path.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <param name="propertyExpression">The property chain, which is inspected without execution.</param>
    /// <param name="text">The message text; null uses the standard invalid format and the property's friendly metadata text.</param>
    /// <returns>The validation exception.</returns>
    public static ValidationException Create<TEntity>(Expression<Func<TEntity, object?>> propertyExpression, LText? text = null) where TEntity : class
        => Create(propertyExpression, null, text);

    /// <summary>
    /// Creates a validation exception for a direct or nested property using its JSON path and a formatted message.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <param name="propertyExpression">The property chain, which is inspected without execution.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values included in the message; no property label or current value is prepended.</param>
    /// <returns>The validation exception.</returns>
    public static ValidationException Create<TEntity>(Expression<Func<TEntity, object?>> propertyExpression, LText? format, params IEnumerable<object?> values) where TEntity : class
        => Create(propertyExpression, null, format, values);

    /// <summary>
    /// Creates a validation exception for a direct or nested property using explicit JSON naming options.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <param name="propertyExpression">The property chain, which is inspected without execution.</param>
    /// <param name="options">The serializer options used for naming; defaults to <see cref="JsonDefaults.SerializerOptions"/>.</param>
    /// <param name="text">The message text; null uses the standard invalid format and the property's friendly metadata text.</param>
    /// <returns>The validation exception.</returns>
    public static ValidationException Create<TEntity>(Expression<Func<TEntity, object?>> propertyExpression, JsonSerializerOptions? options, LText? text) where TEntity : class
        => new(MessageItem.CreateErrorMessage(propertyExpression, options, text));

    /// <summary>
    /// Creates a validation exception for a direct or nested property using explicit JSON naming options and a formatted message.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <param name="propertyExpression">The property chain, which is inspected without execution.</param>
    /// <param name="options">The serializer options used for naming; defaults to <see cref="JsonDefaults.SerializerOptions"/>.</param>
    /// <param name="format">The message text or composite format string.</param>
    /// <param name="values">The values included in the message; no property label or current value is prepended.</param>
    /// <returns>The validation exception.</returns>
    public static ValidationException Create<TEntity>(Expression<Func<TEntity, object?>> propertyExpression, JsonSerializerOptions? options, LText? format, params IEnumerable<object?> values) where TEntity : class
    {
        var args = values.ToArray();
        return args.Length == 0 ? Create(propertyExpression, options, format)
            : Create(RuntimeMetadata.GetJsonPath(propertyExpression, options), format ?? throw new ArgumentNullException(nameof(format)), args);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ValidationException(LText? message) : this(message, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> with a <see cref="MessageItem"/> collection and optional <paramref name="message"/>.
    /// </summary>
    /// <param name="messages">The <see cref="MessageItem"/> collection.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner <see cref="Exception"/>.</param>
    public ValidationException(IEnumerable<MessageItem> messages, LText? message = null, Exception? innerException = null) : this(message, innerException)
    {
        if (messages is not null)
            Messages = [.. messages.Where(x => x.Type == MessageType.Error)];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> with a single <see cref="MessageItem"/>.
    /// </summary>
    /// <param name="item">The <see cref="MessageItem"/>.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner <see cref="Exception"/>.</param>
    public ValidationException(MessageItem item, LText? message = null, Exception? innerException = null) : this(message, innerException)
    {
        if (item is not null && item.Type == MessageType.Error)
            Messages = [item];
    }

    /// <summary>
    /// Gets the underlying message(s) where applicable.
    /// </summary>
    public MessageItemCollection? Messages { get; }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "validation";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.BadRequest);
    }

    /// <inheritdoc/>
    /// <remarks>Prepends the <see cref="Messages"/> where applicable.</remarks>
    public override string ToString()
    {
        if (Messages is null || Messages.Count == 0)
            return base.ToString();

        var sb = new StringBuilder("Messages:");
        foreach (var mi in Messages.Where(x => x.Type == MessageType.Error))
        {
            sb.Append($" [{mi.Property}: {mi.Text}]");
        }

        return sb.ToString() + Environment.NewLine + base.ToString();
    }
}
