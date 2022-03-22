// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Logging;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents an <see cref="ILogger"/> event sender; whereby the <see cref="EventSendData.Data"/> is output using <see cref="LoggerExtensions.LogInformation(ILogger, string, object[])"/>.
    /// </summary>
    /// <remarks>This is intended for testing and/or prototyping purposes.</remarks>
    public class LoggerEventSender : IEventSender
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerEventSender"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public LoggerEventSender(ILogger<LoggerEventSender> logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <inheritdoc/>
        public Task SendAsync(params EventSendData[] events)
        {
            var i = 0;
            foreach (var @event in events)
            {
                try
                {
                    var jo = JsonNode.Parse(@event.Data);
                    _logger.LogInformation("Event[{index}].Data = {Data}", i, jo == null ? "<null>" : $"{Environment.NewLine}{jo.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");
                }
                catch
                {
                    _logger.LogInformation("Event[{index}].Data = {Data}", i, @event.Data == null ? "<null>" : $"{Environment.NewLine}{@event.Data}");
                }

                i++;
            }

            return Task.CompletedTask;
        }
    }
}