// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents the <see cref="EventDataBase"/> property selection.
    /// </summary>
    /// <remarks>The <see cref="EventDataBase.Id"/>, <see cref="EventDataBase.Timestamp"/> and <see cref="EventData.Value"/> are non-selectable; i.e. are always included.</remarks>
    [Flags]
    public enum EventDataProperty
    {
        /// <summary>
        /// Represents no properties.
        /// </summary>
        None = 0,

        /// <summary>
        /// Selects the <see cref="EventDataBase.Subject"/> property.
        /// </summary>
        Subject = 1,

        /// <summary>
        /// Selects the <see cref="EventDataBase.Action"/> property.
        /// </summary>
        Action = 2,

        /// <summary>
        /// Selects the <see cref="EventDataBase.Type"/> property.
        /// </summary>
        Type = 4,

        /// <summary>
        /// Selects the <see cref="EventDataBase.Source"/> property.
        /// </summary>
        Source = 8,

        /// <summary>
        /// Selects the <see cref="EventDataBase.TenantId"/> property.
        /// </summary>
        TenantId = 16,

        /// <summary>
        /// Selects the <see cref="EventDataBase.PartitionKey"/> property.
        /// </summary>
        PartitionKey = 32,

        /// <summary>
        /// Selects the <see cref="EventDataBase.ETag"/> property.
        /// </summary>
        ETag = 64,

        /// <summary>
        /// Selects the <see cref="EventDataBase.CorrelationId"/> property.
        /// </summary>
        CorrelationId = 128,

        /// <summary>
        /// Selects the <see cref="EventDataBase.Attributes"/> property.
        /// </summary>
        Attributes = 256,

        /// <summary>
        /// Selects all of the properties.
        /// </summary>
        All = AllExceptAttributes | Attributes,

        /// <summary>
        /// Selects all of the properties except <see cref="Attributes"/>.
        /// </summary>
        AllExceptAttributes = Subject | Action | Type | Source | TenantId | PartitionKey | ETag | CorrelationId,
    }
}