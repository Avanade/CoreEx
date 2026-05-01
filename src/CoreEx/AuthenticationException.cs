namespace CoreEx;

/// <summary>
/// Represents an <b>Authentication</b> exception.
/// </summary>
/// <remarks>The <see cref="Exception.Message"/> defaults to: <i>An authentication error occurred; the credentials provided are not valid.</i></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class AuthenticationException(LText? message, Exception? innerException) 
    : ExtendedException<AuthenticationException>(message ?? new LText(typeof(AuthenticationException).FullName, _message), innerException)
{
    private const string _message = "An authentication error occurred; the credentials provided are not valid.";

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
    /// </summary>
    public AuthenticationException() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AuthenticationException(LText? message) : this(message, null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = "authentication";
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.Unauthorized);
    }
}