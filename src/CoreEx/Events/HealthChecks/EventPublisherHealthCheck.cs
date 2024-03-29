// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events.HealthChecks
{
    /// <summary>
    /// Provides a health check for the <see cref="IEventSender"/> by sending a health-check <see cref="EventData"/> (see <see cref="EventPublisherHealthCheckOptions.EventData"/>).
    /// </summary>
    /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
    /// <remarks><i>Note:</i> Only use where the corresponding subscriber(s)/consumer(s) are aware and can ignore/filter to avoid potential downstream challenges.</remarks>
    public class EventPublisherHealthCheck(IEventPublisher eventPublisher) : IHealthCheck
    {
        private readonly IEventPublisher _eventPublisher = eventPublisher.ThrowIfNull(nameof(eventPublisher));

        /// <summary>
        /// Gets or sets the <see cref="EventPublisherHealthCheckOptions"/>.
        /// </summary>
        public EventPublisherHealthCheckOptions Options { get; } = new EventPublisherHealthCheckOptions();

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var options = Options ?? new EventPublisherHealthCheckOptions();
            var @event = options.EventData.Copy();
            if (options.Destination is null)
                _eventPublisher.Publish(@event);
            else
                _eventPublisher.PublishNamed(options.Destination, @event);

            await _eventPublisher.SendAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy(null, new Dictionary<string, object> { { "destination", options.Destination ?? "<default>" }, { "published", @event } } );
        }
    }
}