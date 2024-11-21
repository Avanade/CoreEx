// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using CoreEx;
using CoreEx.Azure.ServiceBus;
using CoreEx.Events;
using CoreEx.Mapping.Converters;
using Microsoft.Extensions.DependencyInjection;
using System;
using UnitTestEx.Abstractions;

namespace UnitTestEx
{
    /// <summary>
    /// Provides extension methods to the core <see href="https://github.com/Avanade/unittestex"/>.
    /// </summary>
    public static class UnitTestExExtensions
    {
        /// <summary>
        /// Creates a <see cref="ServiceBusReceivedMessage"/> from the <paramref name="event"/> leveraging the registered <see cref="EventDataToServiceBusConverter"/> to perform the underlying conversion.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="event">The <see cref="EventData"/> or <see cref="EventData{T}"/> value.</param>
        /// <returns>The <see cref="ServiceBusReceivedMessage"/>.</returns>
        /// <remarks>This will result in the <see cref="TesterBase.Services"/> from the underlying host being instantiated. If a <b>Services</b>-related error occurs then consider performing a <see cref="TesterBase.ResetHost()"/> after creation to reset.</remarks>
        public static ServiceBusReceivedMessage CreateServiceBusMessage(this TesterBase tester, EventData @event)
        {
            @event.ThrowIfNull(nameof(@event));
            var message = (tester.Services.GetService<EventDataToServiceBusConverter>() ?? new EventDataToServiceBusConverter(tester.Services.GetService<IEventSerializer>(), tester.Services.GetService<IValueConverter<EventSendData, ServiceBusMessage>>())).Convert(@event).GetRawAmqpMessage();
            return tester.CreateServiceBusMessage(message);
        }

        /// <summary>
        /// Creates a <see cref="ServiceBusReceivedMessage"/> from the <paramref name="event"/> leveraging the registered <see cref="EventDataToServiceBusConverter"/> to perform the underlying conversion.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="event">The <see cref="EventData"/> or <see cref="EventData{T}"/> value.</param>
        /// <param name="messageModify">Optional <see cref="AmqpAnnotatedMessage"/> modifier than enables the message to be further configured.</param>
        /// <returns>The <see cref="ServiceBusReceivedMessage"/>.</returns>
        /// <remarks>This will result in the <see cref="TesterBase.Services"/> from the underlying host being instantiated. If a <b>Services</b>-related error occurs then consider performing a <see cref="TesterBase.ResetHost()"/> after creation to reset.</remarks>
        public static ServiceBusReceivedMessage CreateServiceBusMessage(this TesterBase tester, EventData @event, Action<AmqpAnnotatedMessage>? messageModify)
        {
            @event.ThrowIfNull(nameof(@event));
            var message = (tester.Services.GetService<EventDataToServiceBusConverter>() ?? new EventDataToServiceBusConverter(tester.Services.GetService<IEventSerializer>(), tester.Services.GetService<IValueConverter<EventSendData, ServiceBusMessage>>())).Convert(@event).GetRawAmqpMessage();
            return tester.CreateServiceBusMessage(message, messageModify);
        }
    }
}