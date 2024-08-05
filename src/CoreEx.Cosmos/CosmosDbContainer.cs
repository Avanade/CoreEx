// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.Azure.Cosmos;
using System;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides the core <see cref="Container"/> capabilities.
    /// </summary>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
    /// <param name="dbArgs">The optional <see cref="CosmosDbArgs"/>.</param>
    public class CosmosDbContainer(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null) : ICosmosDbContainer
    {
        private CosmosDbArgs? _dbArgs = dbArgs;

        /// <inheritdoc/>
        public ICosmosDb CosmosDb { get; } = cosmosDb.ThrowIfNull(nameof(cosmosDb));

        /// <inheritdoc/>
        public Container Container { get; } = cosmosDb.GetCosmosContainer(containerId);

        /// <summary>
        /// Gets or sets the Container-specific <see cref="CosmosDbArgs"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="ICosmosDb.DbArgs"/> on first access.</remarks>
        public CosmosDbArgs DbArgs
        {
            get => _dbArgs ??= new CosmosDbArgs(CosmosDb.DbArgs);
            set => _dbArgs = value;
        }

        /// <summary>
        /// Gets the <b>CosmosDb</b> identifier from the <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns>The <b>CosmosDb</b> identifier.</returns>
        /// <remarks>Uses the <see cref="CosmosDbArgs.FormatIdentifier"/> to format the <paramref name="key"/> as a string (as required).</remarks>
        public virtual string GetCosmosId(CompositeKey key) => DbArgs.FormatIdentifier(key) ?? throw new InvalidOperationException("The CompositeKey formatting must not result in a null.");
    }
}