// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            => (exception.ThrowIfNull(nameof(exception))) is IExtendedException eex ? DetermineErrorHandling(errorHandling, eex) : errorHandling.UnhandledHandling;

        /// <summary>
        /// Determines the <see cref="ErrorHandling"/> based on the <paramref name="errorHandling"/> and <paramref name="extendedException"/>.
        /// </summary>
        /// <param name="errorHandling">The <see cref="IErrorHandling"/> configuration.</param>
        /// <param name="extendedException">The <see cref="IExtendedException"/>.</param>
        /// <returns>The <see cref="ErrorHandling"/>.</returns>
        public static ErrorHandling DetermineErrorHandling(IErrorHandling errorHandling, IExtendedException extendedException) => (extendedException.ThrowIfNull(nameof(extendedException))).ErrorCode switch
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
        /// Handles (actions) the error as defined by the <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The <see cref="ErrorHandlerArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the <see cref="ErrorHandlerArgs.Exception"/> is not thrown from within or the existing exception is bubbled, then subsequent processing should be <i>assumed</i> to complete gracefully without continuing.
        /// <para>An <see name="ErrorHandlerArgs.ErrorHandling"/> value of <see cref="ErrorHandling.HandleByHost"/> will result in a throw as it has already been converted into a <see cref="EventSubscriberException"/>; as such, <see cref="ErrorHandling.HandleByHost"/> should generally be handled prior to invocation.</para></remarks>
        public async Task HandleErrorAsync(ErrorHandlerArgs args, CancellationToken cancellationToken)
        {
            // Set the configured error handling for the exception.
            if (args.ErrorHandling != ErrorHandling.HandleByHost)
                args.Exception.ErrorHandling = args.ErrorHandling;

            // Where the exception is known then exception and stack trace need not be logged.
            var ex = args.Exception.HasInnerExtendedException ? null : args.Exception;

            // Handle based on error handling configuration.
            switch (args.ErrorHandling)
            {
                case ErrorHandling.HandleByHost:
                case ErrorHandling.HandleBySubscriber:
                    args.Exception.IsTransient = false;
                    if (args.WorkOrchestrator is not null)
                        await args.WorkOrchestrator.IndeterminateAsync(args.Identifier!, args.Exception.Message, cancellationToken);

                    args.Instrumentation?.Instrument(args.ErrorHandling, args.Exception);
                    throw args.Exception;

                case ErrorHandling.Retry:
                    args.Exception.IsTransient = true;
                    if (args.WorkOrchestrator is not null)
                        await args.WorkOrchestrator.IndeterminateAsync(args.Identifier!, args.Exception.Message, cancellationToken);
                    
                    args.Instrumentation?.Instrument(args.ErrorHandling, args.Exception);
                    throw args.Exception;

                case ErrorHandling.CompleteAsSilent:
                    if (args.WorkOrchestrator is not null)
                        await args.WorkOrchestrator.FailAsync(args.Identifier!, args.Exception.Message, cancellationToken);

                    args.Instrumentation?.Instrument(args.ErrorHandling, args.Exception);
                    args.Logger.LogDebug(ex, LogFormat, args.Exception.Message, args.Exception.ExceptionSource, args.ErrorHandling.ToString());
                    break;

                case ErrorHandling.CompleteWithInformation:
                    if (args.WorkOrchestrator is not null)
                        await args.WorkOrchestrator.FailAsync(args.Identifier!, args.Exception.Message, cancellationToken);

                    args.Instrumentation?.Instrument(args.ErrorHandling, args.Exception);
                    args.Logger.LogInformation(ex, LogFormat, args.Exception.Message, args.Exception.ExceptionSource, args.ErrorHandling.ToString());
                    break;

                case ErrorHandling.CompleteWithWarning:
                    if (args.WorkOrchestrator is not null)
                        await args.WorkOrchestrator.FailAsync(args.Identifier!, args.Exception.Message, cancellationToken);

                    args.Instrumentation?.Instrument(args.ErrorHandling, args.Exception);
                    args.Logger.LogWarning(ex, LogFormat, args.Exception.Message, args.Exception.ExceptionSource, args.ErrorHandling.ToString());
                    break;

                case ErrorHandling.CompleteWithError:
                    if (args.WorkOrchestrator is not null)
                        await args.WorkOrchestrator.FailAsync(args.Identifier!, args.Exception.Message, cancellationToken);

                    args.Instrumentation?.Instrument(args.ErrorHandling, args.Exception);
                    args.Logger.LogError(ex, LogFormat, args.Exception.Message, args.Exception.ExceptionSource, args.ErrorHandling.ToString());
                    break;

                case ErrorHandling.CriticalFailFast:
                    args.Exception.IsTransient = false;
                    if (args.WorkOrchestrator is not null)
                        await args.WorkOrchestrator.FailAsync(args.Identifier!, args.Exception.Message, cancellationToken);

                    args.Instrumentation?.Instrument(args.ErrorHandling, args.Exception);
                    args.Logger.LogCritical(ex, LogFormat, args.Exception.Message, args.Exception.ExceptionSource, args.ErrorHandling.ToString());
                    FailFast(args.Exception);
                    throw args.Exception; // A backup in case FailFast does not function as expected; should _not_ get here!
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