// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Extends the <see cref="IValidator{T}"/>
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    public interface IValidatorEx<T> : IValidatorEx, IValidator<T>
    {
        /// <inheritdoc/>
        async Task<IValidationContext> IValidatorEx.ValidateAsync(object value, ValidationArgs? args, CancellationToken cancellationToken)
            => await ValidateAsync((T)value!, args, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc/>
        async Task<IValidationResult<T>> IValidator<T>.ValidateAsync(T value, CancellationToken cancellationToken)
            => await ValidateAsync(value, null, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Validate the entity value with specified <see cref="ValidationArgs"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="ValidationContext{TEntity}"/>.</returns>
        Task<ValidationContext<T>> ValidateAsync(T value, ValidationArgs? args, CancellationToken cancellationToken);
    }
}