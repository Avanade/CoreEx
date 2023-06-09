﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Events;
using CoreEx.Mapping.Converters;
using System;
using System.Linq;
using System.Net.Mime;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Converts an <see cref="EventSendData"/> to a <see cref="ServiceBusMessage"/>.
    /// </summary>
    public class EventSendDataToServiceBusConverter : IValueConverter<EventSendData, ServiceBusMessage>
    {
        /// <summary>
        /// Gets or sets the <see cref="EventSendData"/> property selection; where a property is selected it will be set as one of the <see cref="ServiceBusMessage"/> properties.
        /// </summary>
        /// <remarks>Defaults to <see cref="EventDataProperty.All"/>.</remarks>
        public EventDataProperty PropertySelection { get; set; } = EventDataProperty.All;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Attributes"/> name for the <see cref="ServiceBusMessage.SessionId"/>.
        /// </summary>
        /// <remarks>Defaults to '<c>_SessionId</c>'.</remarks>
        public string SessionIdAttributeName { get; set; } = $"_{nameof(ServiceBusMessage.SessionId)}";

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Attributes"/> name for the <see cref="ServiceBusMessage.TimeToLive"/>.
        /// </summary>
        /// <remarks>Defaults to '<c>_TimeToLive</c>'.</remarks>
        public string TimeToLiveAttributeName { get; set; } = $"_{nameof(ServiceBusMessage.TimeToLive)}";

        /// <summary>
        /// Indicates whether to use the <see cref="EventDataBase.PartitionKey"/> as the <see cref="ServiceBusMessage.SessionId"/>.
        /// </summary>
        public bool UsePartitionKeyAsSessionId { get; set; } = true;

        /// <inheritdoc/>
        /// <remarks>By default the <see cref="SessionIdAttributeName"/> will be used to update the <see cref="ServiceBusMessage.SessionId"/> from the <see cref="EventDataBase.Attributes"/>, followed by the
        /// <see cref="UsePartitionKeyAsSessionId"/> option, until not <c>null</c>; otherwise, will be left as <c>null</c>.
        /// <para>Similarily, the <see cref="TimeToLiveAttributeName"/> will be used to update the <see cref="ServiceBusMessage.TimeToLive"/> from the <see cref="EventDataBase.Attributes"/>.</para></remarks>
        public ServiceBusMessage Convert(EventSendData @event)
        {
            var message = new ServiceBusMessage(@event.Data)
            {
                MessageId = @event.Id,
                ContentType = MediaTypeNames.Application.Json,
                CorrelationId = @event.CorrelationId ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current.CorrelationId : null),
                Subject = @event.Subject
            };

            if (@event.Action != null && PropertySelection.HasFlag(EventDataProperty.Action))
                message.ApplicationProperties.Add(nameof(EventData.Action), @event.Action);

            if (@event.Source != null && PropertySelection.HasFlag(EventDataProperty.Source))
                message.ApplicationProperties.Add(nameof(EventData.Source), @event.Source.ToString());

            if (@event.Type != null && PropertySelection.HasFlag(EventDataProperty.Type))
                message.ApplicationProperties.Add(nameof(EventData.Type), @event.Type);

            if (@event.TenantId != null && PropertySelection.HasFlag(EventDataProperty.TenantId))
                message.ApplicationProperties.Add(nameof(EventData.TenantId), @event.TenantId);

            if (@event.PartitionKey != null && PropertySelection.HasFlag(EventDataProperty.PartitionKey))
                message.ApplicationProperties.Add(nameof(EventData.PartitionKey), @event.PartitionKey);

            if (@event.ETag != null && PropertySelection.HasFlag(EventDataProperty.ETag))
                message.ApplicationProperties.Add(nameof(EventData.ETag), @event.ETag);

            if (@event.Key != null && PropertySelection.HasFlag(EventDataProperty.Key))
                message.ApplicationProperties.Add(nameof(EventData.Key), @event.Key);

            if (@event.Attributes != null && @event.Attributes.Count > 0 && PropertySelection.HasFlag(EventDataProperty.Attributes))
            {
                // Attrtibutes that start with an underscore are considered internal and will not be sent automatically; i.e. _SessionId and _TimeToLive.
                foreach (var attribute in @event.Attributes.Where(x => !string.IsNullOrEmpty(x.Key) && !x.Key.StartsWith("_")))
                {
                    message.ApplicationProperties.Add(attribute.Key, attribute.Value);
                }
            }

            if (message.SessionId == null)
            {
                if (@event.Attributes != null && @event.Attributes.TryGetValue(SessionIdAttributeName, out var sessionId))
                    message.SessionId = sessionId;
                else
                    message.SessionId = UsePartitionKeyAsSessionId ? @event.PartitionKey : null;
            }

            if (@event.Attributes != null && @event.Attributes.TryGetValue(TimeToLiveAttributeName, out var ttl) && TimeSpan.TryParse(ttl, out var timeToLive))
                message.TimeToLive = timeToLive;

            return message;
        }
    }
}