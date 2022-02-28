// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Text.Json.Serialization;

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents the base <b>Reference Data</b> type.
    /// </summary>
    public abstract class ReferenceDataBase<T> : IReferenceData, IIdentifier<T>, IChangeLog
    {
        /// <inheritdoc/>
        public T Id { get; set; } = default!;

        /// <inheritdoc/>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName("changeLog")]
        public ChangeLog? ChangeLog { get; set; }
    }
}