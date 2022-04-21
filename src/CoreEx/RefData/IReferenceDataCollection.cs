// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides <see cref="GetById(object?)"/> and <see cref="GetByCode(string?)"/> functionality for an <see cref="IReferenceData"/> collection.
    /// </summary>
    public interface IReferenceDataCollection
    {
        /// <summary>
        /// Gets the <see cref="IReferenceData"/> for the specified <see cref="IIdentifier.Id"/>.
        /// </summary>
        /// <param name="id">The specified reference data <see cref="IIdentifier.Id"/>.</param>
        /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <c>null</c>.</returns>
        IReferenceData? GetById(object? id);

        /// <summary>
        /// Gets the<see cref="IReferenceData"/> for the specified <see cref="IReferenceData.Code"/>.
        /// </summary>
        /// <param name="code">The specified <see cref="IReferenceData.Code"/>.</param>
        /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <c>null</c>.</returns>
        IReferenceData? GetByCode(string? code);
    }
}