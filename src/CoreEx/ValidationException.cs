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