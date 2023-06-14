// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides <see cref="CosmosDb"/> model-only container.
    /// </summary>
    public class CosmosDbModelContainer : ICosmosDbContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbModelContainer"/> class.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="container">The <see cref="Microsoft.Azure.Cosmos.Container"/>.</param>
        internal CosmosDbModelContainer(ICosmosDb cosmosDb, Microsoft.Azure.Cosmos.Container container)
        {
            CosmosDb = cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb));
            Container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <inheritdoc/>
        public ICosmosDb CosmosDb { get; }

        /// <inheritdoc/>
        public Microsoft.Azure.Cosmos.Container Container { get; }
    }
}