namespace CoreEx.Abstractions;

/// <summary>
/// Provides the base <see cref="IExtendedException"/> implementation.
/// </summary>
public abstract class ExtendedException: Exception, IExtendedException
{
    private readonly Lazy<Dictionary<string, object?>> _extensions = new(() => []);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedException{TSelf}"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner <see cref="Exception"/>.</param>
    /// <param name="exceptionType">The <see cref="Exception"/> <see cref="Type"/>.</param>
    /// <param name="defaultLoggingEnablement">The default logging enablement where no corresponding configuration setting is found.</param>
    public ExtendedException(LText? message, Exception? innerException, Type? exceptionType = null, bool defaultLoggingEnablement = false) : base(message, innerException)
    {
        ShouldBeLogged = Internal.GetConfigurationValueWithFallback<bool>($"CoreEx:Exceptions:{(exceptionType ?? GetType()).Name}:LoggingEnabled", "CoreEx:Exceptions:LoggingEnabled", defaultLoggingEnablement);
        OnInitialize();
    }

    /// <summary>
    /// Provides the opportunity to perform specific initialization.
    /// </summary>
    protected abstract void OnInitialize();

    /// <inheritdoc/>
    public string? Detail { get; set; }

    /// <inheritdoc/>
    public string? ErrorType { get; set; }

    /// <inheritdoc/>
    public string? ErrorCode { get; set; }

    /// <inheritdoc/>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.InternalServerError;

    /// <inheritdoc/>
    public bool IsError { get; set; } = true;

    /// <inheritdoc/>
    public bool IsTransient { get; set; }

    /// <inheritdoc/>
    public TimeSpan? RetryAfter { get; set; }

    /// <inheritdoc/>
    public bool ShouldBeLogged { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, object?> Extensions => _extensions.Value;

    /// <inheritdoc/>
    public bool HasExtensions => _extensions.IsValueCreated && _extensions.Value.Count > 0;

    /// <inheritdoc/>
    public Result ToResult() => new(this);

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder(base.ToString());
        sb.AppendLine();
        sb.Append($"-> ErrorType: {ErrorType}");
        if (ErrorCode is not null)
            sb.Append($", ErrorCode: {ErrorCode}");

        if (Detail is not null)
            sb.Append($", Detail: {Detail}");

        if (HasExtensions)
        {
            foreach (var kvp in Extensions)
                sb.Append($", {kvp.Key}: {kvp.Value}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts an <paramref name="exception"/> into an <see cref="IResult"/> where <typeparamref name="TResult"/> is an <see cref="IResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
    /// <param name="exception">The <see cref="Exception"/>.</param>
    /// <param name="result">The resulting <typeparamref name="TResult"/> <see cref="IResult.IsFailure"/> value.</param>
    /// <param name="forceAnyException">Indicates whether to force conversion for any exception, not just those implementing <see cref="IExtendedException"/> and considered <see cref="IExtendedException.IsError"/>.</param>
    /// <returns><see langword="true"/> where the conversion was successful; otherwise, <see langword="false"/>.</returns>
    public static bool TryConvertExceptionToResult<TResult>(Exception exception, [NotNullWhen(true)] out TResult? result, bool forceAnyException = false)
    {
        exception.ThrowIfNull();

        // Where the result is an IResult (ROP) and the exception is considered an error then return as an IResult _failure_; unless being forced for any exception.
        if (forceAnyException || (exception is IExtendedException eex && eex.IsError))
        {
            var dresult = default(TResult);
            if (dresult is IResult dir)
            {
                result = (TResult)dir.ToFailure(exception);
                return true;
            }
        }

        // No conversion possible.
        result = default;
        return false;
    }
}