// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using System;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Provides the base <see cref="IEventSubscriberInstrumentation"/> capabilities; specifically the <see cref="GetInstrumentName(string, ErrorHandling?, Exception?)"/> method.
    /// </summary>
    public abstract class EventSubscriberInstrumentationBase : IEventSubscriberInstrumentation
    {
        private enum SubscriberResult
        {
            Complete,
            Transient,
            Error,
            Critical
        }

        /// <summary>
        /// Gets or sets the instrumentation name format.
        /// </summary>
        /// <remarks>The '<c>{0}</c>' represents the supplied prefix, the '<c>{1}</c>' represents the result ('Complete', 'Transient', 'Error', 'Critical'), and the '<c>{2}</c>' represents the suffix ('Success' or corresponding error suffix).
        /// <para>The following represent possible outcomes: '<c>Prefix.Complete.Success</c>', '<c>Prefix.Complete.NotSubscribed</c>', '<c>Prefix.Transient.AuthenticationError</c>', '<c>Prefix.Error.NotFoundError</c>', etc.</para></remarks>
        public string InstrumentationNameFormat { get; set; } = "{0}.{1}.{2}";

        /// <summary>
        /// Gets or sets the instrumentation result where considered complete. 
        /// </summary>
        public string CompleteResultText { get; set; } = nameof(SubscriberResult.Complete);

        /// <summary>
        /// Gets or sets the instrumentation result where considered an error. 
        /// </summary>
        public string ErrorResultText { get; set; } = nameof(SubscriberResult.Error);

        /// <summary>
        /// Gets or sets the instrumentation result where considered transient (see <see cref="ErrorHandling.TransientRetry"/>).
        /// </summary>
        public string TransientResultText { get; set; } = nameof(SubscriberResult.Transient);

        /// <summary>
        /// Gets or sets the instrumentation result where considered critical (see <see cref="ErrorHandling.CriticalFailFast"/>).
        /// </summary>
        public string CriticalResultText { get; set; } = nameof(SubscriberResult.Critical);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a successful completion.
        /// </summary>
        public string SuccessSuffix { get; set; } = "Success";

        /// <summary>
        /// Gets or sets the instrumentation suffix for an see <see cref="ErrorHandling.None"/> or <see cref="ErrorType.UnhandledError"/>.
        /// </summary>
        public string UnhandledErrorSuffix { get; set; } = nameof(ErrorType.UnhandledError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="EventSubscriberExceptionSource.EventDataDeserialization"/>.
        /// </summary>
        public string EventDataErrorSuffix { get; set; } = $"{nameof(EventSubscriberExceptionSource.EventDataDeserialization)}Error";

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="EventSubscriberExceptionSource.OrchestratorAmbiquousSubscriber"/>.
        /// </summary>
        public string AmbiquousSubscriberErrorSuffix { get; set; } = $"{nameof(EventSubscriberExceptionSource.OrchestratorAmbiquousSubscriber)}Error";

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="EventSubscriberExceptionSource.OrchestratorNotSubscribed"/>.
        /// </summary>
        public string NotSubscribedSuffix { get; set; } = "NotSubscribed";

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.ValidationError"/>.
        /// </summary>
        public string ValidationErrorSuffix { get; set; } = nameof(ErrorType.ValidationError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.BusinessError"/>.
        /// </summary>
        public string BusinessErrorSuffix { get; set; } = nameof(ErrorType.BusinessError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.AuthorizationError"/>.
        /// </summary>
        public string AuthorizationErrorSuffix { get; set; } = nameof(ErrorType.AuthorizationError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.ConcurrencyError"/>.
        /// </summary>
        public string ConcurrencyErrorSuffix { get; set; } = nameof(ErrorType.ConcurrencyError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.NotFoundError"/>.
        /// </summary>
        public string NotFoundErrorSuffix { get; set; } = nameof(ErrorType.NotFoundError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.ConflictError"/>.
        /// </summary>
        public string ConflictErrorSuffix { get; set; } = nameof(ErrorType.ConflictError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.DuplicateError"/>.
        /// </summary>
        public string DuplicateErrorSuffix { get; set; } = nameof(ErrorType.DuplicateError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.AuthenticationError"/>.
        /// </summary>
        public string AuthenticationErrorSuffix { get; set; } = nameof(ErrorType.AuthenticationError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a see <see cref="ErrorType.DataConsistencyError"/>.
        /// </summary>
        public string DataConsistencyErrorSuffix { get; set; } = nameof(ErrorType.DataConsistencyError);

        /// <summary>
        /// Gets or sets the instrumentation suffix for a <see cref="ErrorType.TransientError"/>.
        /// </summary>
        public string TransientErrorSuffix { get; set; } = nameof(ErrorType.TransientError);

        /// <summary>
        /// Indicates whether to always set the suffix to <see cref="SuccessSuffix"/> where the result is a <see cref="CompleteResultText"/>.
        /// </summary>
        /// <remarks>When set this will ignore all the other potential <i>error</i> suffixes where the overall result is considered as success.</remarks>
        public bool AlwaysSuffixSuccessOnResultOfComplete { get; set; } = false;

        /// <inheritdoc/>
        public abstract void Instrument(ErrorHandling? errorHandling = null, Exception? exception = null);

        /// <summary>
        /// Gets the instrumentation name based on the configured <see cref="InstrumentationNameFormat"/>, <paramref name="prefix"/>, <paramref name="errorHandling"/> and <paramref name="exception"/> values.
        /// </summary>
        /// <param name="prefix">The instrumentation prefix.</param>
        /// <param name="errorHandling">The <see cref="ErrorHandling"/> where applicable; otherwise, <c>null</c> which indicates success.</param>
        /// <param name="exception">The <see cref="Exception"/> where applicable.</param>
        /// <returns>The corresponding instrumentation name.</returns>
        /// <remarks>See also <see cref="InstrumentationNameFormat"/>.</remarks>
        protected string GetInstrumentName(string prefix, ErrorHandling? errorHandling, Exception? exception)
        {
            var (result, resultText) = errorHandling switch
            {
                null or ErrorHandling.CompleteAsSilent or ErrorHandling.CompleteWithInformation or ErrorHandling.CompleteWithWarning or ErrorHandling.CompleteWithError => (SubscriberResult.Complete, CompleteResultText),
                ErrorHandling.TransientRetry => (SubscriberResult.Transient, TransientResultText),
                ErrorHandling.CriticalFailFast => (SubscriberResult.Critical, CriticalResultText),
                _ => (SubscriberResult.Error, ErrorResultText)
            };

            var suffix = errorHandling switch
            {
                null => SuccessSuffix,
                _ => result == SubscriberResult.Complete && AlwaysSuffixSuccessOnResultOfComplete ? SuccessSuffix : GetExceptionSuffix(exception)
            };

            return string.Format(InstrumentationNameFormat, string.IsNullOrEmpty(prefix) ? throw new ArgumentNullException(nameof(prefix)) : prefix, resultText, suffix);
        }

        /// <summary>
        /// Gets the suffix from the exception.
        /// </summary>
        private string GetExceptionSuffix(Exception? exception) => (exception is not null && exception is EventSubscriberException esex) ? esex.ExceptionSource switch
        {
            EventSubscriberExceptionSource.OrchestratorNotSubscribed => NotSubscribedSuffix,
            EventSubscriberExceptionSource.EventDataDeserialization => EventDataErrorSuffix,
            EventSubscriberExceptionSource.OrchestratorAmbiquousSubscriber => AmbiquousSubscriberErrorSuffix,
            _ => esex.ErrorCode switch
            {
                (int)ErrorType.ValidationError => ValidationErrorSuffix,
                (int)ErrorType.BusinessError => BusinessErrorSuffix,
                (int)ErrorType.AuthorizationError => AuthorizationErrorSuffix,
                (int)ErrorType.ConcurrencyError => ConcurrencyErrorSuffix,
                (int)ErrorType.NotFoundError => NotFoundErrorSuffix,
                (int)ErrorType.ConflictError => ConflictErrorSuffix,
                (int)ErrorType.DuplicateError => DuplicateErrorSuffix,
                (int)ErrorType.AuthenticationError => AuthenticationErrorSuffix,
                (int)ErrorType.TransientError => TransientErrorSuffix,
                (int)ErrorType.DataConsistencyError => DataConsistencyErrorSuffix,
                _ => UnhandledErrorSuffix
            }
        } : UnhandledErrorSuffix;
    }
}