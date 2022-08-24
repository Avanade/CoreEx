// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Azure.Cosmos;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Enables the common <see cref="Microsoft.Azure.Cosmos.Container"/> capabilities.
    /// </summary>
    public interface ICosmosDbContainer
    {
        /// <summary>
        /// Gets the owning <see cref="ICosmosDb"/>.
        /// </summary>
        ICosmosDb CosmosDb { get; }

        /// <summary>
        /// Gets the <see cref="CosmosDbArgs"/>.
        /// </summary>
        CosmosDbArgs? DbArgs { get; }

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.Container"/>.
        /// </summary>
        Container Container { get; }
    }
}