// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Adds validation-related extension methods.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Validates the value using the specified <paramref name="validator"/>.
        /// </summary>
        /// <typeparam name="T">The underlying value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="validator">The corresponding <see cref="IValidator{T}"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static T Validate<T>(this T value, IValidator<T> validator)
        {
            (validator ?? throw new ArgumentNullException(nameof(validator))).Validate(value).ThrowOnError();
            return value;
        }

        /// <summary>
        /// Validates the value asynchronously using the specified <paramref name="validator"/>.
        /// </summary>
        /// <typeparam name="T">The underlying value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="validator">The corresponding <see cref="IValidator{T}"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static async Task<T> ValidateAsync<T>(this T value, IValidator<T> validator)
        {
            (await (validator ?? throw new ArgumentNullException(nameof(validator))).ValidateAsync(value).ConfigureAwait(false)).ThrowOnError();
            return value;
        }
    }
}