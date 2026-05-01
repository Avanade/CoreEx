namespace CoreEx;

/// <summary>
/// Represents a data <b>Duplicate</b> exception.
/// </summary>
/// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A duplicate error occurred.</i></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class DuplicateException(LText? message, Exception? innerException) 
    : ExtendedException<DuplicateException>(message ?? new LText(typeof(DuplicateException).FullName, _message), innerException)
{
    private const string _message = "A duplicate error occurred.";

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateException"/> class.
    /// </summary>
    public DuplicateException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DuplicateException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "duplicate";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.Conflict);
    }
}