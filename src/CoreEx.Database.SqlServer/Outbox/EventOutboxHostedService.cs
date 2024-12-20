﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Hosting;
using CoreEx.Hosting.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private int? _maxDequeueSize;
        private string? _name;

        /// <summary>
        /// Provides an opportunity to make a one-off change to the underlying timer to trigger using the specified <paramref name="oneOffInterval"/> to the registered <see cref="EventOutboxHostedService"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to get the registered <see cref="EventOutboxHostedService"/>.</param>
        /// <param name="oneOffInterval">The one-off interval before triggering; defaults to <c>null</c> which represents an immediate trigger.</param>
        /// <param name="leaveWhereTimeRemainingIsLess">Indicates whether to <i>not</i> adjust the time where the time remaining is less than the one-off interval specified.</param>
        /// <remarks>Where there is more than one instance registered, or none, then no action will be taken.
        /// <para>This functionality is intended for low volume event publishing where there is need to bring forward the configured interval for a one-off execution. This is particularly useful where there is a need to publish
        /// an event immediately versus waiting for the next scheduled execution.</para></remarks>
        public static void OneOffTrigger(IServiceProvider serviceProvider, TimeSpan? oneOffInterval = null, bool leaveWhereTimeRemainingIsLess = true)
        {
            var services = serviceProvider.ThrowIfNull(nameof(serviceProvider)).GetServices<IHostedService>().OfType<EventOutboxHostedService>();
            if (services.Count() == 1)
                services.First().OneOffTrigger(oneOffInterval, leaveWhereTimeRemainingIsLess);
        }

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
        /// <param name="healthCheck">The optional <see cref="TimerHostedServiceHealthCheck"/> to report health.</param>
        /// <param name="partitionKey">The optional partition key.</param>
        /// <param name="destination">The optional destination name (i.e. queue or topic).</param>
        public EventOutboxHostedService(IServiceProvider serviceProvider, ILogger<EventOutboxHostedService> logger, SettingsBase settings, IServiceSynchronizer synchronizer, TimerHostedServiceHealthCheck? healthCheck = null, string? partitionKey = null, string? destination = null)
            : base(serviceProvider, logger, settings, synchronizer, healthCheck)
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
            get => _interval ?? Settings.GetCoreExValue<TimeSpan?>($"{ServiceName}:{IntervalName}".Replace(".", "_")) ?? Settings.GetCoreExValue<TimeSpan?>(IntervalName.Replace(".", "_")) ?? DefaultInterval;
            set => _interval = value;
        }

        /// <summary>
        /// Gets or sets the maximum dequeue size to limit the number of events that are dequeued within a single operation.
        /// </summary>
        /// <remarks>Will default to <see cref="SettingsBase"/> configuration, a) <see cref="TimerHostedServiceBase.ServiceName"/> : <see cref="MaxDequeueSizeName"/>, then b) <see cref="MaxDequeueSizeName"/>, where specified; otherwise, 10.</remarks>
        public int MaxDequeueSize
        {
            get => _maxDequeueSize ?? Settings.GetCoreExValue<int?>($"{ServiceName}:{MaxDequeueSizeName}".Replace(".", "_")) ?? Settings.GetCoreExValue<int?>(MaxDequeueSizeName.Replace(".", "_")) ?? 10;
            set => _maxDequeueSize = value;
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
                throw new InvalidOperationException($"The {nameof(EventOutboxDequeueFactory)} property must be configured to create an instance of the {nameof(EventOutboxDequeueBase)}.");

            int sent;

            do
            {
                // As we want to tight loop the execution where there may be more in the queue, a new 'Scope' is used to ensure new instances of dependencies are used otherwise a disposed error may occur for the underlying transaction.
                using var scope = scopedServiceProvider.CreateScope();
                ExecutionContext.Reset();
                var eod = EventOutboxDequeueFactory(scope.ServiceProvider) ?? throw new InvalidOperationException($"The {nameof(EventOutboxDequeueFactory)} function must return an instance of {nameof(EventOutboxDequeueBase)}.");
                sent = await eod.DequeueAndSendAsync(MaxDequeueSize, PartitionKey, Destination, cancellationToken).ConfigureAwait(false);
            }
            while (sent > 0) ;
        }

        /// <inheritdoc/>
        protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data)
        {
            data.Add("maxDequeueSize", MaxDequeueSize);
            data.Add("partitionKey", PartitionKey ?? "<all>");
            data.Add("destination", Destination ?? "<all>");
            data.Add("synchronizer", SynchronizationName ?? "<none>");

            return base.OnReportHealthStatus(data);
        }
    }
}