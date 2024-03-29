// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events.HealthChecks
{
    /// <summary>
    /// Gets or sets the health-check options for the <see cref="EventPublisherHealthCheck"/>.
    /// </summary>
    public class EventPublisherHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the destination name (e.g. queue or topic).
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// Gets or sets the health-check <see cref="EventData"/> template.
        /// </summary>
        public EventData EventData { get; } = new EventData
        {
            Subject = "health.check",
            Action = "probe",
            Type = "health.check",
            Source = new Uri("health/detailed", UriKind.Relative)
        };
    }
}