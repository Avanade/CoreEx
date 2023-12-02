// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables <see cref="Value"/> validation results.
    /// </summary>
    public interface IValidationResult : ITypedToResult
    {
        /// <summary>
        /// Gets the originating value being validated.
        /// </summary>
        object? Value { get; }

        /// <summary>
        /// Indicates whether there has been a validation or other related error.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Gets a <see cref="MessageItemCollection"/> where <see cref="HasErrors"/> and individual errors have been recorded or other <see cref="MessageType"/> has been recorded; otherwise, <c>null</c>.
        /// </summary>
        MessageItemCollection? Messages { get; }

        /// <summary>
        /// Converts the <see cref="IValidationResult"/> into a corresponding <see cref="Exception"/>.
        /// </summary>
        /// <returns>The corresponding <see cref="Exception"/> (typically a <see cref="ValidationException"/>) where <see cref="HasErrors"/>; otherwise, <c>null</c>.</returns>
        Exception? ToException();

        /// <summary>
        /// Throws an <see cref="Exception"/> (typically a <see cref="ValidationException"/>) where <see cref="HasErrors"/>.
        /// </summary>
        /// <returns>The <see cref="IValidationResult"/> to support fluent-style method-chaining.</returns>
        IValidationResult ThrowOnError();

        /// <summary>
        /// Gets the <see cref="Result.IsFailure"/> <see cref="Result"/> where returned from within an underlying validation operation.
        /// </summary>
        /// <remarks>Where the related <see cref="Result"/> has an <see cref="Result.IsFailure"/> state then the underlying <see cref="Result.Error"/> takes precedence over the validation <see cref="Messages"/>.</remarks>
        Result? FailureResult { get; }
    }
}