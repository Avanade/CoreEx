// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Cosmos.Model;
using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Stj = System.Text.Json;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides <see cref="CosmosDb"/> <see cref="Container"/> capabilities.
    /// </summary>
    /// <remarks>The <see cref="Model"/> property (<see cref="CosmosDbModelContainer"/>) provides the underlying capabilities for direct model-based access.</remarks>
    public partial class CosmosDbContainer
    {
        private readonly Lazy<CosmosDbModelContainer> _model;
        private Func<CosmosDbArgs>? _dbArgsFactory;
        private readonly ConcurrentDictionary<(Type, Type), object> _containers = new();
        private readonly ConcurrentDictionary<(Type, Type), object> _valueContainers = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbContainer"/>.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container"/> identifier.</param>
        public CosmosDbContainer(ICosmosDb cosmosDb, string containerId)
        {
            CosmosDb = cosmosDb.ThrowIfNull(nameof(cosmosDb));
            CosmosContainer = cosmosDb.GetCosmosContainer(containerId.ThrowIfNullOrEmpty(nameof(containerId)));
            _model = new(() => new(this));
        }

        /// <summary>
        /// Gets the owning <see cref="ICosmosDb"/>.
        /// </summary>
        public ICosmosDb CosmosDb { get; }

        /// <summary>
        /// Gets the <see cref="Container"/>.
        /// </summary>
        public Container CosmosContainer { get; }

        /// <summary>
        /// Gets the Container-specific <see cref="CosmosDbArgs"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="UseDbArgs(Func{CosmosDbArgs})"/>; otherwise, <see cref="CosmosDb.DbArgs"/>.</remarks>
        public CosmosDbArgs DbArgs => _dbArgsFactory?.Invoke() ?? new CosmosDbArgs(CosmosDb.DbArgs);

        /// <summary>
        /// Sets the container-specific <see cref="CosmosDbArgs"/>.
        /// </summary>
        /// <param name="dbArgsFactory">The <see cref="CosmosDbArgs"/> creation factory.</param>
        /// <remarks>This can only be set once; otherwise, a <see cref="InvalidOperationException"/> will be thrown.</remarks>
        public CosmosDbContainer UseDbArgs(Func<CosmosDbArgs> dbArgsFactory)
        {
            dbArgsFactory.ThrowIfNull(nameof(dbArgsFactory));
            if (_dbArgsFactory is not null)
                throw new InvalidOperationException($"The {nameof(UseDbArgs)} can only be specified once.");

            _dbArgsFactory = dbArgsFactory;
            return this;
        }

        /// <summary>
        /// Gets the <see cref="CosmosDbModelContainer"/> that encapsulates the direct-to-model operations.
        /// </summary>
        public CosmosDbModelContainer Model => _model.Value;

        /// <summary>
        /// Gets or sets the SQL statement format for the <b>MultiSet</b> operation.
        /// </summary>
        /// <remarks>The SQL statement format must have the <c>{0}</c>> place holder for the list of types represented as comma-separated strings; e.g. <c>"Customer", "Address"</c>.</remarks>
        public string MultiSetSqlStatementFormat { get; private set; } = "SELECT * FROM c WHERE c.type in ({0})";

        /// <summary>
        /// Sets the <see cref="MultiSetSqlStatementFormat"/> for the <b>MultiSet</b> operations.
        /// </summary>
        /// <param name="format">The SQL statement format.</param>
        public CosmosDbContainer UseMultiSetSqlStatement(string format)
        {
            if (!format.ThrowIfNullOrEmpty(nameof(format)).Contains("{0}"))
                throw new ArgumentException("The format must contain '{0}' to insert the 'in' list (contents).", nameof(format));

            return this;
        }

        /// <summary>
        /// Gets the <b>CosmosDb</b> identifier from the <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns>The <b>CosmosDb</b> identifier.</returns>
        /// <remarks>Uses the <see cref="CosmosDbArgs.FormatIdentifier"/> to format the <paramref name="key"/> as a string (as required).</remarks>
        public virtual string GetCosmosId(CompositeKey key) => DbArgs.FormatIdentifier(key) ?? throw new InvalidOperationException("The CompositeKey formatting into an identifier must not result in a null.");

        /// <summary>
        /// Gets the <b>CosmosDb</b> identifier from the <paramref name="model"/> <see cref="IEntityKey.EntityKey"/>.
        /// </summary>
        /// <param name="model">The model value.</param>
        /// <returns>The <b>CosmosDb</b> identifier.</returns>
        public string GetCosmosId<TModel>(TModel model) where TModel : class, IEntityKey => GetCosmosId(model.ThrowIfNull(nameof(model)).EntityKey);

        /// <summary>
        /// Gets the <see cref="PartitionKey"/>.
        /// </summary>
        public PartitionKey GetPartitionKey(PartitionKey? partitionKey) => partitionKey ?? DbArgs.PartitionKey ?? PartitionKey.None;

        /// <summary>
        /// Sets the function to get the <see cref="PartitionKey"/> from the model used by the <see cref="CosmosDbModelContainer.GetPartitionKey{TModel}(TModel, CosmosDbArgs)"/> (used by only by the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="getPartitionKey">The function to get the <see cref="PartitionKey"/> from the model.</param>
        /// <remarks>This can only be set once; otherwise, a <see cref="InvalidOperationException"/> will be thrown.</remarks>
        public CosmosDbContainer UsePartitionKey<TModel>(Func<TModel, PartitionKey>? getPartitionKey) where TModel : class, IEntityKey, new()
        {
            Model.UsePartitionKey(getPartitionKey);
            return this;
        }

        /// <summary>
        /// Sets the function to get the <see cref="PartitionKey"/> from the <see cref="CosmosDbValue{TModel}"/> used by the <see cref="CosmosDbModelContainer.GetPartitionKey{TModel}(CosmosDbValue{TModel}, CosmosDbArgs)"/> (used by only by the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="getPartitionKey">The function to get the <see cref="PartitionKey"/> from the model.</param>
        /// <remarks>This can only be set once; otherwise, a <see cref="InvalidOperationException"/> will be thrown.</remarks>
        public CosmosDbContainer UseValuePartitionKey<TModel>(Func<CosmosDbValue<TModel>, PartitionKey>? getPartitionKey) where TModel : class, IEntityKey, new()
        {
            Model.UsePartitionKey(getPartitionKey);
            return this;
        }

        /// <summary>
        /// Sets (overrides) the name for the model <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="name">The model name.</param>
        public CosmosDbContainer UseModelName<TModel>(string name) where TModel : class, IEntityKey, new()
        {
            Model.UseModelName<TModel>(name);
            return this;
        }

        /// <summary>
        /// Sets the filter for all operations performed on the <typeparamref name="TModel"/> to ensure authorisation is applied. Applies automatically to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="filter">The authorization filter query.</param>
        public CosmosDbContainer UseAuthorizeFilter<TModel>(Func<IQueryable<TModel>, IQueryable<TModel>>? filter) where TModel : class, IEntityKey, new()
        {
            Model.UseAuthorizeFilter(filter);
            return this;
        }

        /// <summary>
        /// Sets the filter for all operations performed on the <see cref="CosmosDbValue{TModel}"/> to ensure authorisation is applied. Applies automatically to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="filter">The authorization filter query.</param>
        public CosmosDbContainer UseValueAuthorizeFilter<TModel>(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? filter) where TModel : class, IEntityKey, new()
        {
            Model.UseAuthorizeFilter(filter);
            return this;
        }

        /// <summary>
        /// Maps <paramref name="model"/> to the entity <b>value</b> formatting/updating any special properties as required.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model value.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <returns>The entity value.</returns>
        [return: NotNullIfNotNull(nameof(model))]
        public T? MapToValue<T, TModel>(TModel? model, CosmosDbArgs dbArgs) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            if (model is null)
                return default;

            // Map the model to the entity value.
            var val = CosmosDb.Mapper.Map<TModel, T>(model, OperationTypes.Get)!;
            if (dbArgs.AutoMapETag && val is IETag et && et.ETag != null)
                et.ETag = ETagGenerator.ParseETag(et.ETag);

            return dbArgs.CleanUpResult ? Cleaner.Clean(val) : val;
        }

        /// <summary>
        /// Maps <paramref name="model"/> to the entity <b>value</b> formatting/updating any special properties as required.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The <see cref="CosmosDbValue{TModel}"/> value.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <returns>The entity value.</returns>
        [return: NotNullIfNotNull(nameof(model))]
        public T? MapToValue<T, TModel>(CosmosDbValue<TModel>? model, CosmosDbArgs dbArgs) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            if (model is null)
                return default;

            ((ICosmosDbValue)model).PrepareAfter(dbArgs);
            var val = CosmosDb.Mapper.Map<TModel, T>(model.Value, OperationTypes.Get)!;
            if (dbArgs.AutoMapETag && val is IETag et)
            {
                if (et.ETag is not null)
                    et.ETag = ETagGenerator.ParseETag(et.ETag);
                else
                    et.ETag = ETagGenerator.ParseETag(model.ETag);
            }

            return DbArgs.CleanUpResult ? Cleaner.Clean(val) : val;
        }

        /// <summary>
        /// Gets (or adds) the typed <see cref="CosmosDbContainer{T, TModel}"/> for the specified <typeparamref name="T"/> and <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="configure">An optional action to perform one-off configuration on initial access.</param>
        /// <returns>The typed <see cref="CosmosDbContainer{T, TModel}"/></returns>
        public CosmosDbContainer<T, TModel> AsTyped<T, TModel>(Action<CosmosDbContainer<T, TModel>>? configure = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => (CosmosDbContainer<T, TModel>)_containers.GetOrAdd((typeof(T), typeof(TModel)), _ =>
            {
                var c = new CosmosDbContainer<T, TModel>(this);
                configure?.Invoke(c);
                return c;
            });

        /// <summary>
        /// Gets (or adds) the typed <see cref="CosmosDbValueContainer{T, TModel}"/> for the specified <typeparamref name="T"/> and <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="configure">An optional action to perform one-off configuration on initial access.</param>
        /// <returns>The typed <see cref="CosmosDbValueContainer{T, TModel}"/></returns>
        public CosmosDbValueContainer<T, TModel> AsValueTyped<T, TModel>(Action<CosmosDbValueContainer<T, TModel>>? configure = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => (CosmosDbValueContainer<T, TModel>)_valueContainers.GetOrAdd((typeof(T), typeof(TModel)), _ =>
            {
                var c = new CosmosDbValueContainer<T, TModel>(this);
                configure?.Invoke(c);
                return c;
            });

        #region Query

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query<T, TModel>(Func<IQueryable<TModel>, IQueryable<TModel>>? query) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => Query<T, TModel>(new CosmosDbArgs(DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query<T, TModel>(PartitionKey? partitionKey = null, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => Query<T, TModel>(new CosmosDbArgs(DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query<T, TModel>(CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => new(this, dbArgs, query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> ValueQuery<T, TModel>(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => ValueQuery<T, TModel>(new CosmosDbArgs(DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> ValueQuery<T, TModel>(PartitionKey? partitionKey = null, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => ValueQuery<T, TModel>(new CosmosDbArgs(DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> ValueQuery<T, TModel>(CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => new(this, dbArgs, query);

        #endregion

        #region Get

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<T?> GetAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await GetWithResultAsync<T, TModel>(key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetWithResultAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => GetWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<T?> GetAsync<T, TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await GetWithResultAsync<T, TModel>(key, partitionKey, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetWithResultAsync<T, TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => GetWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<T?> GetAsync<T, TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await GetWithResultAsync<T, TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<Result<T?>> GetWithResultAsync<T, TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            var result = await Model.GetWithResultAsync<TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(m => MapToValue<T, TModel>(m, dbArgs));
        }

        /// <summary>
        /// Gets the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<T?> GetValueAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await GetValueWithResultAsync<T, TModel>(key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetValueWithResultAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => GetValueWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<T?> GetValueAsync<T, TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await GetValueWithResultAsync<T, TModel>(key, partitionKey, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<T?>> GetValueWithResultAsync<T, TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => GetValueWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Gets the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>..
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<T?> GetValueAsync<T, TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await GetValueWithResultAsync<T, TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<Result<T?>> GetValueWithResultAsync<T, TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            var result = await Model.GetValueWithResultAsync<TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(m => MapToValue<T, TModel>(m, dbArgs));
        }

        #endregion

        #region Create

        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public async Task<T> CreateAsync<T, TModel>(T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await CreateWithResultAsync<T, TModel>(value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<T>> CreateWithResultAsync<T, TModel>(T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => CreateWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs), value, cancellationToken);

        /// <summary>
        /// Creates the entity.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public async Task<T> CreateAsync<T, TModel>(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => (await CreateWithResultAsync<T, TModel>(dbArgs, value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public async Task<Result<T>> CreateWithResultAsync<T, TModel>(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            ChangeLog.PrepareCreated(value.ThrowIfNull(nameof(value)));
            TModel model = CosmosDb.Mapper.Map<T, TModel>(value, OperationTypes.Create)!;

            var result = await Model.CreateWithResultAsync(dbArgs, model, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(model => MapToValue<T, TModel>(model, dbArgs)!);
        }

        /// <summary>
        /// Creates the entity (using underlying <see cref="CosmosDbValue{TModel}"/>).
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public async Task<T> CreateValueAsync<T, TModel>(T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await CreateValueWithResultAsync<T, TModel>(value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<T>> CreateValueWithResultAsync<T, TModel>(T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => CreateValueWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs), value, cancellationToken);

        /// <summary>
        /// Creates the entity (using underlying <see cref="CosmosDbValue{TModel}"/>).
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public async Task<T> CreateValueAsync<T, TModel>(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await CreateValueWithResultAsync<T, TModel>(dbArgs, value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public async Task<Result<T>> CreateValueWithResultAsync<T, TModel>(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            ChangeLog.PrepareCreated(value.ThrowIfNull(nameof(value)));
            TModel model = CosmosDb.Mapper.Map<T, TModel>(value, OperationTypes.Create)!;
            var cdv = new CosmosDbValue<TModel>(Model.GetModelName<TModel>(), model!);

            var result = await Model.CreateValueWithResultAsync(dbArgs, cdv, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(model => MapToValue<T, TModel>(model, dbArgs)!);
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public async Task<T> UpdateAsync<T, TModel>(T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => (await UpdateWithResultAsync<T, TModel>(value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<Result<T>> UpdateWithResultAsync<T, TModel>(T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => UpdateWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs), value, cancellationToken);

        /// <summary>
        /// Updates the entity.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public async Task<T> UpdateAsync<T, TModel>(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => (await UpdateWithResultAsync<T, TModel>(dbArgs, value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public async Task<Result<T>> UpdateWithResultAsync<T, TModel>(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            ChangeLog.PrepareUpdated(value);
            var model = CosmosDb.Mapper.Map<T, TModel>(value.ThrowIfNull(nameof(value)), OperationTypes.Update)!;
            var result = await Model.UpdateWithResultInternalAsync(dbArgs, model, m => CosmosDb.Mapper.Map(value, m, OperationTypes.Update), cancellationToken).ConfigureAwait(false);
            return result.ThenAs(model => MapToValue<T, TModel>(model, dbArgs)!);
        }

        /// <summary>
        /// Updates the entity (using underlying <see cref="CosmosDbValue{TModel}"/>).
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public async Task<T> UpdateValueAsync<T, TModel>(T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await UpdateValueWithResultAsync<T, TModel>(value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<Result<T>> UpdateValueWithResultAsync<T, TModel>(T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => UpdateValueWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs), value, cancellationToken);

        /// <summary>
        /// Updates the entity (using underlying <see cref="CosmosDbValue{TModel}"/>).
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public async Task<T> UpdateValueAsync<T, TModel>(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await UpdateValueWithResultAsync<T, TModel>(dbArgs, value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public async Task<Result<T>> UpdateValueWithResultAsync<T, TModel>(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            ChangeLog.PrepareUpdated(value);
            var model = CosmosDb.Mapper.Map<T, TModel>(value.ThrowIfNull(nameof(value)), OperationTypes.Update)!;
            var cdv = new CosmosDbValue<TModel>(Model.GetModelName<TModel>(), model!);

            var result = await Model.UpdateValueWithResultInternalAsync<TModel>(dbArgs, cdv, cdv => CosmosDb.Mapper.Map(value, cdv.Value, OperationTypes.Update), cancellationToken).ConfigureAwait(false);
            return result.ThenAs(model => MapToValue<T, TModel>(model, dbArgs)!);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => (await DeleteWithResultAsync<T, TModel>(key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => DeleteWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs), key, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync<T, TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => (await DeleteWithResultAsync<T, TModel>(key, partitionKey, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync<T, TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => DeleteWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync<T, TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() 
            => (await DeleteWithResultAsync<T, TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync<T, TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => Model.DeleteWithResultAsync<TModel>(dbArgs, key, cancellationToken);

        /// <summary>
        /// Deletes the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteValueAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await DeleteValueWithResultAsync<T, TModel>(key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteValueWithResultAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => DeleteValueWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs), key, cancellationToken);

        /// <summary>
        /// Deletes the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteValueAsync<T, TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await DeleteValueWithResultAsync<T, TModel>(key, partitionKey, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteValueWithResultAsync<T, TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => DeleteValueWithResultAsync<T, TModel>(new CosmosDbArgs(DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Deletes the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteValueAsync<T, TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => (await DeleteValueWithResultAsync<T, TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteValueWithResultAsync<T, TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => Model.DeleteValueWithResultAsync<TModel>(dbArgs, key, cancellationToken);

        #endregion

        #region MultiSet

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public Task SelectValueMultiSetAsync(PartitionKey partitionKey, params IMultiSetValueArgs[] multiSetArgs) => SelectValueMultiSetAsync(partitionKey, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public Task SelectValueMultiSetAsync(PartitionKey partitionKey, IEnumerable<IMultiSetValueArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectValueMultiSetAsync(partitionKey, null, multiSetArgs, cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="sql">The override SQL statement; will default where not specified.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public async Task SelectValueMultiSetAsync(PartitionKey partitionKey, string? sql, IEnumerable<IMultiSetValueArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => (await SelectValueMultiSetWithResultAsync(partitionKey, sql, multiSetArgs, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public Task<Result> SelectValueMultiSetWithResultAsync(PartitionKey partitionKey, params IMultiSetValueArgs[] multiSetArgs) => SelectValueMultiSetWithResultAsync(partitionKey, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public Task<Result> SelectValueMultiSetWithResultAsync(PartitionKey partitionKey, IEnumerable<IMultiSetValueArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectValueMultiSetWithResultAsync(partitionKey, null, multiSetArgs, cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="sql">The override SQL statement; will default to <see cref="MultiSetSqlStatementFormat"/> where not specified.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The <paramref name="multiSetArgs"/> must be of type <see cref="CosmosDbValue{TModel}"/>. Each <paramref name="multiSetArgs"/> is verified and executed in the order specified.
        /// <para>The underlying SQL will be automatically created from the specified <paramref name="multiSetArgs"/> where not explicitly supplied. Essentially, it is a simple query where all <i>types</i> inferred from the <paramref name="multiSetArgs"/>
        /// are included, for example: <c>SELECT * FROM c WHERE c.type in ("TypeNameA", "TypeNameB")</c></para>
        /// </remarks>
        public async Task<Result> SelectValueMultiSetWithResultAsync(PartitionKey partitionKey, string? sql, IEnumerable<IMultiSetValueArgs> multiSetArgs, CancellationToken cancellationToken = default)
        {
            // Verify that the multi set arguments are valid for this type of get query.
            var multiSetList = multiSetArgs?.ToArray() ?? null;
            if (multiSetList == null || multiSetList.Length == 0)
                throw new ArgumentException($"At least one {nameof(IMultiSetValueArgs)} must be supplied.", nameof(multiSetArgs));

            // Build the Cosmos SQL statement.
            var name = multiSetList[0].GetModelName(this);
            var types = new Dictionary<string, IMultiSetValueArgs>([new KeyValuePair<string, IMultiSetValueArgs>(name, multiSetList[0])]);
            var sb = string.IsNullOrEmpty(sql) ? new StringBuilder($"\"{name}\"") : null;

            if (sb is not null)
            {
                for (int i = 1; i < multiSetList.Length; i++)
                {
                    name = multiSetList[i].GetModelName(this);
                    if (!types.TryAdd(name, multiSetList[i]))
                        throw new ArgumentException($"All {nameof(IMultiSetValueArgs)} must be of different model type.", nameof(multiSetArgs));

                    sb.Append($", \"{name}\"");
                }

                sql = string.Format(MultiSetSqlStatementFormat, sb.ToString());
            }

            // Execute the Cosmos DB query.
            var result = await CosmosDb.Invoker.InvokeAsync(CosmosDb, this, sql, types, async (_, container, sql, types, ct) =>
            {
                // Set up for work.
                var da = new CosmosDbArgs(container.DbArgs, partitionKey);
                var qsi = container.CosmosContainer.GetItemQueryStreamIterator(sql, requestOptions: da.GetQueryRequestOptions());
                IJsonSerializer js = ExecutionContext.GetService<IJsonSerializer>() ?? CoreEx.Json.JsonSerializer.Default;
                var isStj = js is Text.Json.JsonSerializer;

                while (qsi.HasMoreResults)
                {
                    var rm = await qsi.ReadNextAsync(ct).ConfigureAwait(false);
                    if (!rm.IsSuccessStatusCode)
                        return Result.Fail(new InvalidOperationException(rm.ErrorMessage));

                    var json = Stj.JsonDocument.Parse(rm.Content);
                    if (!json.RootElement.TryGetProperty("Documents", out var jds) || jds.ValueKind != Stj.JsonValueKind.Array)
                        return Result.Fail(new InvalidOperationException("Cosmos response JSON 'Documents' property either not found in result or is not an array."));

                    foreach (var jd in jds.EnumerateArray())
                    {
                        if (!jd.TryGetProperty("type", out var jt) || jt.ValueKind != Stj.JsonValueKind.String)
                            return Result.Fail(new InvalidOperationException("Cosmos response documents item 'type' property either not found in result or is not a string."));

                        if (!types.TryGetValue(jt.GetString()!, out var msa))
                            continue;   // Ignore any unexpected type.

                        var model = isStj
                            ? jd.Deserialize(msa.Type, (Stj.JsonSerializerOptions)js.Options)
                            : js.Deserialize(jd.ToString(), msa.Type);

                        if (model is null)
                            return Result.Fail(new InvalidOperationException($"Cosmos response documents item type '{jt.GetRawText()}' deserialization resulted in a null."));

                        var result = msa.AddItem(container, da, model);
                        if (result.IsFailure)
                            return result;
                    }
                }

                return Result.Success;
            }, cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
                return result;

            // Validate the multi-set args and action each accordingly.
            foreach (var msa in multiSetList)
            {
                var r = msa.Verify();
                if (r.IsFailure)
                    return r.AsResult();

                if (!r.Value && msa.StopOnNull)
                    break;

                msa.Invoke();
            }

            return Result.Success;
        }

        #endregion
    }
}