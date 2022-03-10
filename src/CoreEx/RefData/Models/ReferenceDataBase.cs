// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System.Text.Json.Serialization;

namespace CoreEx.RefData.Models
{
    /// <summary>
    /// Represents the base <see cref="IReferenceData"/> model.
    /// </summary>
    public abstract class ReferenceDataBase<T> : IReferenceData, IIdentifier<T>
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
    }
}