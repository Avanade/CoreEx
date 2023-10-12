// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Represents an error type.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Indicates a Validation error.
        /// </summary>
        ValidationError = 1,

        /// <summary>
        /// Indicates a Business error.
        /// </summary>
        BusinessError = 2,

        /// <summary>
        /// Indicates an Authorization error.
        /// </summary>
        AuthorizationError = 3,

        /// <summary>
        /// Indicates a Concurrency error.
        /// </summary>
        ConcurrencyError = 4,

        /// <summary>
        /// Indicates a Not Found error.
        /// </summary>
        NotFoundError = 5,

        /// <summary>
        /// Indicates a Conflict error.
        /// </summary>
        ConflictError = 6,

        /// <summary>
        /// Indicates a Duplicate error.
        /// </summary>
        DuplicateError = 7,

        /// <summary>
        /// Indicates an Authentication error.
        /// </summary>
        AuthenticationError = 8,

        /// <summary>
        /// Indicates a Transient error.
        /// </summary>
        TransientError = 9,

        /// <summary>
        /// Indicates a Data Consistency error.
        /// </summary>
        DataConsistencyError = 10,

        /// <summary>
        /// Indicates an unknown/unhandled error.
        /// </summary>
        UnhandledError = 88
    }
}