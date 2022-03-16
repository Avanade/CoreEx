// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using FluentValidation.Results;
using System;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// <c>FluentValidation</c> extension methods for <see cref="ValidationResult"/>.
    /// </summary>
    public static class ValidationResultExtensions
    {
        /// <summary>
        /// Throws a <see cref="ValidationException"/> where the <see cref="ValidationResult"/> has errors (is not <see cref="ValidationResult.IsValid"/>).
        /// </summary>
        /// <param name="validationResult">The <see cref="ValidationResult"/>.</param>
        /// <exception cref="ValidationException">The resulting <see cref="ValidationException"/> where the <see cref="ValidationResult"/> has errors.</exception>
        public static void ThrowValidationException(this ValidationResult validationResult)
        {
            var vex = CreateValidationException(validationResult);
            if (vex != null)
                throw vex;
        }

        /// <summary>
        /// Creates a <see cref="ValidationException"/> where the <see cref="ValidationResult"/> has errors (is not <see cref="ValidationResult.IsValid"/>).
        /// </summary>
        /// <param name="validationResult">The <see cref="ValidationResult"/>.</param>
        /// <returns>The <see cref="ValidationException"/> where the <see cref="ValidationResult"/> has errors; otherwise, <c>null</c>.</returns>
        public static ValidationException? CreateValidationException(this ValidationResult validationResult)
        {
            if ((validationResult ?? throw new ArgumentNullException(nameof(validationResult))).IsValid)
                return null;

            var mic = new MessageItemCollection();
            foreach (var error in validationResult.Errors)
            {
                mic.AddPropertyError(error.PropertyName, error.ErrorMessage);
            }

            return new ValidationException(mic);
        }
    }
}