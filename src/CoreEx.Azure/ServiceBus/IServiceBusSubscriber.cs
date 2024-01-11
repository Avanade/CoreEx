// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using CoreEx.Abstractions;
using System;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Defines the standardized Azure Service Bus subscriber properties.
    /// </summary>
    public interface IServiceBusSubscriber
    {
        /// <summary>
        /// Indicates when <c>true</c> that an <see cref="Abstractions.ServiceBusMessageActions.AbandonMessageAsync"/> should be issued when a <see cref="IExtendedException.IsTransient"/> <see cref="Exception"/> is encounterd; otherwise, when <c>false</c> allow the <see cref="Exception"/> to bubble up the stack.
        /// </summary>
        bool AbandonOnTransient { get; set; }

        /// <summary>
        /// Gets or sets the optional maximum delivery count before a corresponding <see cref="Abstractions.ServiceBusMessageActions.DeadLetterMessageAsync"/> will be issued.
        /// </summary>
        /// <remarks>Where <c>null</c> this indicates that this checking is solely the responsibility of the Azure Service Bus infrastructure. This value can not exceed the corresponding Azure Service Bus configuration setting as that takes precedence.</remarks>
        int? MaxDeliveryCount { get; set; }

        /// <summary>
        /// Get or sets the optional retry <see cref="TimeSpan"/> to define a multiplicative delay where an <see cref="IExtendedException.IsTransient"/> <see cref="Exception"/> is encounterd.
        /// </summary>
        /// <remarks>The <see cref="ServiceBusReceivedMessage.DeliveryCount"/> is multiplied by this value to achieve the final multiplicative delay value.
        /// <para>This is performed after an unsuccessful transient processing attempt effectively continuing to lock the the <see cref="ServiceBusReceivedMessage"/> for the duration of the delay before finally handling as defined by <see cref="AbandonOnTransient"/>.</para></remarks>
        TimeSpan? RetryDelay { get; set; }

        /// <summary>
        /// Gets or sets the optional maximum retry <see cref="TimeSpan"/> that represents the upper bounds of the multiplicative <see cref="RetryDelay"/>.
        /// </summary>
        /// <remarks>Where a value is specified and the corresponding <see cref="RetryDelay"/> is <c>null</c> this value will be used only achieving a fixed delay value.</remarks>
        TimeSpan? MaxRetryDelay { get; set; }
    }
}