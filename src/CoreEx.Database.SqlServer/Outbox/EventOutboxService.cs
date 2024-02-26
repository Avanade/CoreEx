// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database.SqlServer.Outbox
{
    /// <summary>
    /// Provides the <see cref="EventOutboxDequeueBase"/> dequeue and publish self-service capabilities.
    /// </summary>
    /// <remarks>This will instantiate an <see cref="EventOutboxDequeueBase"/> using the underlying <see cref="ServiceProvider"/> and invoke <see cref="EventOutboxDequeueBase.DequeueAndSendAsync(int, string?, string?, CancellationToken)"/>.</remarks>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="settings">The <see cref="SettingsBase"/>.</param>
    /// <param name="partitionKey">The optional partition key.</param>
    /// <param name="destination">The optional destination name (i.e. queue or topic).</param>
    public class EventOutboxService(IServiceProvider serviceProvider, ILogger<EventOutboxService> logger, SettingsBase? settings = null, string? partitionKey = null, string? destination = null) : ServiceBase(serviceProvider, logger, settings)
    {
        private string? _name;
        private int? _maxIterations;
        private int? _maxDequeueSize;

        /// <summary>
        /// Gets or sets the configuration name for <see cref="MaxDequeueSize"/>. Defaults to '<c>MaxDequeueSize</c>'.
        /// </summary>
        public string MaxDequeueSizeName { get; set; } = "MaxDequeueSize";

        /// <summary>
        /// Gets the optional partition key.
        /// </summary>
        public string? PartitionKey { get; } = partitionKey;

        /// <summary>
        /// Gets the optional destination name (i.e. queue or topic).
        /// </summary>
        public string? Destination { get; } = destination;

        /// <summary>
        /// Gets the service name (used for the likes of configuration and logging).
        /// </summary>
        public override string ServiceName => _name ??= $"{GetType().Name}{(PartitionKey == null ? "" : $".{PartitionKey}")}";

        /// <inheritdoc/>
        /// <remarks>Will default to <see cref="SettingsBase"/> configuration, a) <see cref="ServiceBase.ServiceName"/> : <see cref="ServiceBase.MaxIterationsName"/>, then b) <see cref="ServiceBase.MaxIterationsName"/>, where specified; otherwise, <see cref="ServiceBase.DefaultMaxIterations"/>.</remarks>
        public override int MaxIterations
        {
            get => _maxIterations ?? Settings.GetValue<int?>($"{ServiceName}:{MaxIterationsName}".Replace(".", "_")) ?? Settings.GetValue<int?>(MaxIterationsName.Replace(".", "_")) ?? DefaultMaxIterations;
            set => _maxIterations = value;
        }

        /// <summary>
        /// Gets or sets the maximum dequeue size to limit the number of events that are dequeued within a single operation.
        /// </summary>
        /// <remarks>Will default to <see cref="SettingsBase"/> configuration, a) <see cref="ServiceBase.ServiceName"/> : <see cref="MaxDequeueSizeName"/>, then b) <see cref="MaxDequeueSizeName"/>, where specified; otherwise, 10.</remarks>
        public int MaxDequeueSize
        {
            get => _maxDequeueSize ?? Settings.GetValue<int?>($"{ServiceName}:{MaxDequeueSizeName}".Replace(".", "_")) ?? Settings.GetValue<int?>(MaxDequeueSizeName.Replace(".", "_")) ?? 10;
            set => _maxDequeueSize = value;
        }

        /// <summary>
        /// Get or sets the function to create an instance of <see cref="EventOutboxDequeueBase"/>.
        /// </summary>
        public Func<IServiceProvider, EventOutboxDequeueBase>? EventOutboxDequeueFactory { get; set; }

        /// <summary>
        /// Executes the <see cref="EventOutboxDequeueFactory"/> instance to perform the <see cref="EventOutboxDequeueBase.DequeueAndSendAsync(int, string?, string?, CancellationToken)"/>.
        /// </summary>
        /// <param name="scopedServiceProvider">The scoped <see cref="IServiceProvider"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates to execute the next iteration (i.e. continue); otherwise, <c>false</c> to stop.</returns>
        protected override async Task<bool> ExecuteAsync(IServiceProvider scopedServiceProvider, CancellationToken cancellationToken)
        {
            if (EventOutboxDequeueFactory == null)
                throw new InvalidOperationException($"The {nameof(EventOutboxDequeueFactory)} property must be configured to create an instance of the {nameof(EventOutboxDequeueBase)}.");

            var eod = EventOutboxDequeueFactory(scopedServiceProvider) ?? throw new InvalidOperationException($"The {nameof(EventOutboxDequeueFactory)} function must return an instance of {nameof(EventOutboxDequeueBase)}.");
            var sent = await eod.DequeueAndSendAsync(MaxDequeueSize, PartitionKey, Destination, cancellationToken).ConfigureAwait(false);
            return sent > 0;
        }
    }
}