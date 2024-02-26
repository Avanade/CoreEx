// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus
{
    /// <summary>
    /// Represents the set of message actions that can be performed on a <see cref="ServiceBusReceivedMessage"/> and related <see cref="ServiceBusReceiver"/>.
    /// </summary>
    /// <remarks>This is required as the base <see cref="ServiceBusMessageActions"/> contains internal constructor for <see cref="ServiceBusReceiver"/> therefore this is needed to override methods and implement same.</remarks>
    /// <param name="serviceBusReceiver">The <see cref="ServiceBusReceiver"/>.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public class ServiceBusReceiverActions(ServiceBusReceiver serviceBusReceiver) : ServiceBusMessageActions
    {
        private readonly ServiceBusReceiver _serviceBusReceiver = serviceBusReceiver.ThrowIfNull(nameof(serviceBusReceiver));

        /// <inheritdoc/>
        public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = default!, CancellationToken cancellationToken = default)
            => _serviceBusReceiver.AbandonMessageAsync(message, propertiesToModify, cancellationToken);

        /// <inheritdoc/>
        public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
            => _serviceBusReceiver.CompleteMessageAsync(message, cancellationToken);

        /// <inheritdoc/>
        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, Dictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription = default!, CancellationToken cancellationToken = default)
            => _serviceBusReceiver.DeadLetterMessageAsync(message, propertiesToModify, deadLetterReason, deadLetterErrorDescription, cancellationToken);

        /// <inheritdoc/>
        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = default!, CancellationToken cancellationToken = default)
            => _serviceBusReceiver.DeadLetterMessageAsync(message, propertiesToModify, cancellationToken);

        /// <inheritdoc/>
        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, string deadLetterReason, string deadLetterErrorDescription = default!, CancellationToken cancellationToken = default)
            => _serviceBusReceiver.DeadLetterMessageAsync(message, deadLetterReason, deadLetterErrorDescription, cancellationToken);

        /// <inheritdoc/>
        public override Task DeferMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = default!, CancellationToken cancellationToken = default)
            => _serviceBusReceiver.DeferMessageAsync(message, propertiesToModify, cancellationToken);

        /// <inheritdoc/>
        public override Task RenewMessageLockAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
            => base.RenewMessageLockAsync(message, cancellationToken);
    }
}