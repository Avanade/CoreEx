// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables typed <see cref="Value"/> validation results.
    /// </summary>
    /// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
    public interface IValidationResult<out T> : IValidationResult
    {
        /// <inheritdoc/>
        object? IValidationResult.Value => Value;

        /// <summary>
        /// Gets the originating value being validated.
        /// </summary>
        new T? Value { get; }
    }
}