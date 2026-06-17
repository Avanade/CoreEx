namespace CoreEx;

/// <summary>
/// Represents an <b>Unexpected</b> (internal server error) exception.
/// </summary>
/// <remarks>The <see cref="Exception.Message"/> defaults to: <i>An unexpected internal server error has occurred.</i></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class UnexpectedInternalException(LText? message, Exception? innerException)
    : ExtendedException<UnexpectedInternalException>(message ?? new LText(typeof(UnexpectedInternalException).FullName, _message), innerException, true)
{
    private const string _message = "An unexpected internal server error has occurred.";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedInternalException"/> class.
    /// </summary>
    public UnexpectedInternalException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedInternalException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnexpectedInternalException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "unexpected-internal";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.InternalServerError);
        IsError = false;
    }
}