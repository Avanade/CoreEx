// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Cosmos.Model;
using CoreEx.Entities;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides a typed interface for the primary <see cref="CosmosDbContainer"/> operations.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public sealed class CosmosDbContainer<T, TModel> where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
    {
        private CosmosDbModelContainer<TModel>? _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbContainer{T, TModel}"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="CosmosDbContainer"/>.</param>
        internal CosmosDbContainer(CosmosDbContainer owner)
        {
            Container = owner.ThrowIfNull(nameof(owner));
            CosmosContainer = Container.CosmosContainer;
        }

        /// <summary>
        /// Gets the owning <see cref="CosmosDbContainer"/>.
        /// </summary>
        public CosmosDbContainer Container { get; }

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.Container"/>.
        /// </summary>
        public Container CosmosContainer { get; }

        /// <summary>
        /// Gets the typed <see cref="CosmosDbModelContainer{TModel}"/>.
        /// </summary>
        public CosmosDbModelContainer<TModel> Model => _model ??= new(Container);

        /// <summary>
        /// Sets the function to get the <see cref="PartitionKey"/> from the model used by the <see cref="CosmosDbModelContainer.GetPartitionKey{TModel}(TModel, CosmosDbArgs)"/> (used by only by the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="getPartitionKey">The function to get the <see cref="PartitionKey"/> from the model.</param>
        /// <remarks>Only sets on first execution; otherwise, ignored.</remarks>
        public CosmosDbContainer<T, TModel> UsePartitionKey(Func<TModel, PartitionKey>? getPartitionKey)
        {
            Container.UsePartitionKey(getPartitionKey);
            return this;
        }

        /// <summary>
        /// Sets the filter for all operations performed on the <typeparamref name="TModel"/> to ensure authorisation is applied. Applies automatically to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <param name="filter">The authorization filter query.</param>
        public CosmosDbContainer<T, TModel> UseAuthorizeFilter(Func<IQueryable<TModel>, IQueryable<TModel>>? filter)
        {
            Container.UseAuthorizeFilter(filter);
            return this;
        }

        #region Query

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(Func<IQueryable<TModel>, IQueryable<TModel>>? query) => Container.Query<T, TModel>(query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(PartitionKey? partitionKey = null, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => Container.Query<T, TModel>(partitionKey, query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => Container.Query<T, TModel>(dbArgs, query);

        #endregion

        #region Get

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<T?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => Container.GetAsync<T, TModel>(key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => Container.GetWithResultAsync<T, TModel>(key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<T?> GetAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Container.GetAsync<T, TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Container.GetWithResultAsync<T, TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<T?> GetAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Container.GetAsync<T, TModel>(dbArgs, key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Container.GetWithResultAsync<T, TModel>(dbArgs, key, cancellationToken);

        #endregion

        #region Create

        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<T> CreateAsync(T value, CancellationToken cancellationToken = default) => Container.CreateAsync<T, TModel>(value, cancellationToken);

        /// <summary>
        /// Creates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<T>> CreateWithResultAsync(T value, CancellationToken cancellationToken = default) => Container.CreateWithResultAsync<T, TModel>(value, cancellationToken);

        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<T> CreateAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) => Container.CreateAsync<T, TModel>(dbArgs, value, cancellationToken);

        /// <summary>
        /// Creates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<T>> CreateWithResultAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) => Container.CreateWithResultAsync<T, TModel>(dbArgs, value, cancellationToken);

        #endregion

        #region Update

        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<T> UpdateAsync(T value, CancellationToken cancellationToken = default) => Container.UpdateAsync<T, TModel>(value, cancellationToken);

        /// <summary>
        /// Updates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<Result<T>> UpdateWithResultAsync(T value, CancellationToken cancellationToken = default) => Container.UpdateWithResultAsync<T, TModel>(value, cancellationToken);

        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<T> UpdateAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) => Container.UpdateAsync<T, TModel>(dbArgs, value, cancellationToken);

        /// <summary>
        /// Updates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<Result<T>> UpdateWithResultAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) => Container.UpdateWithResultAsync<T, TModel>(dbArgs, value, cancellationToken);

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CompositeKey key, CancellationToken cancellationToken = default) => Container.DeleteAsync<T, TModel>(key, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => Container.DeleteWithResultAsync<T, TModel>(key, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Container.DeleteAsync<T, TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Container.DeleteWithResultAsync<T, TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Container.DeleteAsync<T, TModel>(dbArgs, key, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Container.DeleteWithResultAsync<T, TModel>(dbArgs, key, cancellationToken);

        #endregion
    }
}