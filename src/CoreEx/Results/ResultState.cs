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
        /// Creates a <see cref="Result{T}"/> with a default <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccess"/>.</returns>
        public static Result<T> Ok<T>() => Result<T>.None;

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with a <see cref="Result{T}.Value"/> that is considered <see cref="Result{T}.IsSuccess"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Result{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="Result{T}"/> that is <see cref="Result{T}.IsSuccess"/>.</returns>
        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

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