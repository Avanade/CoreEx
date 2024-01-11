// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreEx.Results
{
    /// <summary>
    /// Represents the outcome of an operation with a <see cref="Value"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToDebuggerString()}")]
    public readonly struct Result<T> : IResult<T>, IEquatable<Result<T>>
    {
        /// <summary>
        /// Gets the <see cref="IsSuccess"/> <see cref="Result{T}"/> with a default <see cref="Value"/>.
        /// </summary>
        public static Result<T> None { get; } = default;

        private readonly T _value = default!;
        private readonly Exception? _error = default;

        /// <summary>
        /// Initializes a new <see cref="IsSuccess"/> instance of the <see cref="Result{T}"/> with a <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The <see cref="Result{T}.Value"/>.</param>
        public Result(T value) => _value = value;

        /// <summary>
        /// Initializes a new <see cref="IsFailure"/> instance of the <see cref="Result{T}"/> with a corresponding <paramref name="error"/>.
        /// </summary>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        public Result(Exception error) => _error = error ?? throw new ArgumentNullException(nameof(error));

        /// <inheritdoc/>
        object? IResult.Value => Value;

        /// <inheritdoc/>
        public T Value
        {
            get
            {
                if (IsSuccess)
                    return _value;

                Result.ThrowErrorOrAggregateException(Error);
                return default;
            }
        }

        /// <inheritdoc/>
        public Exception Error { get => _error ?? throw new InvalidOperationException($"The {nameof(Error)} cannot be accessed as the {nameof(Result)} is in a successful state."); }

        /// <inheritdoc/>
        public bool IsSuccess => _error is null;

        /// <inheritdoc/>
        public bool IsFailure => _error is not null;

        /// <inheritdoc/>
        IResult IResult.ToFailure(Exception error) => new Result<T>(error);

        /// <summary>
        /// Throws the <see cref="Error"/> where <see cref="IsFailure"/>; otherwise, does nothing.
        /// </summary>
        /// <returns>The <see cref="Result{T}"/> where <see cref="IsSuccess"/> to enable further fluent-style method-chaining.</returns>
        public Result<T> ThrowOnError()
        {
            if (IsFailure)
                Result.ThrowErrorOrAggregateException(Error);

            return this;
        }

        /// <inheritdoc/>
        public override string ToString() => IsSuccess ? $"Success: {(Value is null ? "null" : Value)}" : $"Failure: {Error.Message}";

        /// <summary>
        /// Get the <see cref="string"/> representation of the <see cref="Result"/> for debugging purposes.
        /// </summary>
        private string ToDebuggerString() => IsSuccess ? $"Success: {(Value is null ? "null" : Value)}" : $"Failure: {Error.Message} [{Error.GetType().Name}]";

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with a default <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccess"/>.
        /// </summary>
        /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccess"/> (see <see cref="Result{T}.None"/>).</returns>
        public static Result<T> Ok() => Result<T>.None;

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccess"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccess"/>.</returns>
        public static Result<T> Ok(T value) => value is null || (value is IComparable && Comparer<T>.Default.Compare(value, default!) == 0) ? Result<T>.None : new Result<T>(value);

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Result{T}.Error"/> (see <see cref="Result{T}.IsFailure"/>).
        /// </summary>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="Result{T}.IsFailure"/>.</returns>
        public static Result<T> Fail(Exception error) => new(error);

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Result{T}.Error"/> (see <see cref="Result{T}.IsFailure"/>) of type <see cref="BusinessException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="Result{T}.IsFailure"/>.</returns>
        public static Result<T> Fail(LText message) => new(new BusinessException(message));

        /// <summary>
        /// Requires (validates) that the <see cref="Value"/> is non-default; otherwise, will result in a <see cref="ValidationError(MessageItem)"/>.
        /// </summary>
        /// <param name="name">The value name (defaults to <see cref="Validation.Validation.ValueNameDefault"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>The resulting <see cref="Result{T}"/>.</returns>
        /// <remarks>The format of the error messages is defined by <see cref="Validation.Validation.MandatoryFormat"/>.</remarks>
        public Result<T> Required(string? name = null, LText? text = null)
        {
            if (IsSuccess && Comparer<T>.Default.Compare(Value, default!) == 0)
                return ValidationError(MessageItem.CreateErrorMessage(name ?? Validation.Validation.ValueNameDefault, Validation.Validation.MandatoryFormat, text ?? ((name == null || name == Validation.Validation.ValueNameDefault) ? Validation.Validation.ValueTextDefault : name.ToSentenceCase()!)));

            return this;
        }

        /// <summary>
        /// Implicitly converts an <see cref="Exception"/> to a <see cref="Result"/> that is considered <see cref="IsFailure"/>.
        /// </summary>
        /// <param name="error">The underlying error represented as an <see cref="Exception"/>.</param>
        public static implicit operator Result<T>(Exception error) => new(error);

        /// <summary>
        /// Implicitly converts a <see cref="Result"/> to a <see cref="Result{T}"/> defaulting the <see cref="Value"/> where <see cref="IsSuccess"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        public static implicit operator Result<T>(Result result) => result.Bind(() => new Result<T>());

        /// <summary>
        /// Explicitly converts a <see cref="Result{T}"/> to a <see cref="Result"/> losing the <see cref="Value"/> where <see cref="IsSuccess"/>.
        /// </summary>
        /// <param name="result"></param>
        public static explicit operator Result(Result<T> result) => result.Bind();

        /// <summary>
        /// Implicityly converts a <see cref="Value"/> to a <see cref="Result{T}"/> as <see cref="IsSuccess"/>.
        /// </summary>
        /// <param name="value">The underlying value.</param>
        public static implicit operator Result<T>(T value) => Result<T>.Ok(value);

        /// <summary>
        /// Implicitly converts a <see cref="Result{T}"/> to a <see cref="Value"/> where <see cref="IsSuccess"/>.
        /// </summary>
        /// <param name="result"></param>
        public static implicit operator T(Result<T> result) => result.Value;

        #region Errors

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> ValidationError(LText? message = default) => Result<T>.Fail(new ValidationException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
        /// </summary>
        /// <param name="messages">The <see cref="MessageItem"/> list.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> ValidationError(IEnumerable<MessageItem>? messages) => Result<T>.Fail(new ValidationException(messages!));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
        /// </summary>
        /// <param name="message">The <see cref="MessageItem"/>.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>       
        public static Result<T> ValidationError(MessageItem message) => Result<T>.Fail(new ValidationException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ConflictException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        /// <remarks>An example would be where the identifier provided for a Create operation already exists.</remarks>
        public static Result<T> ConflictError(LText? message = default) => Result<T>.Fail(new ConflictException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ConcurrencyException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> ConcurrencyError(LText? message = default) => Result<T>.Fail(new ConcurrencyException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="DataConsistencyException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> DataConsistencyError(LText? message = default) => Result<T>.Fail(new DataConsistencyException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="DuplicateException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> DuplicateError(LText? message = default) => Result<T>.Fail(new DuplicateException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="NotFoundException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> NotFoundError(LText? message = default) => Result<T>.Fail(new NotFoundException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="TransientException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> TransientError(LText? message = default) => Result<T>.Fail(new TransientException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="AuthenticationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> AuthenticationError(LText? message = default) => Result<T>.Fail(new AuthenticationException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="AuthorizationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result<T> AuthorizationError(LText? message = default) => Result<T>.Fail(new AuthorizationException(message));

        #endregion

        #region Equality

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Result<T> r && Equals(r);

        /// <inheritdoc/>
        public bool Equals(Result<T> other) => IsSuccess ? (other.IsSuccess && EqualityComparer<T>.Default.Equals(Value, other.Value)) : (IsFailure == other.IsFailure && Error.GetType() == other.Error.GetType() && Error.ToString() == other.Error.ToString());

        /// <summary>
        /// Indicates whether the current <see cref="Result"/> is equal to another <see cref="Result"/>.
        /// </summary>
        public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

        /// <summary>
        /// Indicates whether the current <see cref="Result"/> is not equal to another <see cref="Result"/>.
        /// </summary>
        public static bool operator !=(Result<T> left, Result<T> right) => !(left == right);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(IsSuccess, IsSuccess ? (Value?.GetHashCode() ?? 0) : 0, IsFailure ? Error.GetHashCode() : 0);

        #endregion
    }
}