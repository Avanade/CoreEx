// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents a <see cref="IEventPublisher"/> exception.
    /// </summary>
    public class EventPublisherException : Exception, IExceptionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventPublisherException"/> class.
        /// </summary>
        public EventPublisherException() : this((string)null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public EventPublisherException(string? message) : base(message ?? "An unexpected error has occur during event publishing.") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public EventPublisherException(string? message, Exception innerException) : base(message ?? "An unexpected error has occur during event publishing.", innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class using the specified <paramref name="errors"/>.
        /// </summary>
        /// <param name="errors">The <see cref="EventData"/> errors.</param>
        public EventPublisherException(IEnumerable<EventPublisherDataError> errors) : base(null!) => Errors = errors ?? throw new ArgumentNullException(nameof(errors));

        /// <summary>
        /// Gets the underlying <see cref="EventData"/> errors.
        /// </summary>
        public IEnumerable<EventPublisherDataError>? Errors { get; }

        /// <inheritdoc/>
        public IActionResult ToResult()
        {
            if (Errors == null || !Errors.Any())
                return new BadRequestObjectResult(Message);

            var msd = new ModelStateDictionary();
            foreach (var item in Errors)
            {
                msd.AddModelError($"value[{item.Index}]", item.Message);
            }

            return new BadRequestObjectResult(msd) { StatusCode = (int)HttpStatusCode.InternalServerError };
        }
    }

    /// <summary>
    /// Represents an <see cref="IEventPublisher"/> data (<see cref="EventData"/>) error.
    /// </summary>
    public class EventPublisherDataError
    {
        /// <summary>
        /// Gets or sets the item index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}