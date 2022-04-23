// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics;

namespace CoreEx.RefData.Models
{
    /// <summary>
    /// Represents the <see cref="IReferenceData{TId}"/> implementation.
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Code = {Code}, Text = {Text}, IsActivem = {IsActive}")]
    public class ReferenceDataBase<TId> : IReferenceData<TId> where TId : IComparable<TId>, IEquatable<TId>
    {
        /// <inheritdoc/>
        public TId? Id { get; set; } = default!;

        /// <inheritdoc/>
        public string? Code { get; set; }

        /// <inheritdoc/>
        public string? Text { get; set; }

        /// <inheritdoc/>
        public string? Description { get; set; }

        /// <inheritdoc/>
        public int SortOrder { get; set; }

        /// <inheritdoc/>
        public bool IsActive { get; set; }

        /// <inheritdoc/>
        public DateTime? StartDate { get; set; }

        /// <inheritdoc/>
        public DateTime? EndDate { get; set; }

        /// <inheritdoc/>
        public string? ETag { get; set; }

        /// <inheritdoc/>
        public override string ToString() => Text ?? Code ?? Id?.ToString() ?? base.ToString();
    }
}