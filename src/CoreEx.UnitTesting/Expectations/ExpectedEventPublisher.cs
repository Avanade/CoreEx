﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnitTestEx.Abstractions;

namespace UnitTestEx.Expectations
{
    /// <summary>
    /// Provides an expected event publisher to support <see cref="EventExpectations{TTester}"/>.
    /// </summary>
    /// <remarks>Where an <see cref="ILogger"/> is provided then each <see cref="EventData"/> will also be logged during <i>Send</i>.</remarks>
    public sealed class ExpectedEventPublisher : EventPublisher
    {
        private readonly TestSharedState _sharedState;
        private readonly ILogger? _logger;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Get the <c>null</c> key name.
        /// </summary>
        public const string NullKeyName = "<default>";

        /// <summary>
        /// Gets the <see cref="ExpectedEventPublisher"/> from the <see cref="TestSharedState"/>.
        /// </summary>
        /// <param name="sharedState">The <see cref="TestSharedState"/>.</param>
        /// <returns>The <see cref="ExpectedEventPublisher"/> where found; otherwise, <c>null</c>.</returns>
        public static ExpectedEventPublisher? GetFromSharedState(TestSharedState sharedState)
        {
            if (sharedState == null)
                throw new ArgumentNullException(nameof(sharedState));

            return sharedState.StateData.TryGetValue(nameof(ExpectedEventPublisher), out var eep) ? eep as ExpectedEventPublisher : null;
        }

        /// <summary>
        /// Sets the <see cref="ExpectedEventPublisher"/> into the <see cref="TestSharedState"/>.
        /// </summary>
        /// <param name="sharedState">The <see cref="TestSharedState"/>.</param>
        /// <param name="expectedEventPublisher">The <see cref="ExpectedEventPublisher"/>.</param>
        public static void SetToSharedState(TestSharedState sharedState, ExpectedEventPublisher? expectedEventPublisher)
        {
            if (sharedState == null)
                throw new ArgumentNullException(nameof(sharedState));

            sharedState.StateData[nameof(ExpectedEventPublisher)] = expectedEventPublisher ?? throw new ArgumentNullException(nameof(expectedEventPublisher));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedEventPublisher"/> class.
        /// </summary>
        /// <param name="sharedState">The <see cref="TestSharedState"/>.</param>
        /// <param name="logger">The optional <see cref="ILogger"/> for logging the events (each <see cref="EventData"/>).</param>
        /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/> for the logging. Defaults to <see cref="JsonSerializer.Default"/></param>
        /// <param name="eventDataFormatter">The <see cref="EventDataFormatter"/>; defaults where not specified.</param>
        public ExpectedEventPublisher(TestSharedState sharedState, ILogger<ExpectedEventPublisher>? logger = null, IJsonSerializer? jsonSerializer = null, EventDataFormatter? eventDataFormatter = null)
            : base(eventDataFormatter, new CoreEx.Text.Json.EventDataSerializer(), new NullEventSender())
        {
            _sharedState = sharedState ?? throw new ArgumentNullException(nameof(sharedState));
            SetToSharedState(_sharedState, this);
            _logger = logger;
            _jsonSerializer = jsonSerializer ?? JsonSerializer.Default;
        }

        /// <summary>
        /// Gets the dictionary that contains the sent events by destination.
        /// </summary>
        /// <remarks>The sent events are queued as the JSON-serialized representation of the <see cref="EventData"/>.</remarks>
        public ConcurrentDictionary<string, ConcurrentQueue<string?>> SentEvents { get; } = new();

        /// <inheritdoc/>
        protected override Task OnEventSendAsync(string? name, EventData eventData, EventSendData eventSendData, CancellationToken cancellationToken)
        {
            var queue = SentEvents.GetOrAdd(name ?? NullKeyName, _ => new ConcurrentQueue<string?>());
            var json = _jsonSerializer.Serialize(eventData, JsonWriteFormat.Indented);
            queue.Enqueue(json);

            if (_logger != null)
            {
                var sb = new StringBuilder("UnitTestEx > Event send");
                if (!string.IsNullOrEmpty(name))
                    sb.Append($" (destination: '{name}')");

                sb.AppendLine(" ->");
                sb.Append(json);
                _logger.LogInformation("{Event}", sb.ToString());
            }

            return Task.CompletedTask;
        }
    }
}