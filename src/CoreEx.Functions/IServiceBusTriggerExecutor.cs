// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Events;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using System;
using System.Threading.Tasks;

namespace CoreEx.Functions
{
    /// <summary>
    /// Defines the <see cref="ServiceBusTriggerAttribute"/> executor.
    /// </summary>
    public interface IServiceBusTriggerExecutor : IExecutor
    {
        /// <summary>
        /// Encapsulates the execution of an <see cref="ServiceBusReceivedMessage"/> <paramref name="function"/> converting the <paramref name="message"/> into a corresponding <see cref="EventData{T}"/> for processing.
        /// </summary>
        /// <typeparam name="TValue">The event value <see cref="Type"/>.</typeparam>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="afterReceive">A function that enables the <paramref name="message"/> <see cref="EventData"/> to be processed directly after the message is received and deserialized.</param>
        Task RunAsync<TValue>(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, Func<EventData<TValue>, Task> function, bool valueIsRequired = true, Func<EventData<TValue>, Task>? afterReceive = null);
    }
}