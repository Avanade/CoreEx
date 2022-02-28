// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx
{
    /// <summary>
    /// Represents a change log audit.
    /// </summary>
    public class ChangeLog
    {
        /// <summary>
        /// Gets or sets the created <see cref="DateTime"/>.
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the created by (username).
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the updated <see cref="DateTime"/>.
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets the updated by (username).
        /// </summary>
        public string? UpdatedBy { get; set; }
    }
}