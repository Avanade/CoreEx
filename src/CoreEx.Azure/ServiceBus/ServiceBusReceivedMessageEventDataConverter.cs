// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Events;
using CoreEx.Text.Json;
using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Converts a <see cref="ServiceBusReceivedMessage"/> to an <see cref="EventData"/> or <see cref="EventData{T}"/>.
    /// </summary>
    /// <param name="eventSerializer">The  <see cref="IEventSerializer"/> to deserialize the <see cref="BinaryData"/> into the corresponding <see cref="EventData"/> or <see cref="EventData{T}"/>.</param>
    public class ServiceBusReceivedMessageEventDataConverter(IEventSerializer? eventSerializer = null) : IEventDataConverter<ServiceBusReceivedMessage>
    {
        /// <summary>
        /// Gets the <see cref="IEventSerializer"/> to deserialize the <see cref="BinaryData"/> into the corresponding <see cref="EventData"/> or <see cref="EventData{T}"/>.
        /// </summary>
        protected IEventSerializer EventSerializer { get; } = eventSerializer ?? ExecutionContext.GetService<IEventSerializer>() ?? new EventDataSerializer();

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">This method is not supported.</exception>
        /// <remarks>This method is not supported; throws a <see cref="NotSupportedException"/>.</remarks>
        public Task<ServiceBusReceivedMessage> ConvertToAsync(EventData @event, CancellationToken cancellationToken) => throw new NotSupportedException($"The {nameof(ServiceBusReceivedMessage)} constructor is internal; therefore, can not be instantiated.");

        /// <inheritdoc/>
        public Task<EventData> ConvertFromMetadataOnlyAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        {
            var @event = new EventData();
            UpdateMetaDataWhereApplicable(message, @event);
            return Task.FromResult(@event);
        }

        /// <inheritdoc/>
        public async Task<EventData> ConvertFromAsync(ServiceBusReceivedMessage message, Type? valueType, CancellationToken cancellationToken)
        {
            EventData @event;
            if (valueType is null)
            {
                if (message.ContentType == MediaTypeNames.Application.Json)
                    @event = await EventSerializer.DeserializeAsync(message.Body, cancellationToken).ConfigureAwait(false);
                else if (message.ContentType == MediaTypeNames.Text.Plain)
                    @event = new EventData { Value = message.Body.ToString() };
                else
                    @event = new EventData();
            }
            else
            {
                if (message.ContentType == MediaTypeNames.Text.Plain && valueType == typeof(string))
                    @event = new EventData<string> { Value = message.Body.ToString() };
                else
                    @event = await EventSerializer.DeserializeAsync(message.Body, valueType, cancellationToken).ConfigureAwait(false)!;
            }

            UpdateMetaDataWhereApplicable(message, @event);
            return @event;
        }

        /// <inheritdoc/>
        public async Task<EventData<T>> ConvertFromAsync<T>(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        {
            EventData<T> @event = await EventSerializer.DeserializeAsync<T>(message.Body, cancellationToken).ConfigureAwait(false);
            UpdateMetaDataWhereApplicable(message, @event);
            return @event;
        }

        /// <summary>
        /// Updates the metadata from the <paramref name="message"/> where the <paramref name="event"/> <see cref="EventDataBase.Id"/> is not <c>null</c>; otherwise, assume already updated.
        /// </summary>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/></param>
        /// <param name="event">The <see cref="EventData"/>.</param>
        private static void UpdateMetaDataWhereApplicable(ServiceBusReceivedMessage message, EventData @event)
        {
            if (@event.Id is not null)
                return;

            @event.Id = message.MessageId;
            @event.CorrelationId = message.CorrelationId;
            @event.Subject = message.Subject;
            @event.PartitionKey = message.SessionId;

            foreach (var p in message.ApplicationProperties)
            {
                switch (p.Key)
                {
                    case nameof(EventData.Action): @event.Action = p.Value?.ToString(); break;
                    case nameof(EventData.Source): @event.Source = p.Value == null ? null : new Uri(p.Value.ToString()!, UriKind.RelativeOrAbsolute); break;
                    case nameof(EventData.Type): @event.Type = p.Value?.ToString(); break;
                    case nameof(EventData.TenantId): @event.Type = p.Value?.ToString(); break;
                    case nameof(EventData.PartitionKey): @event.PartitionKey = p.Value?.ToString(); break;
                    case nameof(EventData.ETag): @event.ETag = p.Value?.ToString(); break;
                    case nameof(EventData.Key): @event.Key = p.Value?.ToString(); break;
                    default: @event.AddAttribute(p.Key, p.Value?.ToString() ?? string.Empty); break;
                }
            }
        }
    }
}