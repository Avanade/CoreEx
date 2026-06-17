namespace CoreEx;

/// <summary>
/// Represents an <b>Conflict</b> exception.
/// </summary>
/// <remarks>An example would be where the identifier provided for a create operation already exists.
/// <para>The <see cref="Exception.Message"/> defaults to: <i>A data conflict occurred.</i></para></remarks>
public class ConflictException(LText? message, Exception? innerException) 
    : ExtendedException<ConflictException>(message ?? new LText(typeof(ConflictException).FullName, _message), innerException)
{
    private const string _message = "A data conflict occurred.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    public ConflictException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConflictException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "conflict";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.Conflict);
    }
}