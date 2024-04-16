// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database.SqlServer.Outbox
{
    /// <summary>
    /// Provides the base <see cref="EventSendData"/> <see cref="IDatabase">database</see> <i>outbox enqueue</i> <see cref="IEventSender.SendAsync(IEnumerable{EventSendData}, CancellationToken)"/>. 
    /// </summary>
    /// <remarks>By default the events are first sent/enqueued to the database outbox, then a secondary process dequeues and sends. Also, by enqueing to a single database outbox the event publishing order is preserved.
    /// <para>This can however introduce unwanted latency depending on the frequency in which the secondary process performs the dequeue and send, as this is essentially a polling-based operation. To improve (minimize) latency, the primary
    /// <see cref="IEventSender"/> can be specified using <see cref="SetPrimaryEventSender(IEventSender)"/>. This will then be used to send the events immediately, and where successful, they will be audited in the database as dequeued 
    /// event(s); versus on error (as a backup), where they will be enqueued for the out-of-process dequeue and send (as per default). Note: the challenge this primary sender introduces is in-order publishing; there is no means to guarantee order for the 
    /// events that are processed on error.</para></remarks>
    /// <param name="database">The <see cref="IDatabase"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public abstract class EventOutboxEnqueueBase(IDatabase database, ILogger<EventOutboxEnqueueBase> logger) : IEventSender
    {
        private IEventSender? _eventSender;

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        protected IDatabase Database { get; } = database.ThrowIfNull(nameof(database));

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; } = logger.ThrowIfNull(nameof(logger));

        /// <summary>
        /// Gets the database type name for the <see cref="TableValuedParameter"/>.
        /// </summary>
        protected abstract string DbTvpTypeName { get; }

        /// <summary>
        /// Gets the event outbox <i>enqueue</i> stored procedure name.
        /// </summary>
        protected abstract string EnqueueStoredProcedure { get; }

        /// <summary>
        /// Gets the column name for the <see cref="EventDataBase.Id"/> property within the event outbox table.
        /// </summary>
        /// <remarks>Defaults to '<c>EventId</c>'.</remarks>
        protected virtual string EventIdColumnName => "EventId";

        /// <summary>
        /// Gets or sets the default partition key.
        /// </summary>
        /// <remarks>Defaults to '<c>$default</c>'. This will ensure that there is always a value recorded in the database.</remarks>
        public string DefaultPartitionKey { get; set; } = "$default";

        /// <summary>
        /// Gets or sets the default destination name.
        /// </summary>
        /// <remarks>Defaults to '<c>$default</c>'. This will ensure that there is always a value recorded in the database.</remarks>
        public string DefaultDestination { get; set; } = "$default";

        /// <summary>
        /// Sets the <see cref="IEventSender"/> to act as the primary <see cref="IEventSender"/> where <i>outbox enqueue</i> is to provide backup/audit capabilities.
        /// </summary>
        public void SetPrimaryEventSender(IEventSender eventSender)
        {
            if (eventSender != null & eventSender is EventOutboxEnqueueBase)
                throw new ArgumentException($"{nameof(SetPrimaryEventSender)} value must not be of Type {nameof(EventOutboxEnqueueBase)}.", nameof(eventSender));

            _eventSender = eventSender;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="events"><inheritdoc/></param>
        /// <param name="cancellationToken"><inheritdoc/></param>
        /// <remarks>Executes the <see cref="EnqueueStoredProcedure"/> to <i>send / enqueue</i> the <paramref name="events"/> to the underlying database outbox tables.</remarks>
        public async Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellationToken = default)
        {
            if (events == null || !events.Any())
                return;

            Stopwatch sw = Stopwatch.StartNew();
            var unsentEvents = new List<EventSendData>(events);

            if (_eventSender != null)
            {
                try
                {
                    await _eventSender!.SendAsync(events, cancellationToken).ConfigureAwait(false);
                    sw.Stop();
                    unsentEvents.Clear();
                    Logger.LogDebug("{EventCount} event(s) were sent successfully; will be forwarded (sent/enqueued) to the datatbase outbox as sent. [Sender={Sender}, Elapsed={Elapsed}ms]",
                        events.Count(), _eventSender.GetType().Name, sw.Elapsed.TotalMilliseconds);
                }
                catch (EventSendException esex)
                {
                    sw.Stop();
                    Logger.LogWarning(esex, "{UnsentCount} of {EventCount} event(s) were unable to be sent successfully; will be forwarded (sent/enqueued) to the datatbase outbox for an out-of-process send: {ErrorMessage} [Sender={Sender}, Elapsed={Elapsed}ms]",
                        esex.NotSentEvents?.Count() ?? unsentEvents.Count, events.Count(), esex.Message, _eventSender!.GetType().Name, sw.Elapsed.TotalMilliseconds);

                    if (esex.NotSentEvents != null)
                        unsentEvents = esex.NotSentEvents.ToList();
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    Logger.LogWarning(ex, "{EventCount} event(s) were unable to be sent successfully; will be forwarded (sent/enqueued) to the datatbase outbox for an out-of-process send: {ErrorMessage} [Sender={Sender}, Elapsed={Elapsed}ms]",
                        events.Count(), ex.Message, _eventSender!.GetType().Name, sw.Elapsed.TotalMilliseconds);
                }
            }

            sw = Stopwatch.StartNew();
            await Database.StoredProcedure(EnqueueStoredProcedure)
                          .TableValuedParam("@EventList", CreateTableValuedParameter(events, unsentEvents))
                          .NonQueryAsync(cancellationToken).ConfigureAwait(false);

            sw.Stop();
            Logger.LogDebug("{EventCount} event(s) were enqueued; {SuccessCount} as sent, {ErrorCount} to be sent. [Sender={Sender}, Elapsed={Elapsed}ms]",
                events.Count(), events.Count() - unsentEvents.Count, unsentEvents.Count, GetType().Name, sw.Elapsed.TotalMilliseconds);

            AfterSend?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Creates the TVP from the list.
        /// </summary>
        private TableValuedParameter CreateTableValuedParameter(IEnumerable<EventSendData> list, IEnumerable<EventSendData> unsentList)
        {
            var dt = new DataTable();
            dt.Columns.Add(EventIdColumnName, typeof(string));
            dt.Columns.Add("EventDequeued", typeof(bool));
            dt.Columns.Add(nameof(EventSendData.Destination), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Subject), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Action), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Type), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Source), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Timestamp), typeof(DateTimeOffset));
            dt.Columns.Add(nameof(EventSendData.CorrelationId), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Key), typeof(string));
            dt.Columns.Add(nameof(EventSendData.TenantId), typeof(string));
            dt.Columns.Add(nameof(EventSendData.PartitionKey), typeof(string));
            dt.Columns.Add(nameof(EventSendData.ETag), typeof(string));
            dt.Columns.Add(nameof(EventSendData.Attributes), typeof(byte[]));
            dt.Columns.Add(nameof(EventSendData.Data), typeof(byte[]));

            var tvp = new TableValuedParameter(DbTvpTypeName, dt);
            foreach (var item in list)
            {
                var attributes = item.Attributes == null || item.Attributes.Count == 0 ? new BinaryData(Array.Empty<byte>()) : JsonSerializer.Default.SerializeToBinaryData(item.Attributes);

                tvp.AddRow(item.Id, !unsentList.Contains(item),
                    item.Destination ?? DefaultDestination ?? throw new InvalidOperationException($"The {nameof(DefaultDestination)} must have a non-null value."),
                    item.Subject, item.Action, item.Type, item.Source, item.Timestamp, item.CorrelationId, item.Key, item.TenantId,
                    item.PartitionKey ?? DefaultPartitionKey ?? throw new InvalidOperationException($"The {nameof(DefaultPartitionKey)} must have a non-null value."),
                    item.ETag, attributes.ToArray(), item.Data?.ToArray());
            }

            return tvp;
        }

        /// <inheritdoc/>
        public event EventHandler? AfterSend;
    }
}