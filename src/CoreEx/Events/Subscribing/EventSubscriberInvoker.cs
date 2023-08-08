// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Invokers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Represents an invoker to ensure <see cref="Exception"/> handling consistency based on the underlying <see cref="IErrorHandling"/> configuration.
    /// </summary>
    /// <remarks>Note that an <see cref="EventSubscriberException"/> is not directly handled; it is simply bubbled out (this is how an exception is managed as previously handled).</remarks>
    public class EventSubscriberInvoker : InvokerBase<IErrorHandling, ILogger>
    {
        private const string LogFormat = "{Message} [Source: {Source}, Handling: {Handling}]";

        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, IErrorHandling invoker, Func<InvokeArgs, TResult> func, ILogger? args) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected async override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, IErrorHandling errorHandling, Func<InvokeArgs, CancellationToken, Task<TResult>> func, ILogger? logger, CancellationToken cancellationToken)
        {
            if (errorHandling is null) throw new ArgumentNullException(nameof(errorHandling));
            if (func is null) throw new ArgumentNullException(nameof(func));
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            // Execute the subscriber logic.
            try
            {
                return await func(invokeArgs, cancellationToken).ConfigureAwait(false);
            }
            catch (EventSubscriberException) { throw; }
            catch (Exception ex) when (ex is IExtendedException eex)
            {
                // Handle the exception based on the subscriber configuration.
                var handling = DetermineErrorHandling(errorHandling, eex);
                if (handling == ErrorHandling.None)
                    throw;

                HandleError(new EventSubscriberException(ex.Message, ex), handling, logger);
            }
            catch (Exception ex) when (errorHandling.UnhandledHandling != ErrorHandling.None)
            {
                HandleError(new EventSubscriberException(ex.Message, ex), errorHandling.UnhandledHandling, logger);
            }

            return default!;
        }

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
        /// <remarks>Where the <paramref name="eventSubscriberException"/> is not thrown from within or the existing exception is bubbled, then subsequent processing should be <i>assumed</i> to complete gracefully without continuing.
        /// <para>An <paramref name="errorHandling"/> value of <see cref="ErrorHandling.None"/> will be treated as <see cref="ErrorHandling.ThrowSubscriberException"/>; <see cref="ErrorHandling.None"/> should generally be handled prior to invocation.</para></remarks>
        public virtual void HandleError(EventSubscriberException eventSubscriberException, ErrorHandling errorHandling, ILogger logger)
        {
            // Where the exception is known then exception and stack trace need not be logged.
            var ex = eventSubscriberException.HasInnerExtendedException ? null : eventSubscriberException;

            // Handle based on error handling configuration.
            switch (errorHandling)
            {
                case ErrorHandling.TransientRetry:
                    eventSubscriberException.IsTransient = true;
                    throw eventSubscriberException;

                case ErrorHandling.CriticalFailFast:
                    logger.LogCritical(ex, LogFormat, eventSubscriberException.Message, eventSubscriberException.ExceptionSource, errorHandling.ToString());
                    FailFast(eventSubscriberException);
                    goto case ErrorHandling.ThrowSubscriberException; // A backup in case FailFast does not function as expected.

                case ErrorHandling.None:
                case ErrorHandling.ThrowSubscriberException:
                    eventSubscriberException.IsTransient = false;
                    throw eventSubscriberException;

                case ErrorHandling.CompleteWithInformation:
                    logger.LogInformation(ex, LogFormat, eventSubscriberException.Message, eventSubscriberException.ExceptionSource, errorHandling.ToString());
                    break;

                case ErrorHandling.CompleteWithWarning:
                    logger.LogWarning(ex, LogFormat, eventSubscriberException.Message, eventSubscriberException.ExceptionSource, errorHandling.ToString());
                    break;

                case ErrorHandling.CompleteWithError:
                    logger.LogError(ex, LogFormat, eventSubscriberException.Message, eventSubscriberException.ExceptionSource, errorHandling.ToString());
                    break;
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