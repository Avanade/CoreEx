// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="IChangeLog"/> audit properties.
    /// </summary>
    public interface IChangeLogAudit
    {
        /// <summary>
        /// Gets or sets the created <see cref="DateTime"/>.
        /// </summary>
        DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the created by (username).
        /// </summary>
        string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the updated <see cref="DateTime"/>.
        /// </summary>
        DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets the updated by (username).
        /// </summary>
        string? UpdatedBy { get; set; }

        /// <summary>
        /// Indicates whether all the properties for the <see cref="IChangeLogAudit"/> are initial (default).
        /// </summary>
        public bool IsInitial => CreatedDate == default && CreatedBy == default && UpdatedDate == default && UpdatedBy == default;
    }
}