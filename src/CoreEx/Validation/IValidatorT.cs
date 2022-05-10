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

        /// <summary>
        /// Validate the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>The <see cref="IValidationResult"/>.</returns>
        IValidationResult IValidator.Validate(object value) => Validate((T)value ?? throw new ArgumentNullException(nameof(value)));

        /// <summary>
        /// Validate the <paramref name="value"/> asynchronously.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IValidationResult"/>.</returns>
        Task<IValidationResult> IValidator.ValidateAsync(object value, CancellationToken cancellationToken) => ValidateAsync((T)value ?? throw new ArgumentNullException(nameof(value)), cancellationToken);

        /// <summary>
        /// Validate the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>The <see cref="IValidationResult"/>.</returns>
        IValidationResult Validate(T? value);

        /// <summary>
        /// Validate the <paramref name="value"/> asynchronously.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IValidationResult"/>.</returns>
        Task<IValidationResult> ValidateAsync(T? value, CancellationToken cancellationToken = default);
    }
}