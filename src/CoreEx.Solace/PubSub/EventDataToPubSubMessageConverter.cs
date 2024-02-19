// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;
using CoreEx.Events;
using CoreEx.Mapping.Converters;
using CoreEx.Text.Json;
using System;
using SolaceSystems.Solclient.Messaging;

namespace CoreEx.Solace.PubSub
{
    /// <summary>
    /// Converts an <see cref="EventData"/> to a <see cref="IMessage"/>.
    /// </summary>
    /// <remarks>Internally converts an <see cref="EventData"/> to a corresponding <see cref="EventSendData"/> using the <see cref="EventSerializer"/>, then converts to the <see cref="IMessage"/> using the <see cref="EventSendDataConverter"/>.</remarks>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> to serialize the <see cref="EventData"/> into a corresponding <see cref="EventSendData.Data"/>.</param>
    /// <param name="valueConverter">The <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="IMessage"/>.</param>
    public class EventDataToPubSubMessageConverter(IEventSerializer? eventSerializer = null, IValueConverter<EventSendData, IMessage>? valueConverter = null) : IValueConverter<EventData, IMessage>
    {
        /// <summary>
        /// Gets the <see cref="IEventSerializer"/> to serialize the <see cref="EventData"/> into <see cref="BinaryData"/> for the <see cref="EventSendData.Data"/>.
        /// </summary>
        protected IEventSerializer EventSerializer { get; } = eventSerializer ?? ExecutionContext.GetService<IEventSerializer>() ?? new EventDataSerializer();

        /// <summary>
        /// Gets the <see cref="IValueConverter{TSource, TDestination}"/> to convert an <see cref="EventSendData"/> to a corresponding <see cref="IMessage"/>.
        /// </summary>
        protected IValueConverter<EventSendData, IMessage> EventSendDataConverter { get; } = valueConverter ?? ExecutionContext.GetService<IValueConverter<EventSendData, IMessage>>() ?? new EventSendDataToPubSubConverter();

        /// <inheritdoc/>
        public IMessage Convert(EventData @event)
        {
            EventSerializer.EventDataFormatter.Format(@event);
            var esd = new EventSendData(@event) { Data = Invoker.RunSync(() => EventSerializer.SerializeAsync(@event)) };
            return EventSendDataConverter.Convert(esd);
        }
    }
}