// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using Microsoft.Xrm.Sdk;
using System;

namespace CoreEx.Dataverse
{
    /// <summary>
    /// Defines a <i>Dataverse</i> mapper.
    /// </summary>
    public interface IDataverseMapper
    {
        /// <summary>
        /// Gets the source <see cref="Type"/> being mapped from/to the <i>Dataverse</i> <see cref="Entity"/>.
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Maps from an <paramref name="entity"/> creating a corresponding instance of the <see cref="SourceType"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of the <see cref="SourceType"/>.</returns>
        object? MapFromDataverse(Entity entity, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="Entity"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToDataverse(object? value, Entity entity, OperationTypes operationType = OperationTypes.Unspecified);
    }
}