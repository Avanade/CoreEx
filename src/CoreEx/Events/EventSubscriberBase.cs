// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Events.Subscribing;
using CoreEx.Hosting.Work;
using CoreEx.Localization;
using CoreEx.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Provides the base event subscriber capabilities.
    /// </summary>
    /// <param name="eventDataConverter">The <see cref="IEventDataConverter"/>.</param>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
    /// <param name="settings">The <see cref="SettingsBase"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="eventSubscriberInvoker">The <see cref="EventSubscriberInvoker"/>.</param>
    public abstract class EventSubscriberBase(IEventDataConverter eventDataConverter, ExecutionContext executionContext, SettingsBase settings, ILogger<EventSubscriberBase> logger, EventSubscriberInvoker? eventSubscriberInvoker = null) : IErrorHandling
    {
        private static EventSubscriberInvoker? _invoker;
        private ErrorHandler? _errorHandler;

        /// <summary>
        /// Gets the standard message error text.
        /// </summary>
        public static readonly LText MessageErrorText = new($"{typeof(BusinessException).FullName}.{nameof(MessageErrorText)}", "Invalid message; body was not provided, contained invalid JSON, or was incorrectly formatted:");

        /// <summary>
        /// Gets the standard required value error text.
        /// </summary>
        public static readonly LText RequiredErrorText = new($"{typeof(BusinessException).FullName}.{nameof(RequiredErrorText)}", $"{MessageErrorText} Value is required.");

        /// <summary>
        /// Gets the standard null event error text.
        /// </summary>
        public static readonly LText NullEventErrorText = new($"{typeof(BusinessException).FullName}.{nameof(NullEventErrorText)}", $"{MessageErrorText} Event deserialized as null.");

        /// <summary>
        /// Gets the <see cref="IEventDataConverter"/>.
        /// </summary>
        public IEventDataConverter EventDataConverter { get; } = eventDataConverter.ThrowIfNull(nameof(eventDataConverter));

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        public ExecutionContext ExecutionContext { get; } = executionContext.ThrowIfNull(nameof(executionContext));

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        public SettingsBase Settings { get; } = settings.ThrowIfNull(nameof(settings));

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; } = logger.ThrowIfNull(nameof(logger));

        /// <summary>
        /// Gets the <see cref="Subscribing.EventSubscriberInvoker"/>.
        /// </summary>
        public EventSubscriberInvoker EventSubscriberInvoker { get; } = eventSubscriberInvoker ?? (_invoker ??= new EventSubscriberInvoker());

        /// <summary>
        /// Gets or sets the <see cref="ErrorHandling"/> where an <see cref="Exception"/> occurs during <see cref="EventData"/>/<see cref="EventData{T}"/> <see cref="IEventSerializer.DeserializeAsync(BinaryData, CancellationToken)"/>/<see cref="IEventSerializer.DeserializeAsync{T}(BinaryData, CancellationToken)"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.</remarks>
        public ErrorHandling EventDataDeserializationErrorHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.</remarks>
        public ErrorHandling UnhandledHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.</remarks>
        public ErrorHandling SecurityHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.Retry"/>.</remarks>
        public ErrorHandling TransientHandling { get; set; } = ErrorHandling.Retry;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.</remarks>
        public ErrorHandling NotFoundHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.</remarks>
        public ErrorHandling ConcurrencyHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.</remarks>
        public ErrorHandling DataConsistencyHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.</remarks>
        public ErrorHandling InvalidDataHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.HandleBySubscriber"/>.</remarks>
        public ErrorHandling? WorkStateAlreadyFinishedHandling { get; set; } = ErrorHandling.HandleBySubscriber;

        /// <summary>
        /// Gets or sets the optional <see cref="IEventSubscriberInstrumentation"/>.
        /// </summary>
        public IEventSubscriberInstrumentation? Instrumentation { get; set; }

        /// <summary>
        /// Gets or sets the optional <see cref="Hosting.Work.WorkStateOrchestrator"/> to orchestrate and track <see cref="WorkState"/>; this enables the likes of the <i>async request-response</i> pattern.
        /// </summary>
        /// <remarks>The <see cref="WorkState.Id"/> is set from the <see cref="EventDataBase.Id"/> and the <see cref="WorkState.TypeName"/> is set from the <see cref="EventDataBase.Type"/> to enable the underlying operations. Where an
        /// <see cref="EventSubscriberException"/> is thrown the <see cref="WorkState.Status"/> will be set to <see cref="WorkStatus.Indeterminate"/> as it is unknown as to whether the message will be reprocessed.</remarks>
        public WorkStateOrchestrator? WorkStateOrchestrator { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ErrorHandler"/>.
        /// </summary>
        public ErrorHandler ErrorHandler { get => _errorHandler ??= new ErrorHandler(); set => _errorHandler = value; }

        /// <summary>
        /// Performs any checks prior to the processing of the <paramref name="originatingMessage"/>.
        /// </summary>
        /// <param name="identifier">The unique identifier from the originiating message.</param>
        /// <param name="originatingMessage">The originating message.</param>
        /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that processing can continue; otherwise, <c>false</c>.</returns>
        /// <remarks>Where there is a corresponding <see cref="WorkState"/> for the <paramref name="originatingMessage"/> and it is not in a state that is considered valid for processing then processing will not occur.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Future proofing.")]
        protected async Task<bool> OnBeforeProcessingAsync(string identifier, object originatingMessage, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        {
            args.ThrowIfNull(nameof(args));
            if (WorkStateOrchestrator is null)
            {
                args.SetState(this, identifier, null);
                return true;
            }

            var wr = await WorkStateOrchestrator.GetAsync(identifier, cancellationToken).ConfigureAwait(false);
            args.SetState(this, identifier, wr);
            if (wr is null || WorkStatus.InProgress.HasFlag(wr.Status))
                return true;

            if (wr.Status == WorkStatus.Created)
            {
                await WorkStateOrchestrator.StartAsync(identifier, cancellationToken).ConfigureAwait(false);
                return true;
            }

            if (WorkStateAlreadyFinishedHandling is null)
                return true;

            await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(identifier, new EventSubscriberException($"Unable to process message as corresponding work state status is {wr.Status}: {wr.Reason ?? "Unexpected state."}") { ExceptionSource = EventSubscriberExceptionSource.WorkStateAlreadyFinished }, WorkStateAlreadyFinishedHandling.Value, Logger) { Instrumentation = Instrumentation, WorkOrchestrator = null }, cancellationToken).ConfigureAwait(false);
            return false;
        }

        /// <summary>
        /// Deserializes (<see cref="EventDataConverter"/>) the <paramref name="originatingMessage"/> into the specified <see cref="EventData"/> value containg metadata only. 
        /// </summary>
        /// <param name="originatingMessage">The originating message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected Task<EventData> DeserializeEventMetaDataOnlyAsync(object originatingMessage, CancellationToken cancellationToken = default)
            => EventDataConverter.ConvertFromMetadataOnlyAsync(originatingMessage, cancellationToken);

        /// <summary>
        /// Deserializes (<see cref="EventDataConverter"/>) the <paramref name="originatingMessage"/> into the specified <see cref="EventData"/> value. 
        /// </summary>
        /// <param name="identifier">The unique identifier from the originiating message.</param>
        /// <param name="originatingMessage">The originating message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/> where deserialized successfully; otherwise, <c>null</c>.</returns>
        protected async Task<EventData?> DeserializeEventAsync(string identifier, object originatingMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var @event = await EventDataConverter.ConvertFromAsync(originatingMessage, cancellationToken).ConfigureAwait(false);
                if (@event is not null)
                    return @event;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(identifier, new EventSubscriberException($"{MessageErrorText} {ex.Message}", ex) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger) { Instrumentation = Instrumentation, WorkOrchestrator = WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
                return null;
            }

            await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(identifier, new EventSubscriberException(NullEventErrorText) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger) { Instrumentation = Instrumentation, WorkOrchestrator = WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
            return null;
        }

        /// <summary>
        /// Deserializes (<see cref="EventDataConverter"/>) the <paramref name="originatingMessage"/> into the specified <see cref="EventData"/>.
        /// </summary>
        /// <param name="identifier">The unique identifier from the originiating message.</param>
        /// <param name="originatingMessage">The originating message.</param>
        /// <param name="valueType">The optional <see cref="EventData{T}.Value"/> <see cref="Type"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/> or <see cref="EventData{T}"/> (<paramref name="valueType"/>) where deserialized successfully; otherwise, the corresponding <see cref="EventSubscriberException"/>.</returns>
        protected async Task<EventData?> DeserializeEventAsync(string identifier, object originatingMessage, Type? valueType, CancellationToken cancellationToken = default)
        {
            if (valueType is null)
                return await DeserializeEventAsync(identifier, originatingMessage, cancellationToken).ConfigureAwait(false);

            try
            {
                var @event = await EventDataConverter.ConvertFromAsync(originatingMessage, valueType, cancellationToken).ConfigureAwait(false)!;
                if (@event is not null)
                    return @event;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(identifier, new EventSubscriberException($"{MessageErrorText} {ex.Message}", ex) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger) { Instrumentation = Instrumentation, WorkOrchestrator = WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
                return null;
            }

            await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(identifier, new EventSubscriberException(NullEventErrorText) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger) { Instrumentation = Instrumentation, WorkOrchestrator = WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
            return null;
        }

        /// <summary>
        /// Deserializes (<see cref="EventDataConverter"/>) the <paramref name="originatingMessage"/> into the specified <see cref="EventData{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="identifier">The unique identifier from the originiating message.</param>
        /// <param name="originatingMessage">The originating message.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData{T}"/> where deserialized successfully; otherwise, the corresponding <see cref="Exception"/>.</returns>
        /// <remarks>Will result in an <see cref="EventSubscriberException"/> where a deserialization error occurs, or <see cref="ValidationException"/> where <paramref name="valueIsRequired"/> or <paramref name="validator"/> error occurs.</remarks>
        protected async Task<EventData<T>?> DeserializeEventAsync<T>(string identifier, object originatingMessage, bool valueIsRequired = true, IValidator<T>? validator = null, CancellationToken cancellationToken = default)
        {
            // Deserialize the event.
            EventData<T>? @event;
            try
            {
                @event = await EventDataConverter.ConvertFromAsync<T>(originatingMessage, cancellationToken).ConfigureAwait(false)!;
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(identifier, new EventSubscriberException($"{MessageErrorText} {ex.Message}", ex) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger) { Instrumentation = Instrumentation, WorkOrchestrator = WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
                return null;
            }

            if (@event is null)
            {
                await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(identifier, new EventSubscriberException(NullEventErrorText) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger) { Instrumentation = Instrumentation, WorkOrchestrator = WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
                return null;
            }

            // Perform the requested validation where applicable.
            Exception? vex = null;
            if (valueIsRequired && @event.Value == null)
                vex = new ValidationException(RequiredErrorText);
            else if (@event.Value != null && validator != null)
            {
                var vr = await validator.ValidateAsync(@event.Value, cancellationToken).ConfigureAwait(false);
                if (vr.HasErrors)
                    vex = vr.ToException();
            }

            // Exit where the event is considered valid.
            if (vex is null)
                return @event;

            // Handle the validation exception.
            await ErrorHandler.HandleErrorAsync(new ErrorHandlerArgs(identifier, new EventSubscriberException(vex.Message, vex), ErrorHandler.DetermineErrorHandling(this, vex), Logger) { Instrumentation = Instrumentation, WorkOrchestrator = WorkStateOrchestrator }, cancellationToken).ConfigureAwait(false);
            return null;
        }
    }
}