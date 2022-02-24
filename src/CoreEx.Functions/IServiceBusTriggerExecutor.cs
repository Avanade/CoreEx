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
        /// Encapsulates the execution enabling standard functionality to be applied.
        /// </summary>
        /// <typeparam name="T">The event value <see cref="Type"/>.</typeparam>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="afterReceive">A function that enables the <paramref name="message"/> <see cref="EventData"/> to be processed directly after the message is received and deserialized.</param>
        Task RunAsync<T>(ServiceBusReceivedMessage message, Func<EventData<T>, Task> function, ServiceBusMessageActions messageActions, bool valueIsRequired = true, Func<EventData<T>, Task>? afterReceive = null);
    }
}