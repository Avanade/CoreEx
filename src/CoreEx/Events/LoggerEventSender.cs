// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents an <see cref="ILogger"/> event sender; whereby the <see cref="EventSendData.Data"/> is output using <see cref="LoggerExtensions.LogInformation(ILogger, string, object[])"/>.
    /// </summary>
    /// <remarks>This is intended for testing and/or prototyping purposes.</remarks>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public class LoggerEventSender(ILogger<LoggerEventSender> logger) : IEventSender
    {
        private readonly ILogger _logger = logger.ThrowIfNull(nameof(logger));

        /// <inheritdoc/>
        public Task SendAsync(IEnumerable<EventSendData> events, CancellationToken cancellation)
        {
            var i = 0;
            foreach (var @event in events)
            {
                string data;

                try
                {
                    var jo = JsonNode.Parse(@event.Data);
                    data = jo == null ? "<null>" : jo.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
                catch
                {
                    data = @event.Data == null ? "<null>" : @event.Data.ToString();
                }

                _logger.LogInformation("{Event}", $"Event[{i}].Metadata = {Json.JsonSerializer.Default.Serialize(new EventData(@event), Json.JsonWriteFormat.Indented)}{Environment.NewLine}Event[{i}].Data = {data}");
                i++;
            }

            AfterSend?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public event EventHandler? AfterSend;
    }
}