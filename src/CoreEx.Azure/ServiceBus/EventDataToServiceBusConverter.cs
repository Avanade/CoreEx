// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Invokers;
using CoreEx.Events;
using CoreEx.Mapping.Converters;
using CoreEx.Text.Json;
using System;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Converts an <see cref="EventData"/> to a <see cref="ServiceBusMessage"/>.
    /// </summary>
    /// <remarks>Internally converts an <see cref="EventData"/> to a corresponding <see cref="EventSendData"/> using the <see cref="EventSerializer"/>, then converts to the <see cref="ServiceBusMessage"/> using the <see cref="EventSendDataConverter"/>.</remarks>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> to serialize the <see cref="EventData"/> into a corresponding <see cref="EventSendData.Data"/>.</param>
    /// <param name="valueConverter">The <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="ServiceBusMessage"/>.</param>
    public class EventDataToServiceBusConverter(IEventSerializer? eventSerializer = null, IValueConverter<EventSendData, ServiceBusMessage>? valueConverter = null) : IValueConverter<EventData, ServiceBusMessage>
    {
        /// <summary>
        /// Gets the <see cref="IEventSerializer"/> to serialize the <see cref="EventData"/> into <see cref="BinaryData"/> for the <see cref="EventSendData.Data"/>.
        /// </summary>
        protected IEventSerializer EventSerializer { get; } = eventSerializer ?? ExecutionContext.GetService<IEventSerializer>() ?? new EventDataSerializer();

        /// <summary>
        /// Gets the <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="ServiceBusMessage"/>.
        /// </summary>
        protected IValueConverter<EventSendData, ServiceBusMessage> EventSendDataConverter { get; } = valueConverter ?? ExecutionContext.GetService<IValueConverter<EventSendData, ServiceBusMessage>>() ?? new EventSendDataToServiceBusConverter();

        /// <inheritdoc/>
        public ServiceBusMessage Convert(EventData @event)
        {
            EventSerializer.EventDataFormatter.Format(@event);
            var esd = new EventSendData(@event) { Data = Invoker.RunSync(() => EventSerializer.SerializeAsync(@event)) };
            return EventSendDataConverter.Convert(esd);
        }
    }
}