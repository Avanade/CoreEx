// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Events.Subscribing;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents an event subscriber <see cref="Exception"/> that implements <see cref="IExtendedException"/>, that also takes on the characterics of the <see cref="Exception.InnerException"/> where applicable.
    /// </summary>
    public sealed class EventSubscriberException : Exception, IExtendedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventSendException"/> class with a <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The <see cref="Exception.Message"/>.</param>
        public EventSubscriberException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSendException"/> class with a <paramref name="message"/> and <paramref name="innerException"/>
        /// </summary>
        /// <param name="message">The <see cref="Exception.Message"/>.</param>
        /// <param name="innerException">The <see cref="Exception.InnerException"/>.</param>
        public EventSubscriberException(string message, Exception innerException) : base(message, innerException) => IsTransient = InnerExtendedException?.IsTransient ?? false;

        /// <summary>
        /// Gets the <see cref="EventSubscriberExceptionSource"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="EventSubscriberExceptionSource.Subscriber"/>.</remarks>
        public EventSubscriberExceptionSource ExceptionSource { get; set; } = EventSubscriberExceptionSource.Subscriber;

        /// <summary>
        /// Gets the <see cref="Exception.InnerException"/> as an <see cref="IExtendedException"/> where applicable.
        /// </summary>
        private IExtendedException? InnerExtendedException => InnerException is IExtendedException eex ? eex : null;

        /// <inheritdoc/>
        public string ErrorType => InnerExtendedException?.ErrorType ?? (ExceptionSource == EventSubscriberExceptionSource.Subscriber ? Abstractions.ErrorType.UnhandledError.ToString() : ExceptionSource.ToString());

        /// <inheritdoc/>
        public int ErrorCode => InnerExtendedException?.ErrorCode ?? (int)ExceptionSource;

        /// <inheritdoc/>
        public HttpStatusCode StatusCode => InnerExtendedException?.StatusCode ?? HttpStatusCode.InternalServerError;

        /// <inheritdoc/>
        public bool IsTransient { get; set; }

        /// <inheritdoc/>
        public bool ShouldBeLogged => false;

        /// <inheritdoc/>
        public IActionResult ToResult() => InnerExtendedException?.ToResult() ?? this.ToResult(StatusCode);
    }
}