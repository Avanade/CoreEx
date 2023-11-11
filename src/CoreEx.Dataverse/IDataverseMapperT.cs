// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using Microsoft.Xrm.Sdk;
using System;

namespace CoreEx.Dataverse
{
    /// <summary>
    /// Defines a <i>Dataverse</i> mapper.
    /// </summary>
    /// <typeparam name="TSource">The <see cref="IDataverseMapper.SourceType"/>.</typeparam>
    public interface IDataverseMapper<TSource> : IDataverseMapper
    {
        /// <inheritdoc/>
        Type IDataverseMapper.SourceType => typeof(TSource);

        /// <inheritdoc/>
        object? IDataverseMapper.MapFromDataverse(Entity entity, OperationTypes operationType) => MapFromDataverse(entity, operationType)!;

        /// <inheritdoc/>
        void IDataverseMapper.MapToDataverse(object? value, Entity entity, OperationTypes operationType) => MapToDataverse((TSource?)value, entity, operationType);

        /// <summary>
        /// Maps from a <paramref name="entity"/> creating a corresponding instance of <typeparamref name="TSource"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of <typeparamref name="TSource"/>.</returns>
        new TSource? MapFromDataverse(Entity entity, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="Entity"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToDataverse(TSource? value, Entity entity, OperationTypes operationType = OperationTypes.Unspecified);
    }
}