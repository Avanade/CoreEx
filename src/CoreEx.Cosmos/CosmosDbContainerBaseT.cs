// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides base <see cref="Container"/> operations for a <see cref="CosmosDb"/> container.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="Type"/> itself.</typeparam>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
    /// <param name="dbArgs">The optional <see cref="CosmosDbArgs"/>.</param>
    public abstract class CosmosDbContainerBase<T, TModel, TSelf>(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null) : CosmosDbContainer(cosmosDb, containerId, dbArgs), ICosmosDbContainer<T, TModel>
        where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() where TSelf : CosmosDbContainerBase<T, TModel, TSelf>
    {
        /// <inheritdoc/>
        Type ICosmosDbContainer.EntityType => typeof(T);

        /// <inheritdoc/>
        Type ICosmosDbContainer.ModelType => typeof(TModel);

        /// <inheritdoc/>
        Type ICosmosDbContainer.ModelValueType => typeof(CosmosDbValue<TModel>);

        /// <inheritdoc/>
        bool ICosmosDbContainer.IsCosmosDbValueModel => IsCosmosDbValueModel;

        /// <summary>
        /// Indicates whether the <typeparamref name="TModel"/> is encapsulated within a <see cref="CosmosDbValue{TModel}"/>.
        /// </summary>
        protected bool IsCosmosDbValueModel { get; set; } = false;

        /// <inheritdoc/>
        bool ICosmosDbContainer.IsModelValid(object? model, CoreEx.Cosmos.CosmosDbArgs args, bool checkAuthorized) => IsModelValid(model, args, checkAuthorized);

        /// <summary>
        /// Checks whether the <paramref name="model"/> is in a valid state for the operation.
        /// </summary>
        /// <param name="model">The model value (also depends on <see cref="IsCosmosDbValueModel"/>).</param>
        /// <param name="args">The specific <see cref="CosmosDbArgs"/> for the operation.</param>
        /// <param name="checkAuthorized">Indicates whether an additional authorization check should be performed against the <paramref name="model"/>.</param>
        /// <returns><c>true</c> indicates that the model is in a valid state; otherwise, <c>false</c>.</returns>
        protected abstract bool IsModelValid(object? model, CosmosDbArgs args, bool checkAuthorized);

        /// <inheritdoc/>
        object? ICosmosDbContainer.MapToValue(object? model) => MapToValue(model);

        /// <summary>
        /// Maps the model into the entity value.
        /// </summary>
        /// <param name="model">The model value (also depends on <see cref="IsCosmosDbValueModel"/>).</param>
        /// <returns>The entity value.</returns>
        protected abstract T? MapToValue(object? model);

        /// <summary>
        /// Gets the <b>CosmosDb</b> identifier from the <paramref name="value"/> <see cref="IEntityKey.EntityKey"/>.
        /// </summary>
        /// <param name="value">The entity value.</param>
        /// <returns>The <b>CosmosDb</b> identifier.</returns>
        public string GetCosmosId(T value) => GetCosmosId(value.ThrowIfNull(nameof(value)).EntityKey);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<T?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => await GetWithResultAsync(key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetWithResultAsync(new CosmosDbArgs(DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<T?> GetAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => await GetWithResultAsync(key, partitionKey, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => GetWithResultAsync(new CosmosDbArgs(DbArgs, partitionKey), key, cancellationToken);

        /// <inheritdoc/>
        public async Task<T?> GetAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => await GetWithResultAsync(dbArgs, key, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc/>
        public abstract Task<Result<T?>> GetWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public async Task<T> CreateAsync(T value, CancellationToken cancellationToken = default) => await CreateWithResultAsync(value, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Creates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<T>> CreateWithResultAsync(T value, CancellationToken cancellationToken = default) => CreateWithResultAsync(new CosmosDbArgs(DbArgs), value, cancellationToken);

        /// <inheritdoc/>
        public async Task<T> CreateAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) => await CreateWithResultAsync(dbArgs, value, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc/>
        public abstract Task<Result<T>> CreateWithResultAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public async Task<T> UpdateAsync(T value, CancellationToken cancellationToken = default) => await UpdateWithResultAsync(value, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Updates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<Result<T>> UpdateWithResultAsync(T value, CancellationToken cancellationToken = default) => UpdateWithResultAsync(new CosmosDbArgs(DbArgs), value, cancellationToken);

        /// <inheritdoc/>
        public async Task<T> UpdateAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) => await UpdateWithResultAsync(dbArgs, value, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc/>
        public abstract Task<Result<T>> UpdateWithResultAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync(CompositeKey key, CancellationToken cancellationToken = default) => (await DeleteWithResultAsync(key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => DeleteWithResultAsync(new CosmosDbArgs(DbArgs), key, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => (await DeleteWithResultAsync(key, partitionKey, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => DeleteWithResultAsync(new CosmosDbArgs(DbArgs, partitionKey), key, cancellationToken);

        /// <inheritdoc/>
        public async Task DeleteAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => (await DeleteWithResultAsync(dbArgs, key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <inheritdoc/>
        public abstract Task<Result> DeleteWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default);
    }
}