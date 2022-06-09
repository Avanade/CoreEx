// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.RefData
{
    /// <summary>
    /// Enables the base capabilities for a special purpose <see cref="IReferenceData"/> collection specifically for managing a referenced list of <i>serialization identifiers</i> being the underlying <see cref="IReferenceData.Code"/>.
    /// </summary>
    public interface IReferenceDataCodeList
    {
        /// <summary>
        /// Indicates whether the collection contains invalid items (i.e. not <see cref="IReferenceData.IsValid"/>).
        /// </summary>
        /// <returns><c>true</c> indicates that invalid items exist; otherwise, <c>false</c>.</returns>
        bool HasInvalidItems { get; }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the underlying <see cref="IReferenceData"/> list.
        /// </summary>
        /// <returns>The underlying <see cref="IReferenceData"/> list.</returns>
        List<IReferenceData> ToRefDataList();
    }
}