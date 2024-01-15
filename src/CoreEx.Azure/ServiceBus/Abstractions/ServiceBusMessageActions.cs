// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Messaging.ServiceBus;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebJobs = Microsoft.Azure.WebJobs.ServiceBus;
using Worker = Microsoft.Azure.Functions.Worker;

namespace CoreEx.Azure.ServiceBus.Abstractions
{
    /// <summary>
    /// Provides the <see cref="ServiceBusReceivedMessage"/> message actions that can be performed in an implementation agnostic manner.
    /// </summary>
    /// <remarks>This is intended to encapsulate either a <see cref="WebJobs.ServiceBusMessageActions"/> or <see cref="Worker.ServiceBusMessageActions"/> providing a single means to manage.</remarks>
    public class ServiceBusMessageActions
    {
        private readonly WebJobs.ServiceBusMessageActions? _webJobMessageActions;
        private readonly Worker.ServiceBusMessageActions? _workerMessageActions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageActions"/> class.
        /// </summary>
        /// <param name="webJobMessageActions">The <see cref="WebJobs.ServiceBusMessageActions"/>.</param>
        public ServiceBusMessageActions(WebJobs.ServiceBusMessageActions webJobMessageActions) => _webJobMessageActions = webJobMessageActions.ThrowIfNull();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageActions"/> class.
        /// </summary>
        /// <param name="workerMessageActions">The <see cref="Worker.ServiceBusMessageActions"/>.</param>
        public ServiceBusMessageActions(Worker.ServiceBusMessageActions workerMessageActions) => _workerMessageActions = workerMessageActions.ThrowIfNull();

        /// <inheritdoc cref="ServiceBusReceiver.CompleteMessageAsync(ServiceBusReceivedMessage, CancellationToken)"/>
        public virtual async Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
            => await (_webJobMessageActions?.CompleteMessageAsync(message, cancellationToken)
                ?? _workerMessageActions?.CompleteMessageAsync(message, cancellationToken)
                ?? Task.CompletedTask);

        /// <inheritdoc cref="ServiceBusReceiver.AbandonMessageAsync(ServiceBusReceivedMessage, IDictionary{string, object}, CancellationToken)"/>
        public virtual async Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object>? propertiesToModify = default, CancellationToken cancellationToken = default)
            => await (_webJobMessageActions?.AbandonMessageAsync(message, propertiesToModify, cancellationToken)
                ?? _workerMessageActions?.AbandonMessageAsync(message, propertiesToModify, cancellationToken) 
                ?? Task.CompletedTask);

        /// <inheritdoc cref="ServiceBusReceiver.DeadLetterMessageAsync(ServiceBusReceivedMessage, IDictionary{string, object}, string, string, CancellationToken)"/>
        public virtual async Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, Dictionary<string, object>? propertiesToModify = default, string? deadLetterReason = default, string? deadLetterErrorDescription = default, CancellationToken cancellationToken = default)
            => await (_webJobMessageActions?.DeadLetterMessageAsync(message, propertiesToModify, deadLetterReason, deadLetterErrorDescription, cancellationToken) 
                ?? _workerMessageActions?.DeadLetterMessageAsync(message, propertiesToModify, deadLetterReason, deadLetterErrorDescription, cancellationToken) 
                ?? Task.CompletedTask);

        /// <inheritdoc cref="ServiceBusReceiver.DeferMessageAsync(ServiceBusReceivedMessage, IDictionary{string, object}, CancellationToken)"/>.
        public virtual async Task DeferMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object>? propertiesToModify = default, CancellationToken cancellationToken = default)
            => await (_webJobMessageActions?.DeferMessageAsync(message, propertiesToModify, cancellationToken) 
                ?? _workerMessageActions?.DeferMessageAsync(message, propertiesToModify, cancellationToken) 
                ?? Task.CompletedTask);

        /// <summary>
        /// Implicitly converts a <see cref="WebJobs.ServiceBusMessageActions"/> to a <see cref="ServiceBusMessageActions"/>.
        /// </summary>
        /// <param name="actions">The <see cref="WebJobs.ServiceBusMessageActions"/>.</param>
        public static implicit operator ServiceBusMessageActions(WebJobs.ServiceBusMessageActions actions) => new(actions);

        /// <summary>
        /// Implicitly converts a <see cref="Worker.ServiceBusMessageActions"/> to a <see cref="ServiceBusMessageActions"/>.
        /// </summary>
        /// <param name="actions">The <see cref="Worker.ServiceBusMessageActions"/>.</param>
        public static implicit operator ServiceBusMessageActions(Worker.ServiceBusMessageActions actions) => new(actions);
    }
}