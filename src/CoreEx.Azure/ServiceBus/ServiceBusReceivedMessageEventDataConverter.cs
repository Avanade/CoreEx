﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Events;
using CoreEx.Text.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Converts a <see cref="ServiceBusReceivedMessage"/> to an <see cref="EventData"/> or <see cref="EventData{T}"/>.
    /// </summary>
    public class ServiceBusReceivedMessageEventDataConverter : IEventDataConverter<ServiceBusReceivedMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusReceivedMessageEventDataConverter"/> class.
        /// </summary>
        /// <param name="eventSerializer">The  <see cref="IEventSerializer"/> to deserialize the <see cref="BinaryData"/> into the corresponding <see cref="EventData"/> or <see cref="EventData{T}"/>.</param>
        public ServiceBusReceivedMessageEventDataConverter(IEventSerializer? eventSerializer = null) => EventSerializer = eventSerializer ?? ExecutionContext.GetService<IEventSerializer>() ?? new EventDataSerializer();

        /// <summary>
        /// Gets the <see cref="IEventSerializer"/> to deserialize the <see cref="BinaryData"/> into the corresponding <see cref="EventData"/> or <see cref="EventData{T}"/>.
        /// </summary>
        protected IEventSerializer EventSerializer { get; }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">This method is not supported.</exception>
        /// <remarks>This method is not supported; throws a <see cref="NotSupportedException"/>.</remarks>
        public Task<ServiceBusReceivedMessage> ConvertToAsync(EventData @event, CancellationToken cancellationToken) => throw new NotSupportedException($"The {nameof(ServiceBusReceivedMessage)} constructor is internal; therefore, can not be instantiated.");

        /// <inheritdoc/>
        public async Task<EventData> ConvertFromAsync(ServiceBusReceivedMessage message, Type? valueType, CancellationToken cancellationToken)
        {
            EventData @event;
            if (valueType is null)
                @event = await EventSerializer.DeserializeAsync(message.Body, cancellationToken).ConfigureAwait(false);
            else
                @event = await EventSerializer.DeserializeAsync(message.Body, valueType, cancellationToken).ConfigureAwait(false)!;

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
        /// Update the metadata from the message where the Id is not null; otherwise, assume deserialized from the body and carry on.
        /// </summary>
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
                    case nameof(EventData.Source): @event.Source = p.Value == null ? null : new Uri(p.Value.ToString(), UriKind.RelativeOrAbsolute); break;
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