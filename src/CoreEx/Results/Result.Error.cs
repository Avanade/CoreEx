namespace CoreEx.Results;

public readonly partial struct Result
{
    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="AuthenticationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result AuthenticationError(LText? message = default, Action<AuthenticationException>? configure = null) => (new AuthenticationException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="AuthorizationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result AuthorizationError(LText? message = default, Action<AuthorizationException>? configure = null) => (new AuthorizationException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="BusinessException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result BusinessError(LText? message = default, Action<BusinessException>? configure = null) => (new BusinessException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ConcurrencyException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result ConcurrencyError(LText? message = default, Action<ConcurrencyException>? configure = null) => (new ConcurrencyException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ConflictException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    /// <remarks>An example would be where the identifier provided for a Create operation already exists.</remarks>
    public static Result ConflictError(LText? message = default, Action<ConflictException>? configure = null) => (new ConflictException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="DuplicateException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result DuplicateError(LText? message = default, Action<DuplicateException>? configure = null) => (new DuplicateException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result NotFoundError(LText? message = default, Action<NotFoundException>? configure = null) => (new NotFoundException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="TransientException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result TransientError(LText? message = default, Action<TransientException>? configure = null) => (new TransientException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result ValidationError(LText? message = default, Action<ValidationException>? configure = null) => (new ValidationException(message)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="messages">The <see cref="MessageItem"/> list.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
    public static Result ValidationError(IEnumerable<MessageItem>? messages, Action<ValidationException>? configure = null) => (messages is null ? new ValidationException() : new ValidationException(messages)).Adjust(configure);

    /// <summary>
    /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">The <see cref="MessageItem"/>.</param>
    /// <param name="configure">The optional configuration action.</param>
    /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>       
    public static Result ValidationError(MessageItem message, Action<ValidationException>? configure = null) => (new ValidationException(message)).Adjust(configure);
}