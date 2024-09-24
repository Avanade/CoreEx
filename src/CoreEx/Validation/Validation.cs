// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Localization;
using CoreEx.Results;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
        /// Validates (requires) that the <paramref name="value"/> is non-default and continues; otherwise, will throw a <see cref="ValidationException"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The value name.</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>The value where non-default.</returns>
        /// <exception cref="ValidationException">Thrown where the value is default.</exception>
        [return: NotNull()]
#if NETSTANDARD2_1
        public static T Required<T>(this T value, string? name = null, LText? text = null)
#else
        public static T Required<T>(this T value, [CallerArgumentExpression(nameof(value))] string? name = null, LText? text = null)
#endif
            => (Comparer<T?>.Default.Compare(value, default!) == 0) ? throw new ValidationException(MessageItem.CreateErrorMessage(name ?? ValueNameDefault, MandatoryFormat, text ?? ((name == null || name == ValueNameDefault) ? ValueTextDefault : name.ToSentenceCase()!))) : value!;

        /// <summary>
        /// Requires (validates) that the <paramref name="value"/> is non-default and continues; otherwise, will return the <paramref name="result"/> with a corresponding <see cref="ValidationException"/>.
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) instance.</param>
        /// <param name="value">The function to return the value to validate is required.</param>
        /// <param name="name">The value name.</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>The resulting <see cref="IResult"/></returns>
        public static TResult Requires<TResult, T>(this TResult result, Func<T> value, string name, LText? text = null) where TResult : IResult
        {
            value.ThrowIfNull(nameof(value));
            name.ThrowIfNullOrEmpty(nameof(name));

            if (result.IsSuccess && Comparer<T>.Default.Compare(value(), default!) == 0)
                return (TResult)result.ToFailure(new ValidationException(MessageItem.CreateErrorMessage(name, MandatoryFormat, text ?? name.ToSentenceCase()!)));

            return result;
        }

#if NETSTANDARD2_1
        /// <summary>
        /// Requires (validates) that the <paramref name="value"/> is non-default and continues; otherwise, will return the <paramref name="result"/> with a corresponding <see cref="ValidationException"/>.
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) instance.</param>
        /// <param name="value">The value to validate is required.</param>
        /// <param name="name">The value name.</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>The resulting <see cref="IResult"/></returns>
        public static TResult Requires<TResult, T>(this TResult result, T value, string name, LText? text = null) where TResult : IResult => result.Requires(() => value, name, text);
#else
        /// <summary>
        /// Requires (validates) that the <paramref name="value"/> is non-default and continues; otherwise, will return the <paramref name="result"/> with a corresponding <see cref="ValidationException"/>.
        /// </summary>
        /// <typeparam name="TResult">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result"/> or <see cref="Result{T}"/> (see <see cref="IResult"/>) instance.</param>
        /// <param name="value">The value to validate is required.</param>
        /// <param name="name">The value name (defaults to <paramref name="value"/> name using the <see cref="CallerArgumentExpressionAttribute"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>The resulting <see cref="IResult"/></returns>
        public static TResult Requires<TResult, T>(this TResult result, T value, [CallerArgumentExpression(nameof(value))] string? name = null, LText? text = null) where TResult : IResult => result.Requires(() => value, name!, text);
