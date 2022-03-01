// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Events;
using FluentValidation;
using Microsoft.Azure.WebJobs.ServiceBus;
using System;
using System.Threading.Tasks;

namespace CoreEx.Functions.FluentValidation
{
    /// <summary>
    /// Extension methods for <see cref="IServiceBusTriggerExecutor"/>.
    /// </summary>
    public static class ServiceBusTriggerExecutorExtensions
    {
        /// <summary>
        /// Encapsulates the execution of an <see cref="ServiceBusReceivedMessage"/> <paramref name="function"/> converting the <paramref name="message"/> into a corresponding validated <see cref="EventData{T}"/> for processing.
        /// </summary>
        /// <typeparam name="TValue">The event value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TValue"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IServiceBusTriggerExecutor"/>.</param>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="afterReceive">A function that enables the <paramref name="message"/> <see cref="EventData"/> to be processed directly after the message is received and deserialized.</param>
        public static Task RunAsync<TValue, TValidator>(this IServiceBusTriggerExecutor executor, ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, Func<EventData<TValue>, Task> function, bool valueIsRequired = true, Func<EventData<TValue>, Task>? afterReceive = null)
            where TValidator : AbstractValidator<TValue>, new()
            => RunAsync(executor, message, messageActions, function, new TValidator(), valueIsRequired, afterReceive);

        /// <summary>
        /// Encapsulates the execution of an <see cref="ServiceBusReceivedMessage"/> <paramref name="function"/> converting the <paramref name="message"/> into a corresponding validated <see cref="EventData{T}"/> for processing.
        /// </summary>
        /// <typeparam name="TValue">The event value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The <typeparamref name="TValue"/> validator <see cref="Type"/>.</typeparam>
        /// <param name="executor">The <see cref="IServiceBusTriggerExecutor"/>.</param>
        /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
        /// <param name="messageActions">The <see cref="ServiceBusMessageActions"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="validator">The  <see cref="AbstractValidator{TValue}"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="afterReceive">A function that enables the <paramref name="message"/> <see cref="EventData"/> to be processed directly after the message is received and deserialized.</param>
        public static async Task RunAsync<TValue, TValidator>(this IServiceBusTriggerExecutor executor, ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, Func<EventData<TValue>, Task> function, TValidator validator, bool valueIsRequired = true, Func<EventData<TValue>, Task>? afterReceive = null)
            where TValidator : AbstractValidator<TValue>
        {
            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            await executor.RunAsync(message, messageActions, function, valueIsRequired, async ed =>
            {
                var fvr = (validator ?? throw new ArgumentNullException(nameof(validator))).Validate(ed.Value);
                fvr.ThrowValidationException();
                if (afterReceive != null)
                    await afterReceive(ed).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }
}