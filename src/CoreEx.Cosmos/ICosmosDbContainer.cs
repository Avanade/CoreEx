// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Enables the entity and model <see cref="Microsoft.Azure.Cosmos.Container"/> capabilities.
    /// </summary>
    public interface ICosmosDbContainer : ICosmosDbContainerCore
    {
        /// <summary>
        /// Gets the underlying entity <see cref="Type"/>.
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Gets the underlying Cosmos model <see cref="Type"/>.
        /// </summary>
        Type ModelType { get; }

        /// <summary>
        /// Gets the underlying Cosmos model <see cref="CosmosDbValue{TModel}"/> <see cref="Type"/>.
        /// </summary>
        Type ModelValueType { get; }

        /// <summary>
        /// Indicates whether the <see cref="ModelType"/> is encapsulated within a <see cref="CosmosDbValue{TModel}"/>.
        /// </summary>
        bool IsCosmosDbValueModel { get; }

        /// <summary>
        /// Checks whether the <paramref name="model"/> is in a valid state for the operation.
        /// </summary>
        /// <param name="model">The model value (also depends on <see cref="IsCosmosDbValueModel"/>).</param>
        /// <param name="args">The specific <see cref="CosmosDbArgs"/> for the operation.</param>
        /// <param name="checkAuthorized">Indicates whether an additional authorization check should be performed against the <paramref name="model"/>.</param>
        /// <returns><c>true</c> indicates that the model is in a valid state; otherwise, <c>false</c>.</returns>
        bool IsModelValid(object? model, CosmosDbArgs args, bool checkAuthorized);

        /// <summary>
        /// Maps the model into the entity value.
        /// </summary>
        /// <param name="model">The model value (also depends on <see cref="IsCosmosDbValueModel"/>).</param>
        /// <returns>The entity value.</returns>
        object? MapToValue(object? model);
    }
}