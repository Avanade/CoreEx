// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Json;
using CoreEx.Mapping;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace CoreEx.Database.SqlServer.Outbox
{
    /// <summary>
    /// Provides the base <see cref="EventSendData"/> <see cref="IDatabase">database</see> <i>outbox dequeue</i>.
    /// </summary>
    /// <remarks>The <see cref="EventDataBase.Id"/> (being the unique event identifier) can be leveraged by the underlying messaging platform to perform duplicate checking. There is no guarantee that a dequeued event is <i>on</i> published more
    /// than once, the guarantee is at best <i>at-least</i> once semantics based on the implementation of the final <see cref="IEventSender"/>.
    /// </remarks>
    /// <param name="database">The <see cref="IDatabase"/>.</param>
    /// <param name="eventSender">The <see cref="IEventSender"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public abstract class EventOutboxDequeueBase(IDatabase database, IEventSender eventSender, ILogger<EventOutboxDequeueBase> logger) : IDatabaseMapper<EventSendData>
    {
        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        protected IDatabase Database { get; } = database.ThrowIfNull(nameof(database));

        /// <summary>
        /// Gets the <see cref="IEventSender"/>.
        /// </summary>
        protected IEventSender EventSender { get; } = eventSender.ThrowIfNull(nameof(eventSender));

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; } = logger.ThrowIfNull(nameof(logger));

        /// <summary>
        /// Gets the event outbox <i>dequeue</i> stored procedure name.
        /// </summary>
        protected abstract string DequeueStoredProcedure { get; }

        /// <summary>
        /// Gets the column name for the <see cref="EventDataBase.Id"/> property within the event outbox table.
        /// </summary>
        /// <remarks>Defaults to '<c>EventId</c>'.</remarks>
        protected virtual string EventIdColumnName => "EventId";

        /// <summary>
        /// Gets or sets the default partition key.
        /// </summary>
        /// <remarks>Defaults to '<c>$default</c>'. This will ensure that value is nullified when reading from the database.</remarks>
        public string DefaultPartitionKey { get; set; } = "$default";

        /// <summary>
        /// Gets or sets the default destination name.
        /// </summary>
        /// <remarks>Defaults to '<c>$default</c>'. This will ensure that value is nullified when reading from the database.</remarks>
        public string DefaultDestination { get; set; } = "$default";

        /// <summary>
        /// Performs the dequeue of the events (up to <paramref name="maxDequeueSize"/>) from the database outbox and then sends (via <see cref="EventSender"/>).
        /// </summary>
        /// <param name="maxDequeueSize">The maximum dequeue size. Defaults to 50.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="destination">The destination name (i.e. queue or topic).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The number of dequeued and sent events.</returns>
        public async Task<int> DequeueAndSendAsync(int maxDequeueSize = 50, string? partitionKey = null, string? destination = null, CancellationToken cancellationToken = default)
        {
            Stopwatch sw;
            maxDequeueSize = maxDequeueSize > 0 ? maxDequeueSize : 1;

            // Where a cancel has been requested then this is a convenient time to do it.
            if (cancellationToken.IsCancellationRequested)
                return 0;

            // Manage a transaction to ensure that the dequeue only commits after successful publish.
            var txn = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                // Dequeue the events; where there are none to send, then simply exit and try again later.
                Logger.LogTrace("Dequeue events. [MaxDequeueSize={MaxDequeueSize}, PartitionKey={PartitionKey}, Destination={Destination}]", maxDequeueSize, partitionKey, destination);

                sw = Stopwatch.StartNew();
                var events = await DequeueAsync(maxDequeueSize, partitionKey, destination, cancellationToken).ConfigureAwait(false);
                sw.Stop();

                if (events == null || !events.Any())
                {
                    txn.Complete();
                    return 0;
                }

                Logger.LogInformation("{EventCount} event(s) were dequeued. [Elapsed={Elapsed}ms]", events.Count(), sw.Elapsed.TotalMilliseconds);

                // Send the events.
                sw = Stopwatch.StartNew();
                await EventSender.SendAsync(events.ToArray(), cancellationToken).ConfigureAwait(false);
                sw.Stop();
                Logger.LogInformation("{EventCount} event(s) were sent successfully. [Sender={Sender}, Elapsed={Elapsed}ms]", events.Count(), EventSender.GetType().Name, sw.Elapsed.TotalMilliseconds);

                // Commit the transaction.
                txn.Complete();
                return events.Count();
            }
            finally
            {
                txn?.Dispose();
            }
        }

        /// <summary>
        /// Dequeues the <see cref="EventSendData"/> list.
        /// </summary>
        private Task<IEnumerable<EventSendData>> DequeueAsync(int maxDequeueSize, string? partitionKey, string? destination, CancellationToken cancellationToken)
            => Database.StoredProcedure(DequeueStoredProcedure)
                       .Param("@MaxDequeueSize", maxDequeueSize)
                       .Param("@PartitionKey", partitionKey)
                       .Param("@Destination", destination)
                       .SelectQueryAsync(this, cancellationToken);

        /// <inheritdoc/>
        public EventSendData MapFromDb(DatabaseRecord record, OperationTypes operationType = OperationTypes.Unspecified)
        {
            var source = record.GetValue<string?>(nameof(EventSendData.Source));
            var attributes = record.GetValue<byte[]>(nameof(EventSendData.Attributes));
            var data = record.GetValue<byte[]>(nameof(EventSendData.Data));
            var destination = record.GetValue<string?>(nameof(EventSendData.Destination));
            var partitionKey = record.GetValue<string?>(nameof(EventSendData.PartitionKey));

            return new()
            {
                Id = record.GetValue<string?>(EventIdColumnName),
                Destination = destination == DefaultDestination ? null : destination,
                Subject = record.GetValue<string?>(nameof(EventSendData.Subject)),
                Action = record.GetValue<string?>(nameof(EventSendData.Action)),
                Type = record.GetValue<string?>(nameof(EventSendData.Type)),
                Source = string.IsNullOrEmpty(source) ? null : new Uri(source, UriKind.RelativeOrAbsolute),
                Timestamp = record.GetValue<DateTimeOffset>(nameof(EventSendData.Timestamp)),
                CorrelationId = record.GetValue<string?>(nameof(EventSendData.CorrelationId)),
                Key = record.GetValue<string?>(nameof(EventSendData.Key)),
                TenantId = record.GetValue<string?>(nameof(EventSendData.TenantId)),
                PartitionKey = partitionKey == DefaultPartitionKey ? null : partitionKey,
                ETag = record.GetValue<string?>(nameof(EventSendData.ETag)),
                Attributes = attributes == null || attributes.Length == 0 ? null : JsonSerializer.Default.Deserialize<Dictionary<string, string>>(new BinaryData(attributes)),
                Data = data == null || data.Length == 0 ? null : new BinaryData(data)
            };
        }

        /// <inheritdoc/>
        /// <remarks>This method will result in a <see cref="NotSupportedException"/>.</remarks>
        void IDatabaseMapper<EventSendData>.MapToDb(EventSendData? value, DatabaseParameterCollection parameters, OperationTypes operationType) => throw new NotSupportedException();
    }
}