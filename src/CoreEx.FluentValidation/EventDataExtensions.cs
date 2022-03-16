// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using FluentValidation;
using System;
using System.Threading.Tasks;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// <c>FluentValidation</c> extension methods for <see cref="EventData{T}"/>.
    /// </summary>
    public static class EventDataExtensions
    {
        /// <summary>
        /// Validates the <see cref="EventData{T}.Value"/> using the specified <typeparamref name="TValidator"/> <see cref="Type"/> asynchronously.
        /// </summary>
        /// <typeparam name="TValue">The underlying value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="event">The <see cref="EventData{T}"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static Task<TValue> ValidateAsync<TValue, TValidator>(this EventData<TValue> @event) where TValidator : AbstractValidator<TValue>, new()
            => ValidateAsync(@event, new TValidator());

        /// <summary>
        /// Validates the <see cref="EventData{T}.Value"/> using the specified <typeparamref name="TValidator"/> <paramref name="validator"/> asynchronously
        /// </summary>
        /// <typeparam name="TValue">The underlying value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="event">The <see cref="EventData{T}"/>.</param>
        /// <param name="validator">The validator instaance.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static async Task<TValue> ValidateAsync<TValue, TValidator>(this EventData<TValue> @event, TValidator validator) where TValidator : AbstractValidator<TValue>
        {
            if (@event.Value != null)
            {
                var vr = await (validator ?? throw new ArgumentNullException(nameof(validator))).ValidateAsync(@event.Value).ConfigureAwait(false);
                if (!vr.IsValid)
                    vr.ThrowValidationException();
            }

            return @event.Value;
        }

        /// <summary>
        /// Validates the <see cref="EventData{T}.Value"/> using the specified <typeparamref name="TValidator"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TValue">The underlying value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="event">The <see cref="EventData{T}"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static TValue Validate<TValue, TValidator>(this EventData<TValue> @event) where TValidator : AbstractValidator<TValue>, new()
            => Validate(@event, new TValidator());

        /// <summary>
        /// Validates the <see cref="EventData{T}.Value"/> using the specified <typeparamref name="TValidator"/> <paramref name="validator"/>.
        /// </summary>
        /// <typeparam name="TValue">The underlying value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="event">The <see cref="EventData{T}"/>.</param>
        /// <param name="validator">The validator instaance.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static TValue Validate<TValue, TValidator>(this EventData<TValue> @event, TValidator validator) where TValidator : AbstractValidator<TValue>
        {
            if (@event.Value != null)
            {
                var vr = (validator ?? throw new ArgumentNullException(nameof(validator))).Validate(@event.Value);
                if (!vr.IsValid)
                    vr.ThrowValidationException();
            }

            return @event.Value;
        }
    }
}