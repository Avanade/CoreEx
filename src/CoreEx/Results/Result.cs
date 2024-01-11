// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CoreEx.Results
{
    /// <summary>
    /// Represents the outcome of an operation with no value.
    /// </summary>
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToDebuggerString()}")]
    public readonly partial struct Result : IResult, IEquatable<Result>
    {
        /// <summary>
        /// Gets the <see cref="IsSuccess"/> <see cref="Result"/>.
        /// </summary>
        public static Result Success { get; } = new();

        /// <summary>
        /// Gets the <see cref="IsSuccess"/> <see cref="Result"/> <see cref="Task"/>.
        /// </summary>
        public static Task<Result> SuccessTask { get; } = Task.FromResult(Success);

        private readonly Exception? _error = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> that is considered <see cref="IsSuccess"/>.
        /// </summary>
        public Result() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>).
        /// </summary>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        public Result(Exception error) => _error = error ?? throw new ArgumentNullException(nameof(error));

        /// <inheritdoc/>
        object? IResult.Value => null;

        /// <inheritdoc/>
        public Exception Error { get => _error ?? throw new InvalidOperationException($"The {nameof(Error)} cannot be accessed as the {nameof(Result)} is in a successful state."); }

        /// <inheritdoc/>
        public bool IsSuccess => _error is null;

        /// <inheritdoc/>
        public bool IsFailure => _error is not null;

        /// <inheritdoc/>
        IResult IResult.ToFailure(Exception error) => new Result(error);

        /// <summary>
        /// Converts the <see cref="Result"/> to a corresponding <see cref="Result{T}"/> (of <see cref="Type"/> <typeparamref name="T"/>) defaulting to <see cref="Result{T}.None"/> where <see cref="Result.IsSuccess"/>; otherwise, where
        /// <see cref="IsFailure"/> returns a resulting instance with the corresponding <see cref="Error"/>.
        /// </summary>
        /// <typeparam name="T">The (resulting) <see cref="Result{T}"/> <see cref="Type"/>.</typeparam>
        /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
        /// <remarks>This invokes <see cref="CoreExtensions.Bind{T}(Result)"/> internally to perform.</remarks>
        public Result<T> ToResult<T>() => this.Bind<T>();

        /// <summary>
        /// Throws the <see cref="Error"/> where <see cref="IsFailure"/>; otherwise, does nothing.
        /// </summary>
        /// <returns>The <see cref="Result"/> where <see cref="IsSuccess"/> to enable further fluent-style method-chaining.</returns>
        public Result ThrowOnError()
        {
            if (IsFailure)
                ThrowErrorOrAggregateException(Error);

            return this;
        }

        /// <summary>
        /// Throws either the <paramref name="error"/> directly where not previously thrown; otherwise, throws a new <see cref="AggregateException"/> which contains the originating <paramref name="error"/>.
        /// </summary>
        /// <param name="error">The originating <see cref="Exception"/>.</param>
        [DoesNotReturn]
        internal static void ThrowErrorOrAggregateException(Exception error)
        {
            if (error.StackTrace is null)
                throw error;

            throw new AggregateException(error);
        }

        /// <summary>
        /// Executes the specified <paramref name="action"/> and returns <see cref="Success"/>.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>The <see cref="Success"/> <see cref="Result"/>.</returns>
        /// <remarks>This is a helper method to simplify code where an <paramref name="action"/> should be invoked followed immediately by returning a corresponding <see cref="Success"/> to complete/conclude.</remarks>
        public static Result Done(Action action)
        {
            action.ThrowIfNull(nameof(action))();
            return Success;
        }

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccess"/>.</returns>
        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>).
        /// </summary>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result Fail(Exception error) => new(error);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="BusinessException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result Fail(LText? message = null) => new(new BusinessException(message));

        /// <inheritdoc/>
        public override string ToString() => IsSuccess ? "Success." : $"Failure: {Error.Message}";

        /// <summary>
        /// Get the <see cref="string"/> representation of the <see cref="Result"/> for debugging purposes.
        /// </summary>
        private string ToDebuggerString() => IsSuccess ? "Success." : $"Failure: {Error.Message} [{Error.GetType().Name}]";

        /// <summary>
        /// Implicitly converts an <see cref="Exception"/> to a <see cref="Result"/> that is considered <see cref="IsFailure"/>.
        /// </summary>
        /// <param name="error">The underlying error represented as an <see cref="Exception"/>.</param>
        public static implicit operator Result(Exception error) => new(error);

        #region Errors

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result ValidationError(LText? message = default) => new ValidationException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
        /// </summary>
        /// <param name="messages">The <see cref="MessageItem"/> list.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result ValidationError(IEnumerable<MessageItem>? messages) => messages is null ? new ValidationException() : new ValidationException(messages);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ValidationException"/>.
        /// </summary>
        /// <param name="message">The <see cref="MessageItem"/>.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>       
        public static Result ValidationError(MessageItem message) => new ValidationException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ConflictException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        /// <remarks>An example would be where the identifier provided for a Create operation already exists.</remarks>
        public static Result ConflictError(LText? message = default) => new ConflictException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="ConcurrencyException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result ConcurrencyError(LText? message = default) => new ConcurrencyException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="DataConsistencyException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result DataConsistencyError(LText? message = default) => new DataConsistencyException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="DuplicateException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result DuplicateError(LText? message = default) => new DuplicateException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="NotFoundException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result NotFoundError(LText? message = default) => new NotFoundException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="TransientException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result TransientError(LText? message = default) => new TransientException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="AuthenticationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result AuthenticationError(LText? message = default) => new AuthenticationException(message);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="AuthorizationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result AuthorizationError(LText? message = default) => new AuthorizationException(message);

        #endregion

        #region Equality

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Result r && Equals(r);

        /// <inheritdoc/>
        public bool Equals(Result other) => IsSuccess ? other.IsSuccess : (IsFailure == other.IsFailure && Error.GetType() == other.Error.GetType() && Error.ToString() == other.Error.ToString());

        /// <summary>
        /// Indicates whether the current <see cref="Result"/> is equal to another <see cref="Result"/>.
        /// </summary>
        public static bool operator ==(Result left, Result right) => left.Equals(right);

        /// <summary>
        /// Indicates whether the current <see cref="Result"/> is not equal to another <see cref="Result"/>.
        /// </summary>
        public static bool operator !=(Result left, Result right) => !(left == right);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(IsSuccess, IsFailure ? Error.GetHashCode() : 0);

        #endregion
    }
}