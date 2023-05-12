// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Results
{
    /// <summary>
    /// Represents the outcome of an operation with no value.
    /// </summary>
    public readonly partial struct Result : IResult
    {
        /// <summary>
        /// Gets the <see cref="IsSuccessful"/> default <see cref="Result"/>.
        /// </summary>
        public static Result Successful { get; } = new();

        private readonly Exception? _error = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class that is considered <see cref="IsSuccessful"/>.
        /// </summary>
        public Result() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class with an <see cref="Error"/> (see <see cref="IsFailure"/>).
        /// </summary>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        public Result(Exception error) => _error = error ?? throw new ArgumentNullException(nameof(error));

        /// <inheritdoc/>
        public Exception Error { get => _error ?? throw new InvalidOperationException($"The {nameof(Error)} cannot be accessed as the {nameof(Result)} is in a successful state."); }

        /// <inheritdoc/>
        public bool IsSuccessful => _error is null;

        /// <inheritdoc/>
        public bool IsFailure => _error is not null;

        /// <inheritdoc/>
        IResult IResult.ToFailure(Exception error) => new Result(error);

        /// <summary>
        /// Throws the <see cref="Error"/> where <see cref="IsFailure"/>; otherwise, does nothing.
        /// </summary>
        /// <returns>The <see cref="Result"/>.</returns>
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

        /// <inheritdoc/>
        public override string ToString() => IsSuccessful ? "Successful" : $"Failure: {Error.Message}";

        /// <summary>
        /// Creates a <see cref="Result"/> that is considered <see cref="Result{T}.IsSuccessful"/>.
        /// </summary>
        /// <returns>The <see cref="Successful"/> value.</returns>
        public static Result Success() => Successful;

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>).
        /// </summary>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result Failure(Exception error) => new(error);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="BusinessException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result Failure(LText message) => new(new BusinessException(message));

        /// <summary>
        /// Implicitly converts an <see cref="Exception"/> to a <see cref="Result"/> that is considered <see cref="IsFailure"/>.
        /// </summary>
        /// <param name="error">The underlying error represented as an <see cref="Exception"/>.</param>
        public static implicit operator Result(Exception error) => new(error);
    }
}