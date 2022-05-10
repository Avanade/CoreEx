// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using FluentValidation.Results;
using System;
using FV = FluentValidation;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// <c>FluentValidation</c> extension methods.
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Throws a <see cref="ValidationException"/> where the <see cref="ValidationResult"/> has errors (is not <see cref="ValidationResult.IsValid"/>).
        /// </summary>
        /// <param name="validationResult">The <see cref="ValidationResult"/>.</param>
        /// <exception cref="ValidationException">The resulting <see cref="ValidationException"/> where the <see cref="ValidationResult"/> has errors.</exception>
        public static void ThrowValidationException(this ValidationResult validationResult)
        {
            var vex = ToValidationException(validationResult);
            if (vex != null)
                throw vex;
        }

        /// <summary>
        /// Creates a <see cref="ValidationException"/> where the <see cref="ValidationResult"/> has errors (is not <see cref="ValidationResult.IsValid"/>).
        /// </summary>
        /// <param name="validationResult">The <see cref="ValidationResult"/>.</param>
        /// <returns>The <see cref="ValidationException"/> where the <see cref="ValidationResult"/> has errors; otherwise, <c>null</c>.</returns>
        public static ValidationException? ToValidationException(this ValidationResult validationResult)
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

        /// <summary>
        /// Converts the <i>FluentValidation</i> <see cref="FV.IValidator{T}"/> to a <see cref="CoreEx.Validation.IValidator{T}"/> using a <see cref="ValidatorWrapper{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="validator">The <i>FluentValidation</i> <see cref="FV.IValidator{T}"/>.</param>
        /// <returns>The <see cref="CoreEx.Validation.IValidator{T}"/>.</returns>
        public static CoreEx.Validation.IValidator<T> Convert<T>(this FV.IValidator<T> validator) => new ValidatorWrapper<T>(validator);
    }
}