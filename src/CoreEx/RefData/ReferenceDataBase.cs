// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Diagnostics;

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents the <see cref="IReferenceData"/> implementation.
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Code = {Code}, Text = {Text}, IsActive = {IsActive}")]
    public abstract class ReferenceDataBase : IReferenceData
    {
        private Type _idType = null!;

        /// <inheritdoc/>
        Type IIdentifier.IdType => _idType;
        
        /// <inheritdoc/>
        public object? Id { get; set; }

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
        public override string ToString() => Text ?? Code ?? Id?.ToString() ?? base.ToString()!;

        /// <summary>
        /// Sets the underlying <see cref="IIdentifier.IdType"/>.
        /// </summary>
        /// <param name="type">The <see cref="IIdentifier.IdType"/>.</param>
        protected void SetIdType(Type type) => _idType = type;
    }
}