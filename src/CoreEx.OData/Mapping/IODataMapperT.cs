// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;

namespace CoreEx.OData.Mapping
{
    /// <summary>
    /// Defines an <see cref="ODataItem"/> mapper.
    /// </summary>
    /// <typeparam name="TSource">The <see cref="IODataMapper.SourceType"/>.</typeparam>
    public interface IODataMapper<TSource> : IODataMapper
    {
        /// <inheritdoc/>
        Type IODataMapper.SourceType => typeof(TSource);

        /// <inheritdoc/>
        object? IODataMapper.MapFromOData(ODataItem entity, OperationTypes operationType) => MapFromOData(entity, operationType)!;

        /// <inheritdoc/>
        void IODataMapper.MapToOData(object? value, ODataItem entity, OperationTypes operationType) => MapToOData((TSource?)value, entity, operationType);

        /// <summary>
        /// Maps from a <paramref name="entity"/> creating a corresponding instance of <typeparamref name="TSource"/>.
        /// </summary>
        /// <param name="entity">The <see cref="ODataItem"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of <typeparamref name="TSource"/>.</returns>
        new TSource? MapFromOData(ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="ODataItem"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToOData(TSource? value, ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Gets the OData primary key from the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The primary key.</returns>
        object[] GetODataKey(TSource value, OperationTypes operationType = OperationTypes.Unspecified);
    }
}