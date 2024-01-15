// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents an <see cref="ILogger"/> event publisher; whereby the events are output using <see cref="LoggerExtensions.LogInformation(ILogger, string, object[])"/>.
    /// </summary>
    /// <remarks>This is intended for testing and/or prototyping purposes.</remarks>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>. Defaults where not specified.</param>
    /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/>.</param>
    public class LoggerEventPublisher(ILogger<LoggerEventPublisher> logger, EventDataFormatter? eventDataFormatter, IJsonSerializer? jsonSerializer) : EventPublisher(eventDataFormatter, new CoreEx.Text.Json.EventDataSerializer(), new NullEventSender())
    {
        private readonly ILogger _logger = logger.ThrowIfNull(nameof(logger));
        private readonly IJsonSerializer _jsonSerializer = jsonSerializer ?? JsonSerializer.Default;

        /// <inheritdoc/>
        protected override Task OnEventSendAsync(string? name, EventData eventData, EventSendData eventSendData, CancellationToken cancellation)
        {
            var sb = new StringBuilder("Event send");
            if (!string.IsNullOrEmpty(name))
                sb.Append($" (destination: '{name}')");

            sb.AppendLine(" ->");

            var json = _jsonSerializer.Serialize(eventData, JsonWriteFormat.Indented);
            sb.Append(json);
            _logger.LogInformation("{Event}", sb.ToString());

            return Task.CompletedTask;
        }
    }
}