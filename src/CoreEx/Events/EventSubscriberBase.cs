// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
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
    public abstract class EventSubscriberBase
    {
        private const string _errorText = "Invalid message: body was not provided, contained invalid JSON, or was incorrectly formatted:";

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscriberBase"/> class.
        /// </summary>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        protected EventSubscriberBase(IEventSerializer eventSerializer, ExecutionContext executionContext, SettingsBase settings, ILogger<EventSubscriberBase> logger)
        {
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            EventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        public ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        public SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="IEventSerializer"/>.
        /// </summary>
        public IEventSerializer EventSerializer { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Deserializes the JSON <paramref name="eventData"/> into the specified <see cref="EventData"/>.
        /// </summary>
        /// <param name="eventData">The event <see cref="BinaryData"/> to deserialize.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData{T}"/> where deserialized successfully; otherwise, the corresponding <see cref="ValidationException"/>.</returns>
        protected async Task<(EventData?, ValidationException?)> DeserializeEventAsync(BinaryData eventData, CancellationToken cancellationToken = default)
        {
            try
            {
                var @event = await EventSerializer.DeserializeAsync(eventData, cancellationToken).ConfigureAwait(false)!;
                return (@event, null);
            }
            catch (Exception ex)
            {
                return (null, new ValidationException($"{_errorText} {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Deserializes the JSON <paramref name="eventData"/> into the specified <see cref="EventData{T}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="EventData{T}.Value"/> <see cref="Type"/>.</typeparam>
        /// <param name="eventData">The event <see cref="BinaryData"/> to deserialize.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventData{T}"/> where deserialized successfully; otherwise, the corresponding <see cref="ValidationException"/>.</returns>
        protected async Task<(EventData<T>?, ValidationException?)> DeserializeEventAsync<T>(BinaryData eventData, bool valueIsRequired = true, IValidator<T>? validator = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var @event = await EventSerializer.DeserializeAsync<T>(eventData, cancellationToken).ConfigureAwait(false)!;
                if (valueIsRequired && @event.Value == null)
                    return (null, new ValidationException($"{_errorText} Value is mandatory."));

                if (@event.Value != null && validator != null)
                {
                    var vr = await validator.ValidateAsync(@event.Value, cancellationToken).ConfigureAwait(false);
                    if (vr.HasErrors)
                        return (null, vr.ToValidationException());
                }

                return (@event, null);
            }
            catch (Exception ex)
            {
                return (null, new ValidationException($"{_errorText} {ex.Message}", ex));
            }
        }
    }
}