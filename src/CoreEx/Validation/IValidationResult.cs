// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;

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
        /// Indicates whether there has been a validation error.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Gets a <see cref="MessageItemCollection"/> where <see cref="HasErrors"/> and individual errors have been recorded or other <see cref="MessageType"/> has been recorded; otherwise, <c>null</c>.
        /// </summary>
        MessageItemCollection? Messages { get; }

        /// <summary>
        /// Converts the <see cref="IValidationResult"/> into a corresponding <see cref="ValidationException"/>.
        /// </summary>
        /// <returns>The corresponding <see cref="ValidationException"/> where <see cref="HasErrors"/>; otherwise, <c>null</c>.</returns>
        ValidationException? ToValidationException();

        /// <summary>
        /// Throws a <see cref="ValidationException"/> where <see cref="HasErrors"/>.
        /// </summary>
        /// <returns>The <see cref="IValidationResult"/> to support fluent-style method-chaining.</returns>
        IValidationResult ThrowOnError();
    }
}