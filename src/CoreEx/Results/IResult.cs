// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Results
{
    /// <summary>
    /// Enables the use of a <c>Result</c> type to represent the outcome of an operation.
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Gets the underlying value where <see cref="IsSuccess"/>.
        /// </summary>
        object? Value { get; }

        /// <summary>
        /// Gets the underlying error represented as an <see cref="Exception"/>.
        /// </summary>
        Exception Error { get; }

        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Indicates whether the operation was a failure.
        /// </summary>
        bool IsFailure { get; }

        /// <summary>
        /// Creates an equivalent <see cref="IResult"/> that is considered <see cref="IsFailure"/>.
        /// </summary>
        /// <param name="error">The underlying error represented as an <see cref="Exception"/>.</param>
        /// <returns>The resulting <see cref="IResult"/>.</returns>
        IResult ToFailure(Exception error);
    }
}