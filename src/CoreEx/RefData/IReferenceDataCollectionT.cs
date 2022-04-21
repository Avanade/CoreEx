// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides <see cref="GetById(T?)"/> functionality for an <see cref="IReferenceData"/> collection with a typed <see cref="IIdentifier{T}.Id"/>.
    /// </summary>
    public interface IReferenceDataCollection<T> : IReferenceDataCollection
    {
        IReferenceData? IReferenceDataCollection.GetById(object? id) => GetById((T?)id);

        /// <summary>
        /// Gets the <see cref="IReferenceData"/> for the specified <see cref="IIdentifier.Id"/>.
        /// </summary>
        /// <param name="id">The specified reference data <see cref="IIdentifier.Id"/>.</param>
        /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <c>null</c>.</returns>
        IReferenceData? GetById(T? id);
    }
}