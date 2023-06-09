// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents the base for a <see cref="HttpRequestJsonValue"/> and <see cref="HttpRequestJsonValue{T}"/>.
    /// </summary>
    public abstract class HttpRequestJsonValueBase
    {
        /// <summary>
        /// Indicates whether the request value was found to be valid.
        /// </summary>
        public bool IsValid => ValidationException == null;

        /// <summary>
        /// Indicates whether the request value was found to be invalid.
        /// </summary>
        public bool IsInvalid => !IsValid;

        /// <summary>
        /// Gets or sets any corresponding <see cref="Exception"/> related to validation.
        /// </summary>
        /// <remarks>This is typically set as the result of JSON deserialization.</remarks>
        public Exception? ValidationException { get; set; }

        /// <summary>
        /// Converts the <see cref="HttpRequestJsonValueBase"/> into an <see cref="IActionResult"/>.
        /// </summary>
        /// <returns>The corresponding <see cref="IActionResult"/> where <see cref="ValidationException"/> implements <see cref="IExceptionResult"/>.</returns>
        /// <remarks>Where the <see cref="ValidationException"/> does not implement <see cref="IExceptionResult"/> then the <see cref="Exception"/> will be thrown.</remarks>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="IsValid"/>.</exception>
        public IActionResult ToActionResult()
        {
            if (IsValid)
                throw new InvalidOperationException($"The request {nameof(IsValid)} and therefore can not be converted into a {nameof(BadRequestObjectResult)}.");

            if (ValidationException is IExceptionResult er)
                return er.ToResult();

            throw ValidationException!;
        }
    }
}