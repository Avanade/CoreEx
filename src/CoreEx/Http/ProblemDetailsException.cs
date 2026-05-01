namespace CoreEx.Http;

/// <summary>
/// Represents a <see cref="Abstractions.ProblemDetails"/> <see cref="Exception"/>.
/// </summary>
/// <param name="problemDetails">The <see cref="Abstractions.ProblemDetails"/> associated with the exception.</param>
/// <param name="innerException">The exception that is the cause of the current exception.</param>
/// <remarks>This exception does not implement <see cref="IExtendedException"/> by design; this is effectively an internal exception and behaves as such. To enable such behavior then the
/// <see cref="ToException{TException}"/> method can be used and orchestrated accordingly.</remarks>
public class ProblemDetailsException(ProblemDetails problemDetails, Exception? innerException) : Exception(problemDetails.ThrowIfNull().Title ?? problemDetails.Detail, innerException)
{
    /// <summary>
    /// Gets the <see cref="Abstractions.ProblemDetails"/> associated with the exception.
    /// </summary>
    public ProblemDetails ProblemDetails { get; } = problemDetails;

    /// <summary>
    /// Maps the underlying <see cref="ProblemDetails"/> to an exception of type <typeparamref name="TException"/>.
    /// </summary>
    /// <typeparam name="TException">THe <see cref="ExtendedException{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <returns>The corresponding <see cref="Exception"/>.</returns>
    /// <remarks>This is a best effort mapping as there is no means to preserve all information from the originating exception as some fidelity will be lost.</remarks>
    public TException ToException<TException>() where TException : ExtendedException<TException>
    {
        var exception = (TException)Activator.CreateInstance(typeof(TException), (LText)Message, this)!;

        exception.Detail = ProblemDetails.Detail;

        if (ProblemDetails.Status is not null)
            exception.StatusCode = (HttpStatusCode)ProblemDetails.Status.Value;

        exception.ErrorType = ProblemDetails.ErrorType;
        exception.ErrorCode = ProblemDetails.ErrorCode;
        exception.Detail = ProblemDetails.Detail;

        if (ProblemDetails.Extensions is not null)
        {
            foreach (var (key, value) in ProblemDetails.Extensions)
            {
                if (key == HttpNames.ErrorCodeName && value is string errorCode)
                {
                    exception.ErrorCode = errorCode;
                    continue;
                }

                if (key == HttpNames.ErrorTypeName && value is string errorType)
                {
                    exception.ErrorType = errorType;
                    continue;
                }

                exception.Extensions[key] = value;
            }
        }

        return exception;
    }

    /// <summary>
    /// Tries to get the <see cref="BusinessException"/> from the underlying <see cref="ProblemDetails"/>.
    /// </summary>
    /// <param name="exception">The resulting <see cref="BusinessException"/>.</param>
    /// <returns><c>true</c> indicates that the problem details has been determined as a valid <see cref="BusinessException"/>; otherwise, <c>false</c>.</returns>
    /// <remarks>A <see cref="BusinessException"/> is considered valid where its <see cref="IExtendedException.ErrorType"/> is <see cref="BusinessException.BusinessErrorType"/>.</remarks>
    public bool TryGetBusinessException([NotNullWhen(true)] out BusinessException? exception)
    {
        // Check that the error type is the expected business error type; this is required as the problem details may be representing some other exception type and we want to ensure that we only attempt
        // to map to a business exception where appropriate. 
        if (BusinessException.BusinessErrorType.Equals(ProblemDetails.ErrorType, StringComparison.OrdinalIgnoreCase))
        {
            exception = ToException<BusinessException>();
            return true;
        }

        exception = null;
        return false;
    }

    /// <summary>
    /// Determines if the underlying <see cref="ProblemDetails"/> represents a <see cref="BusinessException"/> and if so, throws it.
    /// </summary>
    /// <returns>The <see cref="ProblemDetailsException"/> to support fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="TryGetBusinessException(out BusinessException?)"/>.</remarks>
    public ProblemDetailsException ThrowOnBusinessException()
    {
        if (TryGetBusinessException(out var exception))
            throw exception;

        return this;
    }
}