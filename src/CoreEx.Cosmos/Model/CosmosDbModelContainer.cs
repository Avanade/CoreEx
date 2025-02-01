// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Stj = System.Text.Json;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Provides the underlying operations for model-based access within the <see cref="CosmosDbContainer"/>.
    /// </summary>
    public sealed class CosmosDbModelContainer
    {
        private readonly CosmosDbContainer _owner;
        private readonly Lazy<ConcurrentDictionary<Type, Func<object, PartitionKey?>>> _partitionKeyGets = new();
        private readonly Lazy<ConcurrentDictionary<Type, string>> _typeNames = new();
        private readonly Lazy<ConcurrentDictionary<Type, object>> _filters = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbModelContainer"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="CosmosDbContainer"/>.</param>
        internal CosmosDbModelContainer(CosmosDbContainer owner) => _owner = owner;

        /// <summary>
        /// Checks whether the <paramref name="model"/> is in a valid state for the operation.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model value.</param>
        /// <param name="dbArgs">The specific <see cref="CosmosDbArgs"/> for the operation.</param>
        /// <param name="checkAuthorized">Indicates whether an additional authorization check should be performed against the <paramref name="model"/>.</param>
        /// <returns><c>true</c> indicates that the model is in a valid state; otherwise, <c>false</c>.</returns>
        public bool IsModelValid<TModel>(TModel? model, CosmosDbArgs dbArgs, bool checkAuthorized) where TModel : class, IEntityKey, new()
            => !(!dbArgs.IsModelValid(model)
                || (model is ICosmosDbType mt && mt.Type != GetModelName<TModel>())
                || (checkAuthorized && IsAuthorized(model).IsFailure));

        /// <summary>
        /// Checks whether the <paramref name="model"/> is in a valid state for the operation.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model value.</param>
        /// <param name="dbArgs">The specific <see cref="CosmosDbArgs"/> for the operation.</param>
        /// <param name="checkAuthorized">Indicates whether an additional authorization check should be performed against the <paramref name="model"/>.</param>
        /// <returns><c>true</c> indicates that the model is in a valid state; otherwise, <c>false</c>.</returns>
        public bool IsModelValid<TModel>(CosmosDbValue<TModel>? model, CosmosDbArgs dbArgs, bool checkAuthorized) where TModel : class, IEntityKey, new()
            => !(model is null
                || !dbArgs.IsModelValid(model.Value)
                || model.Type != GetModelName<TModel>()
                || (checkAuthorized && IsAuthorized(model).IsFailure));

        /// <summary>
        /// Sets the function to get the <see cref="PartitionKey"/> from the model used by the <see cref="GetPartitionKey{TModel}(TModel, CosmosDbArgs)"/> (used by only the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="getPartitionKey">The function to get the <see cref="PartitionKey"/> from the model.</param>
        internal void UsePartitionKey<TModel>(Func<TModel, PartitionKey>? getPartitionKey) where TModel : class, IEntityKey, new()
        {
            // Where the function is null we should ignore unless previously set.
            if (getPartitionKey is null)
            {
                if (_partitionKeyGets.IsValueCreated && _partitionKeyGets.Value.ContainsKey(typeof(TModel)))
                    throw new InvalidOperationException($"PartitionKey already set for {typeof(TModel).Name}.");

                return;
            }

            if (!_partitionKeyGets.Value.TryAdd(typeof(TModel), model => getPartitionKey.ThrowIfNull(nameof(getPartitionKey)).Invoke((TModel)model)))
                throw new InvalidOperationException($"PartitionKey already set for {typeof(TModel).Name}.");
        }

        /// <summary>
        /// Gets the <see cref="PartitionKey"/> from the <paramref name="model"/> (used by only by the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="model">The cosmos model <see cref="Type"/>.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <returns>The <see cref="PartitionKey"/>.</returns>
        /// <exception cref="AuthorizationException">Will be thrown where the infered <see cref="PartitionKey"/> is not equal to <see cref="CosmosDbContainer.DbArgs"/> (where not <c>null</c>).</exception>
        public PartitionKey GetPartitionKey<TModel>(TModel model, CosmosDbArgs dbArgs) where TModel : class, IEntityKey, new()
        {
            var dbpk = _owner.DbArgs.PartitionKey;
            var pk = _partitionKeyGets.IsValueCreated && _partitionKeyGets.Value.TryGetValue(typeof(TModel), out var gpk) ? gpk(model!) : null;
            
            if (!pk.HasValue)
                pk = dbArgs.PartitionKey ?? _owner.DbArgs.PartitionKey ?? PartitionKey.None;

            if (dbpk is not null && dbpk != PartitionKey.None && dbpk != pk)
                throw new AuthorizationException();

            return pk.Value;
        }

        /// <summary>
        /// Sets the function to get the <see cref="PartitionKey"/> from the <see cref="CosmosDbValue{TModel}"/> used by the <see cref="GetPartitionKey{TModel}(TModel, CosmosDbArgs)"/> (used by only the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="getPartitionKey">The function to get the <see cref="PartitionKey"/> from the model.</param>
        internal void UsePartitionKey<TModel>(Func<CosmosDbValue<TModel>, PartitionKey>? getPartitionKey) where TModel : class, IEntityKey, new()
        {
            // Where the function is null we should ignore unless previously set.
            if (getPartitionKey is null)
            {
                if (_partitionKeyGets.IsValueCreated && _partitionKeyGets.Value.ContainsKey(typeof(CosmosDbValue<TModel>)))
                    throw new InvalidOperationException($"PartitionKey already set for {typeof(CosmosDbValue<TModel>).Name}.");

                return;
            }

            if (!_partitionKeyGets.Value.TryAdd(typeof(CosmosDbValue<TModel>), model => getPartitionKey.ThrowIfNull(nameof(getPartitionKey)).Invoke((CosmosDbValue<TModel>)model)))
                throw new InvalidOperationException($"PartitionKey already set for {typeof(CosmosDbValue<TModel>).Name}.");
        }

        /// <summary>
        /// Gets the <see cref="PartitionKey"/> from the <paramref name="model"/> (used by only by the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="model">The cosmos model <see cref="Type"/>.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <returns>The <see cref="PartitionKey"/>.</returns>
        /// <exception cref="AuthorizationException">Will be thrown where the infered <see cref="PartitionKey"/> is not equal to <see cref="CosmosDbContainer.DbArgs"/> (where not <c>null</c>).</exception>
        public PartitionKey GetPartitionKey<TModel>(CosmosDbValue<TModel> model, CosmosDbArgs dbArgs) where TModel : class, IEntityKey, new()
        {
            var dbpk = _owner.DbArgs.PartitionKey;
            var pk = _partitionKeyGets.IsValueCreated && _partitionKeyGets.Value.TryGetValue(typeof(CosmosDbValue<TModel>), out var gpk) ? gpk(model!) : null;

            if (!pk.HasValue)
                pk = dbArgs.PartitionKey ?? _owner.DbArgs.PartitionKey ?? PartitionKey.None;

            if (dbpk is not null && dbpk != PartitionKey.None && dbpk != pk)
                throw new AuthorizationException();

            return pk.Value;
        }

        /// <summary>
        /// Sets the name for the model <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="name">The model name.</param>
        internal void UseModelName<TModel>(string name) where TModel : class, IEntityKey, new()
        {
            if (!_typeNames.Value.TryAdd(typeof(TModel), name))
                throw new InvalidOperationException($"Model Type Name already set for {typeof(TModel).Name}.");
        }

        /// <summary>
        /// Gets the name for the model <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <returns>The model name where configured (see <see cref="CosmosDbContainer.UseModelName{TModel}(string)"/>); otherwise, defaults to <see cref="Type"/> <see cref="System.Reflection.MemberInfo.Name"/>.</returns>
        public string GetModelName<TModel>() where TModel : class, IEntityKey, new() => _typeNames.IsValueCreated && _typeNames.Value.TryGetValue(typeof(TModel), out var name) ? name : typeof(TModel).Name;

        /// <summary>
        /// Sets the filter for all operations performed on the <typeparamref name="TModel"/> to ensure authorisation is applied. Applies automatically to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="filter">The authorization filter query.</param>
        internal void UseAuthorizeFilter<TModel>(Func<IQueryable<TModel>, IQueryable<TModel>>? filter) where TModel : class, IEntityKey, new()
        {
            if (filter is null)
            {
                if (_filters.IsValueCreated && _filters.Value.ContainsKey(typeof(TModel)))
                    throw new InvalidOperationException($"Filter already set for {typeof(TModel).Name}.");

                return;
            }

            if (!_filters.Value.TryAdd(typeof(TModel), filter))
                throw new InvalidOperationException($"Filter already set for {typeof(TModel).Name}.");
        }

        /// <summary>
        /// Checks the value to determine whether the user is authorized with the <see cref="GetAuthorizeFilter{TModel}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model value.</param>
        /// <remarks>Either <see cref="Result.Success"/> or <see cref="Result.AuthorizationError"/>.</remarks>
        public Result IsAuthorized<TModel>(TModel model) where TModel : class, IEntityKey, new()
        {
            if (model != default)
            {
                var filter = GetAuthorizeFilter<TModel>();
                if (filter != null && !filter(new [] { model }.AsQueryable()).Any())
                    return Result.AuthorizationError();
            }

            return Result.Success;
        }

        /// <summary>
        /// Gets the authorization filter for the <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <returns>The authorization filter query where configured; otherwise, <c>null</c>.</returns>
        public Func<IQueryable<TModel>, IQueryable<TModel>>? GetAuthorizeFilter<TModel>() where TModel : class, IEntityKey, new() 
            => _filters.IsValueCreated && _filters.Value.TryGetValue(typeof(TModel), out var filter) ? (Func<IQueryable<TModel>, IQueryable<TModel>>)filter : null;

        /// <summary>
        /// Sets the filter for all operations performed on the <see cref="CosmosDbValue{TModel}"/> to ensure authorization is applied. Applies automatically to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="filter">The authorization filter query.</param>
        internal void UseAuthorizeFilter<TModel>(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? filter) where TModel : class, IEntityKey, new()
        {
            if (filter is null)
            {
                if (_filters.IsValueCreated && _filters.Value.ContainsKey(typeof(CosmosDbValue<TModel>)))
                    throw new InvalidOperationException($"Filter already set for {typeof(CosmosDbValue<TModel>).Name}.");

                return;
            }

            if (!_filters.Value.TryAdd(typeof(CosmosDbValue<TModel>), filter))
                throw new InvalidOperationException($"Filter already set for {typeof(CosmosDbValue<TModel>).Name}.");
        }

        /// <summary>
        /// Gets the authorization filter for the <see cref="CosmosDbValue{TModel}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <returns>The authorization filter query where configured; otherwise, <c>null</c>.</returns>
        public Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? GetValueAuthorizeFilter<TModel>() where TModel : class, IEntityKey, new() 
            => _filters.IsValueCreated && _filters.Value.TryGetValue(typeof(CosmosDbValue<TModel>), out var filter) ? (Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>)filter : null;

        /// <summary>
        /// Checks the value to determine whether the user is authorized with the <see cref="GetValueAuthorizeFilter{TModel}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model value.</param>
        /// <remarks>Either <see cref="Result.Success"/> or <see cref="Result.AuthorizationError"/>.</remarks>
        public Result IsAuthorized<TModel>(CosmosDbValue<TModel> model) where TModel : class, IEntityKey, new()
        {
            if (model != null && model.Value != default)
            {
                var filter = GetValueAuthorizeFilter<TModel>();
                if (filter != null && !filter(new CosmosDbValue<TModel>[] { model }.AsQueryable()).Any())
                    return Result.AuthorizationError();
            }

            return Result.Success;
        }

        /// <summary>
        /// Gets the <b>value</b> from the response updating any special properties as required.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="resp">The response value.</param>
        /// <returns>The entity value.</returns>
        internal static TModel? GetResponseValue<TModel>(Response<TModel> resp) where TModel : class, IEntityKey, new() => resp?.Resource == null ? default : resp.Resource;

        /// <summary>
        /// Gets the <b>value</b> from the response updating any special properties as required.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="resp">The response value.</param>
        /// <returns>The entity value.</returns>
        internal static CosmosDbValue<TModel>? GetResponseValue<TModel>(Response<CosmosDbValue<TModel>> resp) where TModel : class, IEntityKey, new() => resp?.Resource == null ? default : resp.Resource;

        #region Query

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> Query<TModel>(Func<IQueryable<TModel>, IQueryable<TModel>>? query) where TModel : class, IEntityKey, new() => Query(new CosmosDbArgs(_owner.DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> Query<TModel>(PartitionKey? partitionKey = null, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) where TModel : class, IEntityKey, new() => Query(new CosmosDbArgs(_owner.DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> Query<TModel>(CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) where TModel : class, IEntityKey, new() => new(_owner, dbArgs, query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> ValueQuery<TModel>(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) where TModel : class, IEntityKey, new() => ValueQuery(new CosmosDbArgs(_owner.DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> ValueQuery<TModel>(PartitionKey? partitionKey = null, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) where TModel : class, IEntityKey, new() => ValueQuery(new CosmosDbArgs(_owner.DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> ValueQuery<TModel>(CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) where TModel : class, IEntityKey, new() => new(_owner, dbArgs, query);

        #endregion

        #region Get

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<TModel?> GetAsync<TModel>(CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await GetWithResultAsync<TModel>(key, cancellationToken)).Value;

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<TModel?>> GetWithResultAsync<TModel>(CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => GetWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<TModel?> GetAsync<TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await GetWithResultAsync<TModel>(key, partitionKey, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<TModel?>> GetWithResultAsync<TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => GetWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<TModel?> GetAsync<TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await GetWithResultAsync<TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<TModel?>> GetWithResultAsync<TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            return _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, _owner.GetCosmosId(key), dbArgs, async (_, id, args, ct) =>
            {
                try
                {
                    var pk = _owner.GetPartitionKey(dbArgs.PartitionKey);
                    var resp = await _owner.CosmosContainer.ReadItemAsync<TModel>(id, pk, args.GetItemRequestOptions(), ct).ConfigureAwait(false);
                    if (!IsModelValid(resp.Resource, args, false))
                        return args.NullOnNotFound ? Result<TModel?>.None : Result<TModel?>.NotFoundError();

                    return Result.Go(IsAuthorized<TModel>(resp)).ThenAs(() => GetResponseValue(resp));
                }
                catch (CosmosException dcex) when (args.NullOnNotFound && dcex.StatusCode == System.Net.HttpStatusCode.NotFound) { return args.NullOnNotFound ? Result<TModel?>.None : Result<TModel?>.NotFoundError(); }
            }, cancellationToken, nameof(GetWithResultAsync));
        }

        /// <summary>
        /// Gets the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="CosmosDbValue{TModel}"/> value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<CosmosDbValue<TModel>?> GetValueAsync<TModel>(CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await GetValueWithResultAsync<TModel>(key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="CosmosDbValue{TModel}"/> value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetValueWithResultAsync<TModel>(CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => GetValueWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="CosmosDbValue{TModel}"/> value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<CosmosDbValue<TModel>?> GetValueAsync<TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await GetValueWithResultAsync<TModel>(key, partitionKey, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="CosmosDbValue{TModel}"/> value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetValueWithResultAsync<TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => GetValueWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Gets the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="CosmosDbValue{TModel}"/> value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<CosmosDbValue<TModel>?> GetValueAsync<TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await GetValueWithResultAsync<TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="CosmosDbValue{TModel}"/> value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetValueWithResultAsync<TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            return _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, _owner.GetCosmosId(key), dbArgs, async (_, id, args, ct) =>
            {
                try
                {
                    var pk = _owner.GetPartitionKey(dbArgs.PartitionKey);
                    var resp = await _owner.CosmosContainer.ReadItemAsync<CosmosDbValue<TModel>>(id, pk, args.GetItemRequestOptions(), ct).ConfigureAwait(false);
                    if (!IsModelValid(resp.Resource, args, false))
                        return args.NullOnNotFound ? Result<CosmosDbValue<TModel>?>.None : Result<CosmosDbValue<TModel>?>.NotFoundError();

                    return Result.Go(IsAuthorized<TModel>(resp)).ThenAs(() => GetResponseValue(resp));
                }
                catch (CosmosException dcex) when (args.NullOnNotFound && dcex.StatusCode == System.Net.HttpStatusCode.NotFound) { return args.NullOnNotFound ? Result<CosmosDbValue<TModel>?>.None : Result<CosmosDbValue<TModel>?>.NotFoundError(); }
            }, cancellationToken, nameof(GetWithResultAsync));
        }

        #endregion

        #region Create

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public async Task<TModel> CreateAsync<TModel>(TModel model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await CreateWithResultAsync(new CosmosDbArgs(_owner.DbArgs), model, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public async Task<TModel> CreateAsync<TModel>(CosmosDbArgs dbArgs, TModel model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await CreateWithResultAsync(dbArgs, model, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public Task<Result<TModel>> CreateWithResultAsync<TModel>(TModel model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => CreateWithResultAsync(new CosmosDbArgs(_owner.DbArgs), model, cancellationToken);

        /// <summary>
        /// Creates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public Task<Result<TModel>> CreateWithResultAsync<TModel>(CosmosDbArgs dbArgs, TModel model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            return _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, Cleaner.PrepareCreate(model.ThrowIfNull(nameof(model))), dbArgs, async (_, m, args, ct) =>
            {
                var pk = GetPartitionKey(m, args);
                return await Result
                    .Go(IsAuthorized(model))
                    .ThenAsAsync(() => _owner.CosmosContainer.CreateItemAsync(Cleaner.PrepareCreate(model), pk, args.GetItemRequestOptions(), ct))
                    .ThenAs(resp => GetResponseValue(resp!)!);
            }, cancellationToken, nameof(CreateWithResultAsync));
        }

        /// <summary>
        /// Creates the <see cref="CosmosDbValue{TModel}"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public async Task<CosmosDbValue<TModel>> CreateValueAsync<TModel>(CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await CreateValueWithResultAsync(new CosmosDbArgs(_owner.DbArgs), model, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the <see cref="CosmosDbValue{TModel}"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public async Task<CosmosDbValue<TModel>> CreateValueAsync<TModel>(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await CreateValueWithResultAsync(dbArgs, model, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the <see cref="CosmosDbValue{TModel}"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public Task<Result<CosmosDbValue<TModel>>> CreateValueWithResultAsync<TModel>(CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => CreateValueWithResultAsync(new CosmosDbArgs(_owner.DbArgs), model, cancellationToken);

        /// <summary>
        /// Creates the <see cref="CosmosDbValue{TModel}"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public Task<Result<CosmosDbValue<TModel>>> CreateValueWithResultAsync<TModel>(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            return _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, Cleaner.PrepareCreate(model.ThrowIfNull(nameof(model))), dbArgs, async (_, m, args, ct) =>
            {
                var pk = GetPartitionKey(m, args);
                return await Result
                    .Go(IsAuthorized(m))
                    .ThenAsAsync(async () =>
                    {
                        ((ICosmosDbValue)m).PrepareBefore(args, typeof(TModel).Name);
                        Cleaner.PrepareCreate(m.Value);
                        var resp = await _owner.CosmosContainer.CreateItemAsync(m, pk, args.GetItemRequestOptions(), ct).ConfigureAwait(false);
                        return GetResponseValue(resp)!;
                    });
            }, cancellationToken, nameof(CreateWithResultAsync));
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates the model.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public async Task<TModel> UpdateAsync<TModel>(TModel model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await UpdateWithResultInternalAsync(new CosmosDbArgs(_owner.DbArgs), model, null, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the model.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public async Task<TModel> UpdateAsync<TModel>(CosmosDbArgs dbArgs, TModel model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await UpdateWithResultInternalAsync(dbArgs, model, null, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public Task<Result<TModel>> UpdateWithResultAsync<TModel>(TModel model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => UpdateWithResultInternalAsync(new CosmosDbArgs(_owner.DbArgs), model, null, cancellationToken);

        /// <summary>
        /// Updates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public Task<Result<TModel>> UpdateWithResultAsync<TModel>(CosmosDbArgs dbArgs, TModel model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => UpdateWithResultInternalAsync(dbArgs, model, null, cancellationToken);

        /// <summary>
        /// Updates the model with a <see cref="Result{T}"/> (internal).
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="modelUpdater">The action to update the model after the read.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        internal Task<Result<TModel>> UpdateWithResultInternalAsync<TModel>(CosmosDbArgs dbArgs, TModel model, Action<TModel>? modelUpdater, CancellationToken cancellationToken) where TModel : class, IEntityKey, new()
        {
            return _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, Cleaner.PrepareUpdate(model.ThrowIfNull(nameof(model))), dbArgs, async (_, m, args, ct) =>
            {
                // Where supporting etag then use IfMatch for concurrency.
                var ro = args.GetItemRequestOptions();
                if (ro.IfMatchEtag == null && m is IETag etag && etag.ETag != null)
                    ro.IfMatchEtag = ETagGenerator.FormatETag(etag.ETag);

                // Must read existing to update.
                var id = _owner.GetCosmosId(m);
                var pk = GetPartitionKey(model, dbArgs);
                var resp = await _owner.CosmosContainer.ReadItemAsync<TModel>(id, pk, ro, ct).ConfigureAwait(false);
                if (!IsModelValid(resp.Resource, args, false))
                    return Result<TModel>.NotFoundError();

                return await Result
                    .Go(IsAuthorized<TModel>(resp))
                    .When(() => m is IETag etag2 && etag2.ETag != null && ETagGenerator.FormatETag(etag2.ETag) != resp.ETag, () => Result.ConcurrencyError())
                    .Then(() =>
                    {
                        ro.SessionToken = resp.Headers?.Session;
                        modelUpdater?.Invoke(resp.Resource);
                        Cleaner.ResetTenantId(resp.Resource);

                        // Re-check auth to make sure not updating to something not allowed.
                        return IsAuthorized<TModel>(resp);
                    })
                    .ThenAsAsync(async () =>
                    {
                        resp = await _owner.CosmosContainer.ReplaceItemAsync(Cleaner.PrepareUpdate(resp.Resource), id, pk, ro, ct).ConfigureAwait(false);
                        return GetResponseValue(resp)!;
                    });
            }, cancellationToken, nameof(UpdateWithResultAsync));
        }

        /// <summary>
        /// Updates the <see cref="CosmosDbValue{TModel}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public async Task<CosmosDbValue<TModel>> UpdateValueAsync<TModel>(CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await UpdateWithResultAsync(new CosmosDbArgs(_owner.DbArgs), model, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the <see cref="CosmosDbValue{TModel}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public async Task<CosmosDbValue<TModel>> UpdateValueAsync<TModel>(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await UpdateWithResultAsync(dbArgs, model, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the <see cref="CosmosDbValue{TModel}"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public Task<Result<CosmosDbValue<TModel>>> UpdateValueWithResultAsync<TModel>(CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => UpdateValueWithResultInternalAsync(new CosmosDbArgs(_owner.DbArgs), model, null, cancellationToken);

        /// <summary>
        /// Updates the <see cref="CosmosDbValue{TModel}"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public Task<Result<CosmosDbValue<TModel>>> UpdateValueWithResultAsync<TModel>(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => UpdateValueWithResultInternalAsync(dbArgs, model, null, cancellationToken);

        /// <summary>
        /// Updates the <see cref="CosmosDbValue{TModel}"/> with a <see cref="Result{T}"/> (internal).
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="modelUpdater">The action to update the model after the read.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        internal Task<Result<CosmosDbValue<TModel>>> UpdateValueWithResultInternalAsync<TModel>(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, Action<CosmosDbValue<TModel>>? modelUpdater, CancellationToken cancellationToken) where TModel : class, IEntityKey, new()
        {
            return _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, Cleaner.PrepareUpdate(model.ThrowIfNull(nameof(model))), dbArgs, async (_, m, args, ct) =>
            {
                // Where supporting etag then use IfMatch for concurrency.
                var ro = args.GetItemRequestOptions();
                if (ro.IfMatchEtag == null && m is IETag etag && etag.ETag != null)
                    ro.IfMatchEtag = ETagGenerator.FormatETag(etag.ETag);

                // Must read existing to update.
                ((ICosmosDbValue)m).PrepareBefore(dbArgs, GetModelName<TModel>());
                var id = m.Id;
                var pk = GetPartitionKey(m, dbArgs);
                var resp = await _owner.CosmosContainer.ReadItemAsync<CosmosDbValue<TModel>>(id, pk, ro, ct).ConfigureAwait(false);
                if (!IsModelValid(resp.Resource, args, false))
                    return Result<CosmosDbValue<TModel>>.NotFoundError();

                return await Result
                    .Go(IsAuthorized(resp.Resource))
                    .When(() => m is IETag etag2 && etag2.ETag != null && ETagGenerator.FormatETag(etag2.ETag) != resp.ETag, () => Result.ConcurrencyError())
                    .Then(() =>
                    {
                        ro.SessionToken = resp.Headers?.Session;
                        modelUpdater?.Invoke(resp.Resource);
                        Cleaner.ResetTenantId(m.Value);
                        ((ICosmosDbValue)resp.Resource).PrepareBefore(dbArgs, GetModelName<TModel>());

                        // Re-check auth to make sure not updating to something not allowed.
                        return IsAuthorized<TModel>(resp);
                    })
                    .ThenAsAsync(async () =>
                    {
                        Cleaner.PrepareUpdate(resp.Resource.Value);
                        resp = await _owner.CosmosContainer.ReplaceItemAsync(resp.Resource, id, pk, ro, ct).ConfigureAwait(false);
                        return GetResponseValue(resp)!;
                    });
            }, cancellationToken, nameof(UpdateValueWithResultAsync));
        }

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync<TModel>(CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await DeleteWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs), key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync<TModel>(CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => DeleteWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs), key, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync<TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await DeleteWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs, partitionKey), key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync<TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => DeleteWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync<TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await DeleteWithResultAsync<TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync<TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            return _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, _owner.GetCosmosId(key), dbArgs, async (_, id, args, ct) =>
            {
                try
                {
                    // Must read the existing to validate.
                    var ro = args.GetItemRequestOptions();
                    var pk = _owner.GetPartitionKey(dbArgs.PartitionKey);
                    var resp = await _owner.CosmosContainer.ReadItemAsync<TModel>(id, pk, ro, ct).ConfigureAwait(false);
                    if (!IsModelValid(resp.Resource, args, false))
                        return Result.Success;

                    // Delete; either logically or physically.
                    if (resp.Resource is ILogicallyDeleted ild)
                    {
                        if (ild.IsDeleted.HasValue && ild.IsDeleted.Value)
                            return Result.Success;

                        ild.IsDeleted = true;
                        return await Result
                            .Go(IsAuthorized(resp.Resource))
                            .ThenAsync(async () =>
                            {
                                ro.SessionToken = resp.Headers?.Session;
                                await _owner.CosmosContainer.ReplaceItemAsync(Cleaner.PrepareUpdate(resp.Resource), id, pk, ro, ct).ConfigureAwait(false);
                                return Result.Success;
                            });
                    }

                    return await Result
                        .Go(IsAuthorized(resp.Resource))
                        .ThenAsync(async () =>
                        {
                            ro.SessionToken = resp.Headers?.Session;
                            await _owner.CosmosContainer.DeleteItemAsync<TModel>(id, pk, ro, ct).ConfigureAwait(false);
                            return Result.Success;
                        });
                }
                catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound) { return Result.NotFoundError(); }
            }, cancellationToken, nameof(DeleteWithResultAsync));
        }

        /// <summary>
        /// Deletes the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteValueAsync<TModel>(CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await DeleteWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs), key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteValueWithResultAsync<TModel>(CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => DeleteValueWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs), key, cancellationToken);

        /// <summary>
        /// Deletes the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteValueAsync<TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await DeleteValueWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs, partitionKey), key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteValueWithResultAsync<TModel>(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => DeleteValueWithResultAsync<TModel>(new CosmosDbArgs(_owner.DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Deletes the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteValueAsync<TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
            => (await DeleteValueWithResultAsync<TModel>(dbArgs, key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the <see cref="CosmosDbValue{TModel}"/> for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteValueWithResultAsync<TModel>(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            return _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, _owner.GetCosmosId(key), dbArgs, async (_, id, args, ct) =>
            {
                try
                {
                    // Must read existing to delete and to make sure we are deleting for the correct Type; don't just trust the key.
                    var ro = args.GetItemRequestOptions();
                    var pk = _owner.GetPartitionKey(dbArgs.PartitionKey);
                    var resp = await _owner.CosmosContainer.ReadItemAsync<CosmosDbValue<TModel>>(id, pk, ro, ct).ConfigureAwait(false);
                    if (!IsModelValid(resp.Resource, args, false))
                        return Result.Success;

                    // Delete; either logically or physically.
                    if (resp.Resource.Value is ILogicallyDeleted ild)
                    {
                        if (ild.IsDeleted.HasValue && ild.IsDeleted.Value)
                            return Result.Success;

                        ild.IsDeleted = true;
                        return await Result
                            .Go(IsAuthorized(resp.Resource))
                            .ThenAsync(async () =>
                            {
                                ro.SessionToken = resp.Headers?.Session;
                                Cleaner.PrepareUpdate(resp.Resource.Value);
                                await _owner.CosmosContainer.ReplaceItemAsync(resp.Resource, id, pk, ro, ct).ConfigureAwait(false);
                                return Result.Success;
                            });
                    }

                    return await Result
                        .Go(IsAuthorized(resp.Resource))
                        .ThenAsync(async () =>
                        {
                            ro.SessionToken = resp.Headers?.Session;
                            await _owner.CosmosContainer.DeleteItemAsync<TModel>(id, pk, ro, ct).ConfigureAwait(false);
                            return Result.Success;
                        });
                }
                catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound) { return Result.NotFoundError(); }
            }, cancellationToken, nameof(DeleteValueWithResultAsync));
        }

        #endregion

        #region MultiSet

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetModelArgs"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetModelArgs"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetModelArgs}, CancellationToken)"/> for further details.</remarks>
        public Task SelectMultiSetAsync(PartitionKey partitionKey, params IMultiSetModelArgs[] multiSetArgs) => SelectMultiSetAsync(partitionKey, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetModelArgs"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetModelArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetModelArgs}, CancellationToken)"/> for further details.</remarks>
        public Task SelectMultiSetAsync(PartitionKey partitionKey, IEnumerable<IMultiSetModelArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectMultiSetAsync(partitionKey, null, multiSetArgs, cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetModelArgs"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="sql">The override SQL statement; will default where not specified.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetModelArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetModelArgs}, CancellationToken)"/> for further details.</remarks>
        public async Task SelectMultiSetAsync(PartitionKey partitionKey, string? sql, IEnumerable<IMultiSetModelArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => (await SelectMultiSetWithResultAsync(partitionKey, sql, multiSetArgs, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetModelArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetModelArgs"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetModelArgs}, CancellationToken)"/> for further details.</remarks>
        public Task<Result> SelectMultiSetWithResultAsync(PartitionKey partitionKey, params IMultiSetModelArgs[] multiSetArgs) => SelectMultiSetWithResultAsync(partitionKey, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetModelArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetModelArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetModelArgs}, CancellationToken)"/> for further details.</remarks>
        public Task<Result> SelectMultiSetWithResultAsync(PartitionKey partitionKey, IEnumerable<IMultiSetModelArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectMultiSetWithResultAsync(partitionKey, null, multiSetArgs, cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetModelArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="sql">The override SQL statement; will default where not specified.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetModelArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The <paramref name="multiSetArgs"/> must be of type <see cref="CosmosDbValue{TModel}"/>. Each <paramref name="multiSetArgs"/> is verified and executed in the order specified.
        /// <para>The underlying SQL will be automatically created from the specified <paramref name="multiSetArgs"/> where not explicitly supplied. Essentially, it is a simple query where all <i>types</i> inferred from the <paramref name="multiSetArgs"/>
        /// are included, for example: <c>SELECT * FROM c WHERE c.type in ("TypeNameA", "TypeNameB")</c></para>
        /// </remarks>
        public async Task<Result> SelectMultiSetWithResultAsync(PartitionKey partitionKey, string? sql, IEnumerable<IMultiSetModelArgs> multiSetArgs, CancellationToken cancellationToken = default)
        {
            // Verify that the multi set arguments are valid for this type of get query.
            var multiSetList = multiSetArgs?.ToArray() ?? null;
            if (multiSetList == null || multiSetList.Length == 0)
                throw new ArgumentException($"At least one {nameof(IMultiSetModelArgs)} must be supplied.", nameof(multiSetArgs));

            // Build the Cosmos SQL statement.
            var name = multiSetList[0].GetModelName(_owner);
            var types = new Dictionary<string, IMultiSetModelArgs>([new KeyValuePair<string, IMultiSetModelArgs>(name, multiSetList[0])]);
            var sb = string.IsNullOrEmpty(sql) ? new StringBuilder($"\"{name}\"") : null;

            if (sb is not null)
            {
                for (int i = 1; i < multiSetList.Length; i++)
                {
                    name = multiSetList[i].GetModelName(_owner);
                    if (!types.TryAdd(name, multiSetList[i]))
                        throw new ArgumentException($"All {nameof(IMultiSetValueArgs)} must be of different model type.", nameof(multiSetArgs));

                    sb.Append($", \"{name}\"");
                }

                sql = string.Format(_owner.MultiSetSqlStatementFormat, sb.ToString());
            }

            // Execute the Cosmos DB query.
            var result = await _owner.CosmosDb.Invoker.InvokeAsync(_owner.CosmosDb, _owner, sql, types, async (_, container, sql, types, ct) =>
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