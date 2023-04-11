﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Events.Subscribing;
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
    public abstract class EventSubscriberBase : IErrorHandling
    {
        private static EventSubscriberInvoker? _invoker;

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
        /// Initializes a new instance of the <see cref="EventSubscriberBase"/> class.
        /// </summary>
        /// <param name="eventDataConverter">The <see cref="IEventDataConverter"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="eventSubscriberInvoker">The <see cref="EventSubscriberInvoker"/>.</param>
        protected EventSubscriberBase(IEventDataConverter eventDataConverter, ExecutionContext executionContext, SettingsBase settings, ILogger<EventSubscriberBase> logger, EventSubscriberInvoker? eventSubscriberInvoker = null)
        {
            EventDataConverter = eventDataConverter ?? throw new ArgumentNullException(nameof(eventDataConverter));
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EventSubscriberInvoker = eventSubscriberInvoker ?? (_invoker ??= new EventSubscriberInvoker());
        }

        /// <summary>
        /// Gets the <see cref="IEventDataConverter"/>.
        /// </summary>
        public IEventDataConverter EventDataConverter { get; }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        public ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        public SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Gets the <see cref="Subscribing.EventSubscriberInvoker"/>.
        /// </summary>
        public EventSubscriberInvoker EventSubscriberInvoker { get; }

        /// <summary>
        /// Gets or sets the <see cref="ErrorHandling"/> where an <see cref="Exception"/> occurs during <see cref="EventData"/>/<see cref="EventData{T}"/> <see cref="IEventSerializer.DeserializeAsync(BinaryData, CancellationToken)"/>/<see cref="IEventSerializer.DeserializeAsync{T}(BinaryData, CancellationToken)"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="ErrorHandling.Exception"/>.</remarks>
        public ErrorHandling EventDataDeserializationErrorHandling { get; set; } = ErrorHandling.Exception;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.Exception"/>.</remarks>
        public ErrorHandling UnhandledHandling { get; set; } = ErrorHandling.Exception;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.Exception"/>.</remarks>
        public ErrorHandling SecurityHandling { get; set; } = ErrorHandling.Exception;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.Retry"/>.</remarks>
        public ErrorHandling TransientHandling { get; set; } = ErrorHandling.Retry;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.Exception"/>.</remarks>
        public ErrorHandling NotFoundHandling { get; set; } = ErrorHandling.Exception;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.Exception"/>.</remarks>
        public ErrorHandling ConcurrencyHandling { get; set; } = ErrorHandling.Exception;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.Exception"/>.</remarks>
        public ErrorHandling InvalidDataHandling { get; set; } = ErrorHandling.Exception;

        /// <summary>
        /// Deserializes (<see cref="EventDataConverter"/>) the <paramref name="originatingMessage"/> into the specified <see cref="EventData"/>.
        /// </summary>
        /// <param name="originatingMessage">The originating message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/> where deserialized successfully; otherwise, <c>null</c>.</returns>
        protected async Task<EventData?> DeserializeEventAsync(object originatingMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var @event = await EventDataConverter.ConvertFromAsync(originatingMessage, cancellationToken).ConfigureAwait(false)!;
                if (@event is not null)
                    return @event;
            }
            catch (Exception ex)
            {
                EventSubscriberInvoker.HandleError(new EventSubscriberException($"{MessageErrorText} {ex.Message}", ex) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger);
                return null;
            }

            EventSubscriberInvoker.HandleError(new EventSubscriberException(NullEventErrorText) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger);
            return null;
        }

        /// <summary>
        /// Deserializes (<see cref="EventDataConverter"/>) the <paramref name="originatingMessage"/> into the specified <see cref="EventData"/>.
        /// </summary>
        /// <param name="originatingMessage">The originating message.</param>
        /// <param name="valueType">The optional <see cref="EventData{T}.Value"/> <see cref="Type"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData"/> or <see cref="EventData{T}"/> (<paramref name="valueType"/>) where deserialized successfully; otherwise, the corresponding <see cref="EventSubscriberException"/>.</returns>
        protected async Task<EventData?> DeserializeEventAsync(object originatingMessage, Type? valueType, CancellationToken cancellationToken = default)
        {
            if (valueType is null)
                return await DeserializeEventAsync(originatingMessage, cancellationToken).ConfigureAwait(false);

            try
            {
                var @event = await EventDataConverter.ConvertFromAsync(originatingMessage, valueType, cancellationToken).ConfigureAwait(false)!;
                if (@event is not null)
                    return @event;
            }
            catch (Exception ex)
            {
                EventSubscriberInvoker.HandleError(new EventSubscriberException($"{MessageErrorText} {ex.Message}", ex) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger);
                return null;
            }

            EventSubscriberInvoker.HandleError(new EventSubscriberException(NullEventErrorText) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger);
            return null;
        }

        /// <summary>
        /// Deserializes (<see cref="EventDataConverter"/>) the <paramref name="originatingMessage"/> into the specified <see cref="EventData{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="originatingMessage">The originating message.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData{T}"/> where deserialized successfully; otherwise, the corresponding <see cref="Exception"/>.</returns>
        /// <remarks>Will result in an <see cref="EventSubscriberException"/> where a deserialization error occurs, or <see cref="ValidationException"/> where <paramref name="valueIsRequired"/> or <paramref name="validator"/> error occurs.</remarks>
        protected async Task<EventData<T>?> DeserializeEventAsync<T>(object originatingMessage, bool valueIsRequired = true, IValidator<T>? validator = null, CancellationToken cancellationToken = default)
        {
            // Deserialize the event.
            EventData<T>? @event;
            try
            {
                @event = await EventDataConverter.ConvertFromAsync<T>(originatingMessage, cancellationToken).ConfigureAwait(false)!;
            }
            catch (Exception ex)
            {
                EventSubscriberInvoker.HandleError(new EventSubscriberException($"{MessageErrorText} {ex.Message}", ex) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger);
                return null;
            }

            if (@event is null)
            {
                EventSubscriberInvoker.HandleError(new EventSubscriberException(NullEventErrorText) { ExceptionSource = EventSubscriberExceptionSource.EventDataDeserialization }, EventDataDeserializationErrorHandling, Logger);
                return null;
            }

            // Perform the requested validation where applicable.
            ValidationException? vex = null;
            if (valueIsRequired && @event.Value == null)
                vex = new ValidationException(RequiredErrorText);
            else if (@event.Value != null && validator != null)
            {
                var vr = await validator.ValidateAsync(@event.Value, cancellationToken).ConfigureAwait(false);
                if (vr.HasErrors)
                    vex = vr.ToValidationException();
            }

            // Exit where the event is considered valid.
            if (vex is null)
                return @event;

            // Handle the validation exception.
            if (InvalidDataHandling == ErrorHandling.None)
                throw vex;

            EventSubscriberInvoker.HandleError(new EventSubscriberException(vex.Message, vex), InvalidDataHandling, Logger);
            return null;
        }
    }
}