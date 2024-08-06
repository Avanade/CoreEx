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
        bool IsCosmosDbValueEncapsulated { get; }

        /// <summary>
        /// Maps the model into the entity value.
        /// </summary>
        /// <param name="model">The model value (also depends on <see cref="IsCosmosDbValueEncapsulated"/>).</param>
        /// <returns>The entity value.</returns>
        object? MapToValue(object? model);
    }
}