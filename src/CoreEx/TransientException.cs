namespace CoreEx;

/// <summary>
/// Represents a <b>Transient</b> exception; i.e. is a candidate for a retry.
/// </summary>
/// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A transient error has occurred; please try again.</i></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class TransientException(LText? message, Exception? innerException) 
    : ExtendedException<TransientException>(message ?? new LText(typeof(TransientException).FullName, _message), innerException)
{
    private const string _message = "A transient error has occurred; please try again.";

    /// <summary>
    /// Gets the default retry after interval.
    /// </summary>
    public readonly static TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Initializes a new instance of the <see cref="TransientException"/> class.
    /// </summary>
    public TransientException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransientException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public TransientException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "transient";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.ServiceUnavailable);
        IsTransient = true;
        RetryAfter = DefaultRetryAfter;
    }
}