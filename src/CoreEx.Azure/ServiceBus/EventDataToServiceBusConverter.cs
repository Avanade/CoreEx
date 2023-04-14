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
    public class EventDataToServiceBusConverter : IValueConverter<EventData, ServiceBusMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataToServiceBusConverter"/> class.
        /// </summary>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/> to serialize the <see cref="EventData"/> into a corresponding <see cref="EventSendData.Data"/>.</param>
        /// <param name="valueConverter">The <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="ServiceBusMessage"/>.</param>
        public EventDataToServiceBusConverter(IEventSerializer? eventSerializer = null, IValueConverter<EventSendData, ServiceBusMessage>? valueConverter = null)
        {
            EventSerializer = eventSerializer ?? ExecutionContext.GetService<IEventSerializer>() ?? new EventDataSerializer();
            EventSendDataConverter = valueConverter ?? ExecutionContext.GetService<IValueConverter<EventSendData, ServiceBusMessage>>() ?? new EventSendDataToServiceBusConverter();
        }

        /// <summary>
        /// Gets the <see cref="IEventSerializer"/> to serialize the <see cref="EventData"/> into <see cref="BinaryData"/> for the <see cref="EventSendData.Data"/>.
        /// </summary>
        protected IEventSerializer EventSerializer { get; }

        /// <summary>
        /// Gets the <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="ServiceBusMessage"/>.
        /// </summary>
        protected IValueConverter<EventSendData, ServiceBusMessage> EventSendDataConverter { get; }

        /// <inheritdoc/>
        public ServiceBusMessage Convert(EventData @event)
        {
            EventSerializer.EventDataFormatter.Format(@event);
            var esd = new EventSendData(@event) { Data = Invoker.RunSync(() => EventSerializer.SerializeAsync(@event)) };
            return EventSendDataConverter.Convert(esd);
        }
    }
}