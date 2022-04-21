// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the extended <b>Reference Data</b> properties.
    /// </summary>
    public interface IReferenceDataExtended : IReferenceData
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        string? Description { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        int SortOrder { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="IReferenceDataExtended"/> is active.
        /// </summary>
        /// <value><c>true</c> where active; otherwise, <c>false</c>.</value>
        bool IsActive { get; set; }

        /// <summary>
        /// Gets of sets the validity start date.
        /// </summary>
        DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets of sets the validity end date.
        /// </summary>
        DateTime? EndDate { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="IReferenceDataExtended"/> is known and in a valid state.
        /// </summary>
        bool IsValid { get; }
    }
}