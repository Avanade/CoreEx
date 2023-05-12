// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Results
{
    public readonly partial struct Result
    {
        /// <summary>
        /// Creates a <see cref="Result"/> that is considered <see cref="Result{T}.IsSuccessful"/>.
        /// </summary>
        /// <returns>The <see cref="Successful"/> value.</returns>
        public static Result Success() => Successful;

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with a default <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccessful"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccessful"/>.</returns>
        public static Result<T> Success<T>() => Result<T>.Successful;

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccessful"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccessful"/>.</returns>
        public static Result<T> Success<T>(T value) => Comparer<T>.Default.Compare(value, default!) == 0 ? Result<T>.Successful : new Result<T>(value);

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
        public static Result Failure(string message) => new(new BusinessException(message));

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Result{T}.Error"/> (see <see cref="Result{T}.IsFailure"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="Result{T}.IsFailure"/>.</returns>
        public static Result<T> Failure<T>(Exception error) => new(error);

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Result{T}.Error"/> (see <see cref="Result{T}.IsFailure"/>) of type <see cref="BusinessException"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="message">The error represented as an <see cref="Exception"/>.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="Result{T}.IsFailure"/>.</returns>
        public static Result<T> Failure<T>(string message) => new(new BusinessException(message));
    }
}