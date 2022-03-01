// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Logging;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents an <see cref="ILogger"/> event publisher; whereby the events are output using <see cref="LoggerExtensions.LogInformation(ILogger, string, object[])"/>.
    /// </summary>
    /// <remarks>This in intended for testing and/or prototyping purposes.</remarks>
    public class LoggerEventPublisher : IEventPublisher
    {
        private readonly ILogger _logger;
        private readonly EventDataFormatter _eventDataFormatter;
        private readonly IEventSerializer _eventSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerEventPublisher"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>.</param>
        /// <param name="eventSerializer">The <see cref="IEventSerializer"/>.</param>
        public LoggerEventPublisher(ILogger<LoggerEventPublisher> logger, EventDataFormatter? eventDataFormatter, IEventSerializer eventSerializer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventDataFormatter = eventDataFormatter ?? new EventDataFormatter();
            _eventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));
        }

        /// <inheritdoc/>
        public async Task SendAsync(params EventData[] events)
        {
            foreach (var @event in events)
            {
                var e = @event.Copy();
                _eventDataFormatter.Format(e);
                var bd = await _eventSerializer.SerializeAsync(e).ConfigureAwait(false);

                try
                {
                    var jo = JsonNode.Parse(bd);
                    _logger.LogInformation("{Content}", jo == null ? "<null>" : jo.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                catch
                {
                    _logger.LogInformation("{Content}", bd == null ? "<null>" : bd.ToString());
                }
            }
        }

        /// <inheritdoc/>
        public Task SendAsync(string name, params EventData[] events)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return SendAsync(events);
        }
    }
}