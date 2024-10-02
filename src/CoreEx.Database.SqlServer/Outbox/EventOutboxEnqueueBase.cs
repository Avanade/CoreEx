// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
                          .Param("@EventList", CreateEventsJsonForDatabase(events, unsentEvents))
                          .NonQueryAsync(cancellationToken).ConfigureAwait(false);

            sw.Stop();
            Logger.LogDebug("{EventCount} event(s) were enqueued; {SuccessCount} as sent, {ErrorCount} to be sent. [Sender={Sender}, Elapsed={Elapsed}ms]",
                events.Count(), events.Count() - unsentEvents.Count, unsentEvents.Count, GetType().Name, sw.Elapsed.TotalMilliseconds);

            AfterSend?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Creates the events JSON to send to the database.
        /// </summary>
        private string CreateEventsJsonForDatabase(IEnumerable<EventSendData> list, IEnumerable<EventSendData> unsentList)
        {
            using var stream = new MemoryStream();
            using var json = new Utf8JsonWriter(stream);

            json.WriteStartArray();

            foreach (var item in list)
            {
                json.WriteStartObject();
                if (item.Id is not null)
                    json.WriteString(EventIdColumnName, item.Id);

                json.WriteBoolean("EventDequeued", !unsentList.Contains(item));
                json.WriteString(nameof(EventSendData.Destination), item.Destination ?? DefaultDestination ?? throw new InvalidOperationException($"The {nameof(DefaultDestination)} must have a non-null value."));

                if (item.Subject is not null)
                    json.WriteString(nameof(EventSendData.Subject), item.Subject);

                if (item.Action is not null)
                    json.WriteString(nameof(EventSendData.Action), item.Action);

                if (item.Type is not null)
                    json.WriteString(nameof(EventSendData.Type), item.Type);

                if (item.Source is not null)
                    json.WriteString(nameof(EventSendData.Source), item.Source?.ToString());

                if (item.Timestamp is not null)
                    json.WriteString(nameof(EventSendData.Timestamp), (DateTimeOffset)item.Timestamp);

                if (item.CorrelationId is not null)
                    json.WriteString(nameof(EventSendData.CorrelationId), item.CorrelationId);

                if (item.Key is not null)
                    json.WriteString(nameof(EventSendData.Key), item.Key);

                if (item.TenantId is not null)
                    json.WriteString(nameof(EventSendData.TenantId), item.TenantId);

                json.WriteString(nameof(EventSendData.PartitionKey), item.PartitionKey ?? DefaultPartitionKey ?? throw new InvalidOperationException($"The {nameof(DefaultPartitionKey)} must have a non-null value."));

                if (item.ETag is not null)
                    json.WriteString(nameof(EventSendData.ETag), item.ETag);

                json.WriteBase64String(nameof(EventSendData.Attributes), item.Attributes == null || item.Attributes.Count == 0 ? new BinaryData([]) : Json.JsonSerializer.Default.SerializeToBinaryData(item.Attributes));
                json.WriteBase64String(nameof(EventSendData.Data), item.Data ?? new BinaryData([]));
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <inheritdoc/>
        public event EventHandler? AfterSend;
    }
}