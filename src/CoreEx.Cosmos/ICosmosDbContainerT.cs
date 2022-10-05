// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Enables the common <see cref="Container"/> operations.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public interface ICosmosDbContainer<T, TModel> : ICosmosDbContainer where T : class, new() where TModel : class, IIdentifier, new()
    {
        /// <summary>
        /// Gets the <see cref="PartitionKey"/>.
        /// </summary>
        /// <param name="value">The value to infer <see cref="PartitionKey"/> from.</param>
        /// <returns>The <see cref="PartitionKey"/>.</returns>
        PartitionKey GetPartitionKey(T value);

        /// <summary>
        /// Gets the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="ICosmosDb.PartitionKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        Task<T?> GetAsync(string id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        Task<T> CreateAsync(T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        Task<T> UpdateAsync(T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="ICosmosDb.PartitionKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        Task DeleteAsync(string id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default);
    }
}