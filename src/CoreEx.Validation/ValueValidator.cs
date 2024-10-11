// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables validation for a value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    public class ValueValidator<T>
    {
        private readonly ValueValidatorConfiguration<T> _configuration;
        private readonly ValidationValue<T> _validationValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueValidator{T}"/> class.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The value name (defaults to <see cref="Validation.ValueNameDefault"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        internal ValueValidator(T? value, string? name = null, LText? text = null)
        {
            _configuration = new ValueValidatorConfiguration<T>(string.IsNullOrEmpty(name) ? Validation.ValueNameDefault : name, text);
            _validationValue = new ValidationValue<T>(null, value);
        }

        /// <summary>
        /// Gets the initiating value being validated.
        /// </summary>
        public T? Value { get => _validationValue.Value; }

        /// <summary>
        /// Enables the validator underlying to be further configured.
        /// </summary>
        /// <param name="validator">The <see cref="ValueValidatorConfiguration{T}"/>.</param>
        /// <returns>The <see cref="ValueValidator{T}"/> instance to support fluent-style method-chaining.</returns>
        public ValueValidator<T> Configure(Action<ValueValidatorConfiguration<T>>? validator)
        {
            validator?.Invoke(_configuration);
            return this;
        }

        /// <summary>
        /// Validates the <see cref="Value"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        public Task<ValueValidatorResult<ValidationValue<T>, T>> ValidateAsync(CancellationToken cancellationToken = default)
            => ValidationInvoker.Current.InvokeAsync(this, (_, cancellationToken) => _configuration.ValidateAsync(_validationValue, cancellationToken), cancellationToken);

        /// <summary>
        /// Validates the <see cref="Value"/>.
        /// </summary>
        /// <param name="throwOnError">Indicates whether to automatically throw a <see cref="ValidationException"/> where <see cref="IValidationResult.HasErrors"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        public async Task<ValueValidatorResult<ValidationValue<T>, T>> ValidateAsync(bool throwOnError, CancellationToken cancellationToken = default)
        {
            var vr = await ValidateAsync(cancellationToken).ConfigureAwait(false);
            return throwOnError ? vr.ThrowOnError() : vr;
        }
    }
}