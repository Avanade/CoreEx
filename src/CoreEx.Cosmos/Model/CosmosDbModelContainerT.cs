// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Provides a typed interface for the primary <see cref="CosmosDbModelContainer"/> operations.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public class CosmosDbModelContainer<TModel> where TModel : class, IEntityKey, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbModelContainer{TModel}"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="CosmosDbContainer"/>.</param>
        internal CosmosDbModelContainer(CosmosDbContainer owner) => Owner = owner.ThrowIfNull(nameof(owner));

        /// <summary>
        /// Gets the owning <see cref="CosmosDbContainer"/>.
        /// </summary>
        public CosmosDbContainer Owner { get; }

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.Container"/>.
        /// </summary>
        public Container CosmosContainer => Owner.CosmosContainer;

        /// <summary>
        /// Sets the function to get the <see cref="PartitionKey"/> from the model used by the <see cref="CosmosDbModelContainer.GetPartitionKey{TModel}(TModel, CosmosDbArgs)"/> (used by only by the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="getPartitionKey">The function to get the <see cref="PartitionKey"/> from the model.</param>
        /// <remarks>This can only be set once; otherwise, a <see cref="InvalidOperationException"/> will be thrown.</remarks>
        public CosmosDbModelContainer<TModel> UsePartitionKey(Func<TModel, PartitionKey>? getPartitionKey)
        {
            Owner.Model.UsePartitionKey(getPartitionKey);
            return this;
        }

        /// <summary>
        /// Sets the name for the model <see cref="Type"/>.
        /// </summary>
        /// <param name="name">The model name.</param>
        public CosmosDbModelContainer<TModel> UseModelName(string name)
        {
            Owner.Model.UseModelName<TModel>(name);
            return this;
        }

        /// <summary>
        /// Sets the filter for all operations performed on the <typeparamref name="TModel"/> to ensure authorisation is applied. Applies automatically to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <param name="filter">The authorization filter query.</param>
        public CosmosDbModelContainer<TModel> UseAuthorizeFilter(Func<IQueryable<TModel>, IQueryable<TModel>>? filter)
        {
            Owner.Model.UseAuthorizeFilter(filter);
            return this;
        }

        #region Query

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> Query(Func<IQueryable<TModel>, IQueryable<TModel>>? query) => Owner.Model.Query<TModel>(query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> Query(PartitionKey? partitionKey = null, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => Owner.Model.Query<TModel>(partitionKey, query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> Query(CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => Owner.Model.Query<TModel>(dbArgs, query);

        #endregion

        #region Get

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<TModel?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.GetAsync<TModel>(key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<TModel?>> GetWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.GetWithResultAsync<TModel>(key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<TModel?> GetAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Owner.Model.GetAsync<TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<TModel?>> GetWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Owner.Model.GetWithResultAsync<TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<TModel?> GetAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.GetAsync<TModel>(dbArgs, key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<TModel?>> GetWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.GetWithResultAsync<TModel>(dbArgs, key, cancellationToken);

        #endregion

        #region Create

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<TModel> CreateAsync(TModel value, CancellationToken cancellationToken = default) => Owner.Model.CreateAsync<TModel>(value, cancellationToken);

        /// <summary>
        /// Creates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<TModel>> CreateWithResultAsync(TModel value, CancellationToken cancellationToken = default) => Owner.Model.CreateWithResultAsync<TModel>(value, cancellationToken);

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<TModel> CreateAsync(CosmosDbArgs dbArgs, TModel value, CancellationToken cancellationToken = default) => Owner.Model.CreateAsync<TModel>(dbArgs, value, cancellationToken);

        /// <summary>
        /// Creates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<TModel>> CreateWithResultAsync(CosmosDbArgs dbArgs, TModel value, CancellationToken cancellationToken = default) => Owner.Model.CreateWithResultAsync<TModel>(dbArgs, value, cancellationToken);

        #endregion

        #region Update

        /// <summary>
        /// Updates the model.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<TModel> UpdateAsync(TModel value, CancellationToken cancellationToken = default) => Owner.Model.UpdateAsync<TModel>(value, cancellationToken);

        /// <summary>
        /// Updates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<Result<TModel>> UpdateWithResultAsync(TModel value, CancellationToken cancellationToken = default) => Owner.Model.UpdateWithResultAsync<TModel>(value, cancellationToken);

        /// <summary>
        /// Updates the model.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<TModel> UpdateAsync(CosmosDbArgs dbArgs, TModel value, CancellationToken cancellationToken = default) => Owner.Model.UpdateAsync<TModel>(dbArgs, value, cancellationToken);

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.DeleteAsync<TModel>(key, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.DeleteWithResultAsync<TModel>(key, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Owner.Model.DeleteAsync<TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Owner.Model.DeleteWithResultAsync<TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.DeleteAsync<TModel>(dbArgs, key, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.DeleteWithResultAsync<TModel>(dbArgs, key, cancellationToken);

        #endregion
    }
}