﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Validation;
using System;
using System.Threading;
using System.Threading.Tasks;
using FV = FluentValidation;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// Represents an <see cref="FV.IValidator{T}"/> wrapper to enable <i>CoreEx</i> <see cref="IValidator{T}"/> interoperability.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="fluentValidator">The <see cref="FV.IValidator{T}"/> to wrap.</param>
    public sealed class ValidatorWrapper<T>(FV.IValidator<T> fluentValidator) : IValidator<T>
    {
        /// <summary>
        /// Gets the underlying <see cref="FV.IValidator{T}"/> that is being wrapped.
        /// </summary>
        public FV.IValidator<T> Validator { get; } = fluentValidator.ThrowIfNull(nameof(fluentValidator));

        /// <inheritdoc/>
        public async Task<IValidationResult<T>> ValidateAsync(T value, CancellationToken cancellationToken = default)
            => value == null ? new ValidationResultWrapper<T>() : new ValidationResultWrapper<T>(await Validator.ValidateAsync(value, cancellationToken).ConfigureAwait(false), value);
    }
}