namespace CoreEx;

/// <summary>
/// Represents an <b>Authorization</b> exception.
/// </summary>
/// <remarks>The <see cref="Exception.Message"/> defaults to: <i>An authorization error occurred; you are not permitted to perform this action.</i></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class AuthorizationException(LText? message, Exception? innerException) 
    : ExtendedException<AuthorizationException>(message ?? new LText(typeof(AuthorizationException).FullName, _message), innerException)
{
    private const string _message = "An authorization error occurred; you are not permitted to perform this action.";

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationException"/> class.
    /// </summary>
    public AuthorizationException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AuthorizationException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "authorization";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.Forbidden);
    }
}