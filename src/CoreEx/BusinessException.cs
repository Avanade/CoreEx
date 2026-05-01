namespace CoreEx;

/// <summary>
/// Represents a <b>Business</b> exception.
/// </summary>
/// <remarks>This is a special purpose exception intended for a business-oriented error that could be returned to the consumer as-is. These are typically errors that are unlikely to be known upfront by the consuming
/// application and would be difficult to guard against. These are likely to occur regularly in a correctly functioning system. For example, a business rule violation such as "Customer cannot be deleted as they have
/// active orders" would be a good candidate for this type of exception. As distinct from a <see cref="ValidationException"/> which is intended for known/expected errors, for example, "Customer name is required".
/// <para>There is no default <see cref="Exception.Message"/>.</para></remarks>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class BusinessException(LText? message, Exception? innerException) 
    : ExtendedException<BusinessException>(message ?? new LText(typeof(BusinessException).FullName, _message), innerException)
{
    private const string _message = "A business error occurred.";

    /// <summary>
    /// Gets the business error type.
    /// </summary>
    public const string BusinessErrorType = "business";

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessException"/> class using the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BusinessException(LText? message) : this(message.ThrowIfNull(), null) { }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        ErrorType = BusinessErrorType;
        StatusCode = GetConfiguredStatusCode(HttpStatusCode.BadRequest);
    }
}