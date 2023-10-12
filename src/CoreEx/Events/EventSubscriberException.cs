// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Events.Subscribing;
using System;
using System.Net;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents an event subscriber <see cref="Exception"/> that implements <see cref="IExtendedException"/>, that also takes on the characterics of the <see cref="Exception.InnerException"/> where applicable.
    /// </summary>
    /// <remarks>This is intended for internal <i>CoreEx</i> use only to manage <see cref="IEventSubscriber"/> errors/exceptions; throwing or catching directly may result in unintended side-effects.</remarks>
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

        /// <summary>
        /// Indicates that there is an <see cref="Exception.InnerException"/> and that it implements <see cref="IExtendedException"/>.
        /// </summary>
        public bool HasInnerExtendedException => InnerException is IExtendedException;

        /// <summary>
        /// Gets or sets the <see cref="ErrorHandling"/> used when handling the error.
        /// </summary>
        /// <remarks>See <see cref="ErrorHandler.HandleError(EventSubscriberException, ErrorHandling, Microsoft.Extensions.Logging.ILogger, IEventSubscriberInstrumentation?)"/></remarks>
        public ErrorHandling ErrorHandling { get; set; } = ErrorHandling.None;

        /// <summary>
        /// Gets the error type/reason.
        /// </summary>
        /// <remarks>See either the <see cref="Abstractions.ErrorType"/> or <see cref="EventSubscriberExceptionSource"/> for standard values.</remarks>
        public string ErrorType => InnerExtendedException?.ErrorType ?? (ExceptionSource == EventSubscriberExceptionSource.Subscriber ? Abstractions.ErrorType.UnhandledError.ToString() : ExceptionSource.ToString());

        /// <inheritdoc/>
        public int ErrorCode => InnerExtendedException?.ErrorCode ?? (int)ExceptionSource;

        /// <inheritdoc/>
        public HttpStatusCode StatusCode => InnerExtendedException?.StatusCode ?? HttpStatusCode.InternalServerError;

        /// <inheritdoc/>
        public bool IsTransient { get; set; }

        /// <inheritdoc/>
        public bool ShouldBeLogged => false;
    }
}