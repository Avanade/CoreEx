﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Localization;
using CoreEx.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace CoreEx.Validation
{
    /// <summary>
    /// Adds validation-related extension methods.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Gets or sets the format string for the Mandatory error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is required.</c>'.</remarks>
        public static LText MandatoryFormat { get; set; } = new("CoreEx.Validation.MandatoryFormat", "{0} is required.");

        /// <summary>
        /// Gets or sets the default value name.
        /// </summary>
        /// <remarks>Defaults to: '<c>value</c>'.</remarks>
        public static string ValueNameDefault { get; set; } = "value";

        /// <summary>
        /// Gets or sets the default value <see cref="LText"/>.
        /// </summary>
        public static LText ValueTextDefault { get; set; } = "Value";

        /// <summary>
        /// Validates the value asynchronously using the specified <paramref name="validator"/>.
        /// </summary>
        /// <typeparam name="T">The underlying value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="validator">The corresponding <see cref="IValidator{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static async Task<T?> ValidateValueAsync<T>(this T value, IValidator<T> validator, CancellationToken cancellationToken = default) where T : class
        {
            (await (validator ?? throw new ArgumentNullException(nameof(validator))).ValidateAsync(value, cancellationToken).ConfigureAwait(false)).ThrowOnError();
            return value;
        }

        /// <summary>
        /// Validates (requires) that the <paramref name="value"/> is non-default and continues; otherwise, will throw a <see cref="ValidationException"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The value name (defaults to <see cref="ValueNameDefault"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>The value where non-default.</returns>
        /// <exception cref="ValidationException">Thrown where the value is default.</exception>
        public static T? Required<T>(this T? value, string? name = null, LText? text = null)
            => (Comparer<T?>.Default.Compare(value, default!) == 0) ? throw new ValidationException(MessageItem.CreateErrorMessage(name ?? ValueNameDefault, MandatoryFormat, text ?? ((name == null || name == ValueNameDefault) ? ValueTextDefault : name.ToSentenceCase()!))) : value;

        /// <summary>
        /// Validates (requires) that the <paramref name="result"/> <see cref="Result{T}.Value"/> is non-default and executes the <paramref name="success"/> action; otherwise, will return a <see cref="Result{T}"/> with a <see cref="ValidationException"/> (see <see cref="Result{T}.ValidationError(MessageItem)"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="success">The <see cref="Action{T}"/> to invoke when non-default.</param>
        /// <param name="name">The value name (defaults to <see cref="ValueNameDefault"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static Result<T> Required<T>(this Result<T> result, Action<T>? success = null, string? name = null, LText? text = null) => result.Then(v =>
        {
            if (Comparer<T>.Default.Compare(v, default!) == 0)
                return Result<T>.ValidationError(MessageItem.CreateErrorMessage(name ?? ValueNameDefault, MandatoryFormat, text ?? ((name == null || name == ValueNameDefault) ? ValueTextDefault : name.ToSentenceCase()!)));

            success?.Invoke(v);
            return Result<T>.Ok(v);
        });

        /// <summary>
        /// Validates using the <paramref name="validator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="validator">The function to get the <see cref="IValidator{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToValidationException"/>.</remarks>
        public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, Func<IValidator<T>> validator, CancellationToken cancellationToken = default) where T : class
        {
            if (validator == null) throw new ArgumentNullException(nameof(validator));

            return await result.ThenAsync(async v =>
            {
                var vi = validator() ?? throw new InvalidOperationException($"The {nameof(validator)} function must return a non-null instance to perform the requested validation.");
                var vr = await vi.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
                return vr.ToResult<T>();
            });
        }

        /// <summary>
        /// Validates using the <paramref name="validator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToValidationException"/>.</remarks>
        public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, IValidator<T> validator, CancellationToken cancellationToken = default) where T : class
        {
            if (validator == null) throw new ArgumentNullException(nameof(validator));

            return await result.ThenAsync(async v =>
            {
                var vr = await validator.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
                return vr.ToResult<T>();
            });
        }

        /// <summary>
        /// Converts a <paramref name="value"/> to a <see cref="Result{T}"/> where <typeparamref name="T"/> and <typeparamref name="R"/> are the same.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="R">The <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown where <typeparamref name="T"/> and <typeparamref name="R"/> are not the same.</exception>
        public static Result<R> ConvertValueToResult<T, R>(T value)
        {
            if (value is null)
                return Result<R>.None;
            else if (value is R r)
                return Result<R>.Ok(r);
            else
                throw new InvalidOperationException($"Cannot convert {typeof(T).Name} to {typeof(R).Name}.");
        }
    }
}