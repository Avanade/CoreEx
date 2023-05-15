// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System;
using System.Collections.Generic;

namespace CoreEx.Results
{
    /// <summary>
    /// Represents the outcome of an operation with a <see cref="Value"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
    public readonly struct Result<T> : IResultValue<T>
    {
        /// <summary>
        /// Gets the <see cref="IsSuccess"/> <see cref="Result{T}"/> with a default <see cref="Value"/>.
        /// </summary>
        public static Result<T> None { get; } = default;

        private readonly T _value = default!;
        private readonly Exception? _error = default;

        /// <summary>
        /// Initializes a new <see cref="IsSuccess"/> instance of the <see cref="Result{T}"/> class with a <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The <see cref="Result{T}.Value"/>.</param>
        public Result(T value) => _value = value is Exception ? throw new ArgumentException($"A {nameof(Value)} cannot be a {nameof(Type)} of {nameof(Exception)}.", nameof(value)) : value;

        /// <summary>
        /// Initializes a new <see cref="IsFailure"/> instance of the <see cref="Result{T}"/> class with a corresponding <paramref name="error"/>.
        /// </summary>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        public Result(Exception error) => _error = error ?? throw new ArgumentNullException(nameof(error));

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
        /// <returns>The <see cref="Result"/>.</returns>
        public Result<T> ThrowOnError()
        {
            if (IsFailure)
                Result.ThrowErrorOrAggregateException(Error);

            return this;
        }

        /// <inheritdoc/>
        public override string ToString() => IsSuccess ? $"Success: {(Value is null ? "null" : Value)}" : $"Failure: {Error.Message}";

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
        /// Implicitly converts a <see cref="Result{T}"/> to a <see cref="Result"/> loosing the <see cref="Value"/> where <see cref="IsSuccess"/>.
        /// </summary>
        /// <param name="result"></param>
        public static implicit operator Result(Result<T> result) => result.Bind();

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
    }
}