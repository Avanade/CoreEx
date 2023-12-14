// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Diagnostics;

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents the basic <see cref="IReferenceData"/> implementation.
    /// </summary>
    /// <remarks>For a fully-featured implementation see <see cref="Extended.ReferenceDataBaseEx{TId, TSelf}"/>.</remarks>
    [DebuggerDisplay("Id = {Id}, Code = {Code}, Text = {Text}, IsActive = {IsActive}")]
    public abstract class ReferenceDataBase : IReferenceData
    {
        private bool _isValid = true;

        /// <inheritdoc/>
        Type IIdentifier.IdType => throw new NotImplementedException();
        
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
        public bool IsActive { get; set; } = true;

        /// <inheritdoc/>
        public DateTime? StartDate { get; set; }

        /// <inheritdoc/>
        public DateTime? EndDate { get; set; }

        /// <inheritdoc/>
        public string? ETag { get; set; }

        /// <inheritdoc/>
        bool IReferenceData.IsValid => _isValid;

        /// <inheritdoc/>
        void IReferenceData.SetInvalid() => _isValid = false;

        /// <inheritdoc/>
        public override string ToString() => Text ?? Code ?? Id?.ToString() ?? base.ToString()!;
    }
}