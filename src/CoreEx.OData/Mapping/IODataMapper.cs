// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;

namespace CoreEx.OData.Mapping
{
    /// <summary>
    /// Defines an <see cref="ODataItem"/> mapper.
    /// </summary>
    public interface IODataMapper
    {
        /// <summary>
        /// Gets the source <see cref="Type"/> being mapped from/to the <see cref="ODataItem"/>.
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Maps from an <paramref name="entity"/> creating a corresponding instance of the <see cref="SourceType"/>.
        /// </summary>
        /// <param name="entity">The <see cref="ODataItem"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of the <see cref="SourceType"/>.</returns>
        object? MapFromOData(ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="ODataItem"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToOData(object? value, ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified);
    }
}