// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Text.Json.Serialization;

namespace CoreEx.RefData.Models
{
    /// <summary>
    /// Represents the extended <see cref="IReferenceDataExtended"/> base implementation.
    /// </summary>
    public abstract class ReferenceDataExtendedBase<T> : IReferenceDataExtended, IIdentifier<T>
    {
        /// <inheritdoc/>
        [JsonPropertyName("id")]
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
    }
}