// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Adds validation-related extension methods.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Validates the value asynchronously using the specified <paramref name="validator"/>.
        /// </summary>
        /// <typeparam name="T">The underlying value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="validator">The corresponding <see cref="IValidator{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static async Task<T> ValidateAsync<T>(this T value, IValidator<T> validator, CancellationToken cancellationToken = default)
        {
            (await (validator ?? throw new ArgumentNullException(nameof(validator))).ValidateAsync(value, cancellationToken).ConfigureAwait(false)).ThrowOnError();
            return value;
        }
    }
}