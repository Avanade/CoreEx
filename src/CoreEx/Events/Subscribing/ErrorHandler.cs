// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using Microsoft.Extensions.Logging;
using System;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Provides the standardized error handling to ensure/enable consistency of behaviour.
    /// </summary>
    /// <remarks>The only reason this class is not sealed is to allow the <see cref="FailFast"/> to be overridden to enable specific functionality to the environment where required.</remarks>
    public class ErrorHandler
    {
        private const string LogFormat = "{Message} [Source: {Source}, Handling: {Handling}]";

        /// <summary>
        /// Determines the <see cref="ErrorHandling"/> based on the <paramref name="errorHandling"/> and <paramref name="exception"/>.
        /// </summary>
        /// <param name="errorHandling">The <see cref="IErrorHandling"/> configuration.</param>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns>The <see cref="ErrorHandling"/>.</returns>
        public static ErrorHandling DetermineErrorHandling(IErrorHandling errorHandling, Exception exception)
            => (exception ?? throw new ArgumentNullException(nameof(exception))) is IExtendedException eex ? DetermineErrorHandling(errorHandling, eex) : errorHandling.UnhandledHandling;

        /// <summary>
        /// Determines the <see cref="ErrorHandling"/> based on the <paramref name="errorHandling"/> and <paramref name="extendedException"/>.
        /// </summary>
        /// <param name="errorHandling">The <see cref="IErrorHandling"/> configuration.</param>
        /// <param name="extendedException">The <see cref="IExtendedException"/>.</param>
        /// <returns>The <see cref="ErrorHandling"/>.</returns>
        public static ErrorHandling DetermineErrorHandling(IErrorHandling errorHandling, IExtendedException extendedException) => (extendedException ?? throw new ArgumentNullException(nameof(extendedException))).ErrorCode switch
        {
            (int)ErrorType.AuthenticationError or (int)ErrorType.AuthorizationError => errorHandling.SecurityHandling,
            (int)ErrorType.BusinessError or (int)ErrorType.ConflictError or (int)ErrorType.DuplicateError or (int)ErrorType.ValidationError => errorHandling.InvalidDataHandling,
            (int)ErrorType.ConcurrencyError => errorHandling.ConcurrencyHandling,
            (int)ErrorType.DataConsistencyError => errorHandling.DataConsistencyHandling,
            (int)ErrorType.NotFoundError => errorHandling.NotFoundHandling,
            (int)ErrorType.TransientError => errorHandling.TransientHandling,
            _ => errorHandling.UnhandledHandling
        };

        /// <summary>
        /// Handles (actions) the <paramref name="eventSubscriberException"/> as defined by the <paramref name="errorHandling"/>.
        /// </summary>
        /// <param name="eventSubscriberException">The <see cref="EventSubscriberException"/>.</param>
        /// <param name="errorHandling">The <see cref="ErrorHandling"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="instrumentation">The optional <see cref="IEventSubscriberInstrumentation"/>.</param>
        /// <remarks>Where the <paramref name="eventSubscriberException"/> is not thrown from within or the existing exception is bubbled, then subsequent processing should be <i>assumed</i> to complete gracefully without continuing.
        /// <para>An <paramref name="errorHandling"/> value of <see cref="ErrorHandling.None"/> will result in a throw as it has already been converted into a <see cref="EventSubscriberException"/>; as such, <see cref="ErrorHandling.None"/> should generally be handled prior to invocation.</para></remarks>
        public void HandleError(EventSubscriberException eventSubscriberException, ErrorHandling errorHandling, ILogger logger, IEventSubscriberInstrumentation? instrumentation)
        {
            // Set the configured error handling for the exception.
            if (errorHandling != ErrorHandling.None)
                eventSubscriberException.ErrorHandling = errorHandling;

            // Where the exception is known then exception and stack trace need not be logged.
            var ex = eventSubscriberException.HasInnerExtendedException ? null : eventSubscriberException;

            // Handle based on error handling configuration.
            switch (errorHandling)
            {
                case ErrorHandling.None:
                case ErrorHandling.Handle:
                    eventSubscriberException.IsTransient = false;
                    instrumentation?.Instrument(errorHandling, eventSubscriberException);
                    throw eventSubscriberException;

                case ErrorHandling.Retry:
                    eventSubscriberException.IsTransient = true;
                    instrumentation?.Instrument(errorHandling, eventSubscriberException);
                    throw eventSubscriberException;

                case ErrorHandling.CompleteAsSilent:
                    instrumentation?.Instrument(errorHandling, eventSubscriberException);
                    break;

                case ErrorHandling.CompleteWithInformation:
                    instrumentation?.Instrument(errorHandling, eventSubscriberException);
                    logger.LogInformation(ex, LogFormat, eventSubscriberException.Message, eventSubscriberException.ExceptionSource, errorHandling.ToString());
                    break;

                case ErrorHandling.CompleteWithWarning:
                    instrumentation?.Instrument(errorHandling, eventSubscriberException);
                    logger.LogWarning(ex, LogFormat, eventSubscriberException.Message, eventSubscriberException.ExceptionSource, errorHandling.ToString());
                    break;

                case ErrorHandling.CompleteWithError:
                    instrumentation?.Instrument(errorHandling, eventSubscriberException);
                    logger.LogError(ex, LogFormat, eventSubscriberException.Message, eventSubscriberException.ExceptionSource, errorHandling.ToString());
                    break;

                case ErrorHandling.CriticalFailFast:
                    eventSubscriberException.IsTransient = false;
                    instrumentation?.Instrument(errorHandling, eventSubscriberException);
                    logger.LogCritical(ex, LogFormat, eventSubscriberException.Message, eventSubscriberException.ExceptionSource, errorHandling.ToString());
                    FailFast(eventSubscriberException);
                    throw eventSubscriberException; // A backup in case FailFast does not function as expected; should _not_ get here!
            }
        }

        /// <summary>
        /// Handles the <see cref="ErrorHandling.CriticalFailFast"/>.
        /// </summary>
        /// <param name="eventSubscriberException">The <see cref="EventSubscriberException"/>.</param>
        /// <remarks>By default invokes <see cref="Environment.FailFast(string, Exception)"/>. This method should be overridden where a different behaviour is required.</remarks>
        protected virtual void FailFast(EventSubscriberException eventSubscriberException) => Environment.FailFast(eventSubscriberException.Message, eventSubscriberException);
    }
}