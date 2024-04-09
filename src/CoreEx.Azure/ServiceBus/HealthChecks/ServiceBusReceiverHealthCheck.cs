// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.ServiceBus.HealthChecks
{
    /// <summary>
    /// Provides a <see cref="ServiceBusReceiver"/> <see cref="IHealthCheck"/> to verify the receiver is accessible by peeking a message.
    /// </summary>
    /// <param name="receiverFactory">The <see cref="ServiceBusReceiver"/> create factory.</param>
    public class ServiceBusReceiverHealthCheck(Func<ServiceBusReceiver> receiverFactory) : IHealthCheck
    {
        private readonly Func<ServiceBusReceiver> _receiverFactory = receiverFactory.ThrowIfNull(nameof(receiverFactory));

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await using var receiver = _receiverFactory() ?? throw new InvalidOperationException("The ServiceBusReceiver factory returned null.");
            var msg = await receiver.PeekMessageAsync(null, cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy(null, new Dictionary<string, object>{ { "message", msg is null ? "none" : new Message { MessageId = msg.MessageId, CorrelationId = msg.CorrelationId, Subject = msg.Subject, SessionId = msg.SessionId, PartitionKey = msg.PartitionKey } } });
        }

        private class Message
        {
            public string? MessageId { get; set; }
            public string? CorrelationId { get; set; }
            public string? Subject { get; set; }
            public string? SessionId { get; set; }
            public string? PartitionKey { get; set; }
        }
    }
}
