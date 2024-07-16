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
    public abstract class CosmosDbModelContainerBase<TModel, TSelf>(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null) : CosmosDbContainerBase<TSelf>(cosmosDb, containerId, dbArgs), ICosmosDbModelContainer<TModel>
        where TModel : class, IEntityKey, new () where TSelf : CosmosDbModelContainerBase<TModel, TSelf> { }
}