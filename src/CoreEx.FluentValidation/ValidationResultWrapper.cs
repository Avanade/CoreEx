// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Localization;
using CoreEx.Validation;
using FluentValidation.Results;
using System;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// Represents a <see cref="ValidationResult"/> wrapper to enable <i>CoreEx</i> <see cref="IValidationResult"/>.
    /// </summary>
    public class ValidationResultWrapper : IValidationResult
    {
        private ValidationException? _vex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultWrapper"/> class with the specified <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The <see cref="ValidationResult"/> to wrap.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ValidationResultWrapper(ValidationResult result) => Result = result ?? throw new ArgumentNullException(nameof(result));

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultWrapper"/> class where the value was <c>null</c> and therefore no corresponding <see cref="Result"/>.
        /// </summary>
        public ValidationResultWrapper() => _vex = new ValidationException(new LText("CoreEx.FluentValidation.NullValueException", "Value is required."));

        /// <summary>
        /// Gets the underlying <see cref="ValidationResult"/> that is being wrapped (where applicable).
        /// </summary>
        public ValidationResult? Result { get; }

        /// <inheritdoc/>
        public bool HasErrors => Result == null || !Result.IsValid;

        /// <inheritdoc/>
        public MessageItemCollection? Errors => HasErrors ? ToValidationException()!.Messages : null;

        /// <inheritdoc/>
        IValidationResult IValidationResult.ThrowOnError() => ThrowOnError();

        /// <summary>
        /// Throws a <see cref="ValidationException"/> where <see cref="HasErrors"/>.
        /// </summary>
        /// <returns>The <see cref="ValidationResultWrapper"/> to support fluent-style method-chaining.</returns>
        public ValidationResultWrapper ThrowOnError()
        {
            if (HasErrors)
                throw ToValidationException()!;

            return this;
        }

        /// <inheritdoc/>
        public ValidationException? ToValidationException() => HasErrors ? (_vex ??= Result!.ToValidationException()) : null;
    }
}