#endif

        /// <summary>
        /// Validates using the <paramref name="validator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="validator">The function to get the <see cref="IValidator{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToException"/>.</remarks>
        public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, Func<IValidator<T>> validator, CancellationToken cancellationToken = default) where T : class
        {
            validator.ThrowIfNull(nameof(validator));

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
        /// <param name="validator">The function to get the <see cref="IValidator{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToException"/>.</remarks>
        public static async Task<Result<T>> ValidateAsync<T>(this Task<Result<T>> result, Func<IValidator<T>> validator, CancellationToken cancellationToken = default) where T : class
        {
            validator.ThrowIfNull(nameof(validator));

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
        /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToException"/>.</remarks>
        public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, IValidator<T> validator, CancellationToken cancellationToken = default) where T : class
        {
            validator.ThrowIfNull(nameof(validator));

            return await result.ThenAsync(async v =>
            {
                var vr = await validator.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
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
        /// <remarks>Where the <see cref="IValidationResult"/> <see cref="IValidationResult.HasErrors"/> the corresponding <see cref="IResult.Error"/> will be updated with the <see cref="IValidationResult.ToException"/>.</remarks>
        public static async Task<Result<T>> ValidateAsync<T>(this Task<Result<T>> result, IValidator<T> validator, CancellationToken cancellationToken = default) where T : class
        {
            validator.ThrowIfNull(nameof(validator));

            return await result.ThenAsync(async v =>
            {
                var vr = await validator.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
                return vr.ToResult<T>();
            });
        }

        /// <summary>
        /// Validates using the <paramref name="multiValidator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="multiValidator">The function to get the <see cref="MultiValidator"/> instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> ValidateAsync(this Result result, Func<MultiValidator> multiValidator, CancellationToken cancellationToken = default)
        {
            multiValidator.ThrowIfNull(nameof(multiValidator));

            return await result.ThenAsync(async () =>
            {
                var mv = multiValidator() ?? throw new InvalidOperationException($"The {nameof(multiValidator)} function must return a non-null instance to perform the requested validation.");
                var vr = await mv.ValidateAsync(cancellationToken).ConfigureAwait(false);
                return vr.ToResult();
            });
        }

        /// <summary>
        /// Validates using the <paramref name="multiValidator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        /// <param name="multiValidator">The function to get the <see cref="MultiValidator"/> instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result"/>.</returns>
        public static async Task<Result> ValidateAsync(this Task<Result> result, Func<MultiValidator> multiValidator, CancellationToken cancellationToken = default)
        {
            multiValidator.ThrowIfNull(nameof(multiValidator));

            return await result.ThenAsync(async () =>
            {
                var mv = multiValidator() ?? throw new InvalidOperationException($"The {nameof(multiValidator)} function must return a non-null instance to perform the requested validation.");
                var vr = await mv.ValidateAsync(cancellationToken).ConfigureAwait(false);
                return vr.ToResult();
            });
        }

        /// <summary>
        /// Validates using the <paramref name="multiValidator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="multiValidator">The function to get the <see cref="MultiValidator"/> instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> ValidateAsync<T>(this Result<T> result, Func<T, MultiValidator> multiValidator, CancellationToken cancellationToken = default)
        {
            multiValidator.ThrowIfNull(nameof(multiValidator));

            return await result.ThenAsync(async v =>
            {
                var mv = multiValidator(v) ?? throw new InvalidOperationException($"The {nameof(multiValidator)} function must return a non-null instance to perform the requested validation.");
                var vr = await mv.ValidateAsync(cancellationToken).ConfigureAwait(false);
                return vr.ToResult().Bind<T>();
            });
        }

        /// <summary>
        /// Validates using the <paramref name="multiValidator"/> where the <paramref name="result"/> is <see cref="Result.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="result">The <see cref="Result{T}"/>.</param>
        /// <param name="multiValidator">The function to get the <see cref="MultiValidator"/> instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        public static async Task<Result<T>> ValidateAsync<T>(this Task<Result<T>> result, Func<T, MultiValidator> multiValidator, CancellationToken cancellationToken = default)
        {
            multiValidator.ThrowIfNull(nameof(multiValidator));

            return await result.ThenAsync(async v =>
            {
                var mv = multiValidator(v) ?? throw new InvalidOperationException($"The {nameof(multiValidator)} function must return a non-null instance to perform the requested validation.");
                var vr = await mv.ValidateAsync(cancellationToken).ConfigureAwait(false);
                return vr.ToResult().Bind<T>();
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