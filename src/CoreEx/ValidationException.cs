// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Net;

namespace CoreEx
{
    /// <summary>
    /// Represents a <b>Validation</b> exception.
    /// </summary>
    /// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A data validation error occurred.</i></remarks>
    public class ValidationException : Exception, IExtendedException
    {
        private const string _message = "A data validation error occurred.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ValidationException(string? message) : base(message ?? _message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public ValidationException(string? message, Exception innerException) : base(message ?? _message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class using the specified <paramref name="modelStateDictionary"/> and <paramref name="message"/>.
        /// </summary>
        /// <param name="modelStateDictionary">The <see cref="ModelStateDictionary"/> that contains the validation errors.</param>
        /// <param name="message">The error message.</param>
        public ValidationException(ModelStateDictionary modelStateDictionary, string? message = null) : this(message)
            => ModelStateDictionary = modelStateDictionary ?? throw new ArgumentNullException(nameof(modelStateDictionary));

        /// <summary>
        /// Gets the <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/>.
        /// </summary>
        public ModelStateDictionary? ModelStateDictionary { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.ValidationError"/> value.</returns>
        public ErrorType ErrorType => ErrorType.ValidationError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.BadRequest"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><c>false</c>; is not considered transient.</returns>
        public bool IsTransient => false;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ShouldExceptionBeLogged"/> value.</returns>
        public bool ShouldBeLogged => ShouldExceptionBeLogged;

        /// <inheritdoc/>
        public IActionResult ToResult() => ModelStateDictionary == null || ModelStateDictionary.IsValid ? new BadRequestObjectResult(Message) : new BadRequestObjectResult(ModelStateDictionary);
    }
}