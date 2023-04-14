// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database.SqlServer.Outbox
{
    /// <summary>
    /// Provides the <see cref="EventOutboxDequeueBase"/> dequeue and publish (<see cref="SynchronizedTimerHostedServiceBase{TSync}"/>) capabilities.
    /// </summary>
    /// <remarks>This will instantiate an <see cref="EventOutboxDequeueBase"/> using the underlying <see cref="ServiceProvider"/> and invoke <see cref="EventOutboxDequeueBase.DequeueAndSendAsync(int, string?, string?, CancellationToken)"/>.</remarks>
    public class EventOutboxHostedService : SynchronizedTimerHostedServiceBase<EventOutboxHostedService>
    {
        private TimeSpan? _interval;
        private int? _maxQuerySize;
        private string? _name;

        /// <summary>
        /// Get or sets the configuration name for <see cref="Interval"/>. Defaults to '<c>Interval</c>'.
        /// </summary>
        public string IntervalName { get; set; } = "Interval";

        /// <summary>
        /// Gets or sets the configuration name for <see cref="MaxDequeueSize"/>. Defaults to '<c>MaxDequeueSize</c>'.
        /// </summary>
        public string MaxDequeueSizeName { get; set; } = "MaxDequeueSize";

        /// <summary>
        /// Gets or sets the default interval seconds used where the specified <see cref="Interval"/> is not configured/specified. Defaults to <b>thirty</b> seconds.
        /// </summary>
        public static TimeSpan DefaultInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initializes a new instance of the <see cref="EventOutboxHostedService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="synchronizer">The <see cref="IServiceSynchronizer"/>.</param>
        /// <param name="partitionKey">The optional partition key.</param>
        /// <param name="destination">The optional destination name (i.e. queue or topic).</param>
        public EventOutboxHostedService(IServiceProvider serviceProvider, ILogger<EventOutboxHostedService> logger, SettingsBase settings, IServiceSynchronizer synchronizer, string? partitionKey = null, string? destination = null)
            : base(serviceProvider, logger, settings, synchronizer)
        {
            PartitionKey = partitionKey;
            Destination = destination;

            // Build the synchronization name.
            var sb = new StringBuilder();
            if (partitionKey != null)
                sb.Append($"PartitionKey-{partitionKey}");

            if (destination != null)
            {
                if (sb.Length > 0)
                    sb.Append('-');

                SynchronizationName = $"Destination-{destination}";
            }

            SynchronizationName = sb.Length > 0 ? sb.ToString() : null;
        }

        /// <summary>
        /// Gets the optional partition key.
        /// </summary>
        public string? PartitionKey { get; }

        /// <summary>
        /// Gets the optional destination name (i.e. queue or topic).
        /// </summary>
        public string? Destination { get; }

        /// <summary>
        /// Gets the service name (used for the likes of configuration and logging).
        /// </summary>
        public override string ServiceName => _name ??= $"{GetType().Name}{(PartitionKey == null ? "" : $".{PartitionKey}")}";

        /// <summary>
        /// Gets or sets the interval between each execution.
        /// </summary>
        /// <remarks>Will default to <see cref="SettingsBase"/> configuration, a) <see cref="TimerHostedServiceBase.ServiceName"/> : <see cref="IntervalName"/>, then b) <see cref="IntervalName"/>, where specified; otherwise, <see cref="DefaultInterval"/>.</remarks>
        public override TimeSpan Interval
        {
            get => _interval ?? Settings.GetValue<TimeSpan?>($"{ServiceName}:{IntervalName}".Replace(".", "_")) ?? Settings.GetValue<TimeSpan?>(IntervalName.Replace(".", "_")) ?? DefaultInterval;
            set => _interval = value;
        }

        /// <summary>
        /// Gets or sets the maximum dequeue size to limit the number of events that are dequeued within a single operation.
        /// </summary>
        /// <remarks>Will default to <see cref="SettingsBase"/> configuration, a) <see cref="TimerHostedServiceBase.ServiceName"/> : <see cref="MaxDequeueSizeName"/>, then b) <see cref="MaxDequeueSizeName"/>, where specified; otherwise, 10.</remarks>
        public int MaxDequeueSize
        {
            get => _maxQuerySize ?? Settings.GetValue<int?>($"{ServiceName}:{MaxDequeueSizeName}".Replace(".", "_")) ?? Settings.GetValue<int?>(MaxDequeueSizeName.Replace(".", "_")) ?? 10;
            set => _maxQuerySize = value;
        }

        /// <summary>
        /// Get or sets the function to create an instance of <see cref="EventOutboxDequeueBase"/>.
        /// </summary>
        public Func<IServiceProvider, EventOutboxDequeueBase>? EventOutboxDequeueFactory { get; set; }

        /// <summary>
        /// Executes the <see cref="EventOutboxDequeueFactory"/> instance to perform the <see cref="EventOutboxDequeueBase.DequeueAndSendAsync(int, string?, string?, CancellationToken)"/> until queue is empty.
        /// </summary>
        /// <param name="scopedServiceProvider"><inheritdoc/></param>
        /// <param name="cancellationToken"><inheritdoc/></param>
        protected override async Task SynchronizedExecuteAsync(IServiceProvider scopedServiceProvider, CancellationToken cancellationToken = default)
        {
            if (EventOutboxDequeueFactory == null)
                throw new NotImplementedException($"The {nameof(EventOutboxDequeueFactory)} property must be configured to create an instance of the {nameof(EventOutboxDequeueBase)}.");

            try
            {
                int sent;

                do
                {
                    // As we want to tight loop the execution where there mey be more in the queue, a new 'Scope' is used to ensure new instances of dependencies are used otherwise a disposed error may occur for the underlying transaction.
                    using var scope = scopedServiceProvider.CreateScope();
                    var eod = EventOutboxDequeueFactory(scope.ServiceProvider) ?? throw new InvalidOperationException($"The {nameof(EventOutboxDequeueFactory)} function must return an instance of {nameof(EventOutboxDequeueBase)}.");
                    sent = await eod.DequeueAndSendAsync(MaxDequeueSize, PartitionKey, Destination, cancellationToken).ConfigureAwait(false);
                }
                while (sent > 0) ;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Event Outbox dequeue and send failed: {Error}", ex.Message);
            }
        }
    }
}