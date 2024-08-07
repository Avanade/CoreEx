// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.Azure.Cosmos;
using System;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Provides the base <see cref="Container"/> capabilities.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="Type"/> itself.</typeparam>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
    /// <param name="dbArgs">The optional <see cref="CosmosDbArgs"/>.</param>
    public abstract class CosmosDbModelContainerBase<TModel, TSelf>(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null) : CosmosDbContainer(cosmosDb, containerId, dbArgs), ICosmosDbModelContainer<TModel>
        where TModel : class, IEntityKey, new () where TSelf : CosmosDbModelContainerBase<TModel, TSelf>
    {
        /// <inheritdoc/>
        bool ICosmosDbModelContainer.IsModelValid(object? model, CoreEx.Cosmos.CosmosDbArgs args, bool checkAuthorized) => IsModelValid(model, args, checkAuthorized);

        /// <summary>
        /// Checks whether the <paramref name="model"/> is in a valid state for the operation.
        /// </summary>
        /// <param name="model">The model to be checked.</param>
        /// <param name="args">The specific <see cref="CosmosDbArgs"/> for the operation.</param>
        /// <param name="checkAuthorized">Indicates whether an additional authorization check should be performed against the <paramref name="model"/>.</param>
        /// <returns><c>true</c> indicates that the model is in a valid state; otherwise, <c>false</c>.</returns>
        protected abstract bool IsModelValid(object? model, CosmosDbArgs args, bool checkAuthorized);
    }
}