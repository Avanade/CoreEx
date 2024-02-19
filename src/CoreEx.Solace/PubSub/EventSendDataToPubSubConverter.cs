// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Mapping.Converters;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Linq;
using System.Net.Mime;

namespace CoreEx.Solace.PubSub
{
    /// <summary>
    /// Converts an <see cref="EventSendData"/> to a <see cref="IMessage"/>.
    /// </summary>
    public class EventSendDataToPubSubConverter : IValueConverter<EventSendData, IMessage>
    {
        /// <summary>
        /// Gets or sets the <see cref="EventSendData"/> property selection; where a property is selected it will be set as one of the <see cref="IMessage"/> properties.
        /// </summary>
        /// <remarks>Defaults to <see cref="EventDataProperty.All"/>.</remarks>
        public EventDataProperty PropertySelection { get; set; } = EventDataProperty.All;

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Attributes"/> name for the <see cref="IMessage.UserPropertyMap"/>.
        /// </summary>
        /// <remarks>Defaults to '<c>_SessionId</c>'.</remarks>
        public string SessionIdAttributeName { get; set; } = $"_{nameof(IMessage.ApplicationMessageId)}";

        /// <summary>
        /// Gets or sets the <see cref="EventDataBase.Attributes"/> name for the <see cref="IMessage.TimeToLive"/>.
        /// </summary>
        /// <remarks>Defaults to '<c>_TimeToLive</c>'.</remarks>
        public string TimeToLiveAttributeName { get; set; } = $"_{nameof(IMessage.TimeToLive)}";

        /// <summary>
        /// Indicates whether to use the <see cref="EventDataBase.PartitionKey"/> as the <see cref="IMessage.UserPropertyMap"/>.
        /// </summary>
        public bool UsePartitionKeyAsSessionId { get; set; } = true;

        /// <inheritdoc/>
        /// <remarks>By default the <see cref="SessionIdAttributeName"/> will be used to update the <see cref="IMessage.UserPropertyMap"/> from the <see cref="EventDataBase.Attributes"/>, followed by the
        /// <see cref="UsePartitionKeyAsSessionId"/> option, until not <c>null</c>; otherwise, will be left as <c>null</c>.
        /// <para>Similarily, the <see cref="TimeToLiveAttributeName"/> will be used to update the <see cref="IMessage.TimeToLive"/> from the <see cref="EventDataBase.Attributes"/>.</para></remarks>
        public IMessage Convert(EventSendData @event)
        {
            var message = ContextFactory.Instance.CreateMessage();

            message.BinaryAttachment = @event.Data?.ToArray() ?? [];
            message.ApplicationMessageId = @event.Id;
            message.HttpContentType = MediaTypeNames.Application.Json;
            message.CorrelationId = @event.CorrelationId ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current.CorrelationId : null);

            message.CreateUserPropertyMap();
            message.UserPropertyMap.AddString("Subject", @event.Subject);

            if (@event.Action != null && PropertySelection.HasFlag(EventDataProperty.Action))
                message.UserPropertyMap.AddString(nameof(EventData.Action), @event.Action);

            if (@event.Source != null && PropertySelection.HasFlag(EventDataProperty.Source))
                message.UserPropertyMap.AddString(nameof(EventData.Source), @event.Source.ToString());

            if (@event.Type != null && PropertySelection.HasFlag(EventDataProperty.Type))
                message.UserPropertyMap.AddString(nameof(EventData.Type), @event.Type);

            if (@event.TenantId != null && PropertySelection.HasFlag(EventDataProperty.TenantId))
                message.UserPropertyMap.AddString(nameof(EventData.TenantId), @event.TenantId);

            if (@event.PartitionKey != null && PropertySelection.HasFlag(EventDataProperty.PartitionKey))
                message.UserPropertyMap.AddString(nameof(EventData.PartitionKey), @event.PartitionKey);

            if (@event.ETag != null && PropertySelection.HasFlag(EventDataProperty.ETag))
                message.UserPropertyMap.AddString(nameof(EventData.ETag), @event.ETag);

            if (@event.Key != null && PropertySelection.HasFlag(EventDataProperty.Key))
                message.UserPropertyMap.AddString(nameof(EventData.Key), @event.Key);

            if (@event.Attributes != null && @event.Attributes.Count > 0 && PropertySelection.HasFlag(EventDataProperty.Attributes))
            {
                // Attrtibutes that start with an underscore are considered internal and will not be sent automatically; i.e. _SessionId and _TimeToLive.
                foreach (var attribute in @event.Attributes.Where(x => !string.IsNullOrEmpty(x.Key) && !x.Key.StartsWith('_')))
                {
                    message.UserPropertyMap.AddString(attribute.Key, attribute.Value);
                }
            }

            if (@event.Attributes != null && @event.Attributes.TryGetValue(SessionIdAttributeName, out var sessionId))
                message.UserPropertyMap.AddString("SessionId", sessionId);
            else if (@event.PartitionKey != null)
                message.UserPropertyMap.AddString("SessionId", UsePartitionKeyAsSessionId ? @event.PartitionKey : string.Empty);

            if (@event.Attributes != null && @event.Attributes.TryGetValue(TimeToLiveAttributeName, out var ttl) && TimeSpan.TryParse(ttl, out var timeToLive))
                message.TimeToLive = (long)timeToLive.TotalMilliseconds;

            return message;
        }
    }
}