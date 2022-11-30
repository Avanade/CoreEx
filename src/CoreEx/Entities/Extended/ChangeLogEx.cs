// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents an extended <see cref="EntityBase"/> <see cref="IChangeLogAudit"/>.
    /// </summary>
    public class ChangeLogEx : EntityBase, IChangeLogAudit
    {
        private DateTime? _createdDate;
        private string? _createdBy;
        private DateTime? _updatedDate;
        private string? _updatedBy;

        /// <summary>
        /// Gets or sets the created <see cref="DateTime"/>.
        /// </summary>
        public DateTime? CreatedDate { get => _createdDate; set => SetValue(ref _createdDate, value); }

        /// <summary>
        /// Gets or sets the created by (username).
        /// </summary>
        public string? CreatedBy { get => _createdBy; set => SetValue(ref _createdBy, value); }

        /// <summary>
        /// Gets or sets the updated <see cref="DateTime"/>.
        /// </summary>
        public DateTime? UpdatedDate { get => _updatedDate; set => SetValue(ref _updatedDate, value); }

        /// <summary>
        /// Gets or sets the updated by (username).
        /// </summary>
        public string? UpdatedBy { get => _updatedBy; set => SetValue(ref _updatedBy, value); }

        /// <inheritdoc/>
        protected override IEnumerable<IPropertyValue> GetPropertyValues()
        {
            yield return CreateProperty(nameof(CreatedDate), CreatedDate, v => CreatedDate = v);
            yield return CreateProperty(nameof(CreatedBy), CreatedBy, v => CreatedBy = v);
            yield return CreateProperty(nameof(UpdatedDate), UpdatedDate, v => UpdatedDate = v);
            yield return CreateProperty(nameof(CreatedBy), UpdatedBy, v => UpdatedBy = v);
        }
    }
}