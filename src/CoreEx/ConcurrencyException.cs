namespace CoreEx;

/// <summary>
/// Represents a data <b>Concurrency</b> exception.
/// </summary>
/// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A concurrency error occurred; please refresh the data and try again.</i></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class ConcurrencyException(LText? message, Exception? innerException) 
    : ExtendedException<ConcurrencyException>(message ?? new LText(typeof(ConcurrencyException).FullName, _message), innerException)
{
    private const string _message = "A concurrency error occurred; please refresh the data and try again.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    public ConcurrencyException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConcurrencyException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "concurrency";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.PreconditionFailed);
    }
}