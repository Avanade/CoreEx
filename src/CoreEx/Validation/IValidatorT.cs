// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables typed value validation.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    public interface IValidator<T> : IValidator
    {
        /// <inheritdoc/>
        Type IValidator.ValueType => typeof(T);

        /// <inheritdoc/>
        async Task<IValidationResult> IValidator.ValidateAsync(object? value, CancellationToken cancellationToken) => await ValidateAsync((T)value!, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Validate the <paramref name="value"/> asynchronously.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IValidationResult"/>.</returns>
        Task<IValidationResult<T>> ValidateAsync(T? value, CancellationToken cancellationToken);
    }
}