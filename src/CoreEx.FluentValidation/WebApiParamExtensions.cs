// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.WebApis;
using FluentValidation;
using System;
using System.Threading.Tasks;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// <c>FluentValidation</c> extension methods for <see cref="WebApiParam{T}"/>.
    /// </summary>
    public static class WebApiParamExtensions
    {
        /// <summary>
        /// Validates the <see cref="WebApiParam{T}.Value"/> using the specified <typeparamref name="TValidator"/> <see cref="Type"/> asynchronously.
        /// </summary>
        /// <typeparam name="TValue">The underlying value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="wap">The <see cref="WebApiParam{T}"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static Task<TValue> ValidateAsync<TValue, TValidator>(this WebApiParam<TValue> wap) where TValidator : AbstractValidator<TValue>, new()
            => ValidateAsync(wap, new TValidator());

        /// <summary>
        /// Validates the <see cref="WebApiParam{T}.Value"/> using the specified <typeparamref name="TValidator"/> <paramref name="validator"/> asynchronously
        /// </summary>
        /// <typeparam name="TValue">The underlying value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="wap">The <see cref="WebApiParam{T}"/>.</param>
        /// <param name="validator">The validator instaance.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static async Task<TValue> ValidateAsync<TValue, TValidator>(this WebApiParam<TValue> wap, TValidator validator) where TValidator : AbstractValidator<TValue>
        {
            var val = wap.InspectValue((wap ?? throw new ArgumentNullException(nameof(wap))).Value);
            if (val != null)
            {
                var vr = await (validator ?? throw new ArgumentNullException(nameof(validator))).ValidateAsync(val).ConfigureAwait(false);
                if (!vr.IsValid)
                    vr.ThrowValidationException();
            }

            return val!;
        }

        /// <summary>
        /// Validates the <see cref="WebApiParam{T}.Value"/> using the specified <typeparamref name="TValidator"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TValue">The underlying value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="wap">The <see cref="WebApiParam{T}"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static TValue Validate<TValue, TValidator>(this WebApiParam<TValue> wap) where TValidator : AbstractValidator<TValue>, new()
            => Validate(wap, new TValidator());

        /// <summary>
        /// Validates the <see cref="WebApiParam{T}.Value"/> using the specified <typeparamref name="TValidator"/> <paramref name="validator"/>.
        /// </summary>
        /// <typeparam name="TValue">The underlying value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="wap">The <see cref="WebApiParam{T}"/>.</param>
        /// <param name="validator">The validator instaance.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static TValue Validate<TValue, TValidator>(this WebApiParam<TValue> wap, TValidator validator) where TValidator : AbstractValidator<TValue>
        {
            var val = wap.InspectValue((wap ?? throw new ArgumentNullException(nameof(wap))).Value);
            if (val != null)
            {
                var vr = (validator ?? throw new ArgumentNullException(nameof(validator))).Validate(val);
                if (!vr.IsValid)
                    vr.ThrowValidationException();
            }

            return val!;
        }
    }
}