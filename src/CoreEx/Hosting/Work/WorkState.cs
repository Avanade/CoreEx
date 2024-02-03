// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Text.Json.Serialization;

namespace CoreEx.Hosting.Work
{
    /// <summary>
    /// Represents the status and result of a long-running <see cref="WorkStateOrchestrator"/>-tracked work instance.
    /// </summary>
    public class WorkState : IIdentifier<string>
    {
        /// <inheritdoc/>
        /// <remarks>The identifier must be globally unique across all work types.</remarks>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkStateOrchestrator"/> type name.
        /// </summary>
        /// <remarks>Enables separation between one or more <see cref="WorkState"/> types.</remarks>
        [JsonPropertyName("type")]
        public string? TypeName { get; set; }

        /// <summary>
        /// Gets or sets the related entity key where applicable.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkStatus"/>.
        /// </summary>
        public WorkStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkStatus.Created"/> <see cref="DateTimeOffset"/>.
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkStateOrchestrator"/> expiry <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <remarks>Where the work has not <see cref="WorkStatus.Finished"/> by the expiry it will be automatically <see cref="WorkStatus.Expired"/>.</remarks>
        public DateTimeOffset Expiry { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkStatus.Started"/> <see cref="DateTimeOffset"/>.
        /// </summary>
        public DateTimeOffset? Started { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkStatus.Indeterminate"/> <see cref="DateTimeOffset"/>.
        /// </summary>
        public DateTimeOffset? Indeterminate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkStatus.Finished"/> <see cref="DateTimeOffset"/>.
        /// </summary>
        public DateTimeOffset? Finished { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WorkStatus.Failed"/> or <see cref="WorkStatus.Expired"/> reason.
        /// </summary>
        public string? Reason { get; set; }
    }
}