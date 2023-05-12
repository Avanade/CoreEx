// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Localization;
using System;
using System.Collections.Generic;

namespace CoreEx.Results
{
    public readonly partial struct Result
    {
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
        public static Result<T> Success<T>(T value) => Result<T>.Success(value);

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Result{T}.Error"/> (see <see cref="Result{T}.IsFailure"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="error">The error represented as an <see cref="Exception"/>.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="Result{T}.IsFailure"/>.</returns>
        public static Result<T> Failure<T>(Exception error) => Result<T>.Failure(error);

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with an <see cref="Result{T}.Error"/> (see <see cref="Result{T}.IsFailure"/>) of type <see cref="BusinessException"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result{T}"/> that has a state of <see cref="Result{T}.IsFailure"/>.</returns>
        public static Result<T> Failure<T>(LText message) => Result<T>.Failure(message);

        /// <summary>
        /// Gets the <see cref="Successful"/> <see cref="Result"/>; acts as a synonym.
        /// </summary>
        public static Result Ok() => Successful;

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccessful"/>; acts as a synonym for <see cref="Success{T}(T)"/>
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccessful"/>.</returns>
        public static Result<T> Ok<T>(T value) => Success(value);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="BusinessException"/>; acts as a synonym for <see cref="Failure(Exception)"/>
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result Err(Exception error) => Failure(error);

        /// <summary>
        /// Creates a <see cref="Result"/> with an <see cref="Error"/> (see <see cref="IsFailure"/>) of type <see cref="BusinessException"/>; acts as a synonym for <see cref="Failure(LText)"/>
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The <see cref="Result"/> that has a state of <see cref="IsFailure"/>.</returns>
        public static Result Err(LText message) => Failure(message);

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
        public static Result ValidationError(IEnumerable<MessageItem> messages) => new ValidationException(messages);

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
    }
}