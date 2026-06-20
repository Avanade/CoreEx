namespace CoreEx.Results;

public static partial class ResultsExtensions
{
    extension(IResult result)
    {
        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="NotFoundException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsNotFoundError => result.IsFailureOfType<NotFoundException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="ValidationException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsValidationError => result.IsFailureOfType<ValidationException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="BusinessException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsBusinessError => result.IsFailureOfType<BusinessException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="ConflictException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsConflictError => result.IsFailureOfType<ConflictException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="ConcurrencyException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsConcurrencyError => result.IsFailureOfType<ConcurrencyException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="DataConsistencyException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsDataConsistencyError => result.IsFailureOfType<DataConsistencyException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="DuplicateException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsDuplicateError => result.IsFailureOfType<DuplicateException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="TransientException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsTransientError => result.IsFailureOfType<TransientException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="AuthenticationException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsAuthenticationError => result.IsFailureOfType<AuthenticationException>();

        /// <summary>
        /// Indicates whether the <see cref="IResult"/> <see cref="IResult.IsFailure"/> and the <see cref="IResult"/> is a <see cref="AuthorizationException"/>.
        /// </summary>
        /// <remarks>A <see langword="false"/> result does not imply <see cref="IResult.IsSuccess"/>, just that it is not in the specified error state.</remarks>
        public bool IsAuthorizationError => result.IsFailureOfType<AuthorizationException>();
    }
}