// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Azure.Cosmos;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Enables the model-only <see cref="Container"/>.
    /// </summary>
    public interface ICosmosDbModelContainer : ICosmosDbContainerCore 
    {
        /// <summary>
        /// Checks whether the <paramref name="model"/> is in a valid state for the operation.
        /// </summary>
        /// <param name="model">The model to be checked.</param>
        /// <param name="args">The specific <see cref="CosmosDbArgs"/> for the operation.</param>
        /// <param name="checkAuthorized">Indicates whether an additional authorization check should be performed against the <paramref name="model"/>.</param>
        /// <returns><c>true</c> indicates that the model is in a valid state; otherwise, <c>false</c>.</returns>
        bool IsModelValid(object? model, CosmosDbArgs args, bool checkAuthorized);
    }
}