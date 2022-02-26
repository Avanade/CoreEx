// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the <see cref="EventData"/> property selection.
    /// </summary>
    /// <remarks>The <see cref="EventData.Id"/>, <see cref="EventData.Timestamp"/> and <see cref="EventData.Value"/> are non-selectable; i.e. are always included.</remarks>
    [Flags]
    public enum EventDataProperty
    {
        /// <summary>
        /// Represents no properties.
        /// </summary>
        None = 0,

        /// <summary>
        /// Selects the <see cref="EventData.Subject"/> property.
        /// </summary>
        Subject = 1,

        /// <summary>
        /// Selects the <see cref="EventData.Action"/> property.
        /// </summary>
        Action = 2,

        /// <summary>
        /// Selects the <see cref="EventData.Type"/> property.
        /// </summary>
        Type = 4,

        /// <summary>
        /// Selects the <see cref="EventData.Source"/> property.
        /// </summary>
        Source = 8,

        /// <summary>
        /// Selects the <see cref="EventData.TenantId"/> property.
        /// </summary>
        TenantId = 16,

        /// <summary>
        /// Selects the <see cref="EventData.PartitionKey"/> property.
        /// </summary>
        PartitionKey = 32,

        /// <summary>
        /// Selects the <see cref="EventData.ETag"/> property.
        /// </summary>
        ETag = 64,

        /// <summary>
        /// Selects the <see cref="EventData.CorrelationId"/> property.
        /// </summary>
        CorrelationId = 128,

        /// <summary>
        /// Selects the <see cref="EventData.Attributes"/> property.
        /// </summary>
        Attributes = 256,

        /// <summary>
        /// Selects all of the properties.
        /// </summary>
        All = AllExceptAttributes | Attributes,

        /// <summary>
        /// Selects all of the properties except <see cref="Attributes"/>.
        /// </summary>
        AllExceptAttributes = Subject | Action | Type | Source | TenantId | PartitionKey | ETag | CorrelationId
    }
}