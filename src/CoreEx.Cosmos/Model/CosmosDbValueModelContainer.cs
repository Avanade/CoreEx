// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
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
    /// Provides <see cref="CosmosDb"/> model-only container.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
    /// <param name="dbArgs">The optional <see cref="CosmosDbArgs"/>.</param>
    public class CosmosDbValueModelContainer<TModel>(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null) : CosmosDbModelContainerBase<TModel, CosmosDbValueModelContainer<TModel>>(cosmosDb, containerId, dbArgs) where TModel : class, IEntityKey, new()
    {
        private Func<CosmosDbValue<TModel>, PartitionKey>? _partitionKey;

        /// <summary>
        /// Gets the <see cref="Type"/> name.
        /// </summary>
        /// <remarks><see cref="UseTypeName(string)"/> enables override.</remarks>
        public string TypeName { get; private set; } = typeof(TModel).Name;

        /// <summary>
        /// Overrides the <see cref="TypeName"/>.
        /// </summary>
        /// <param name="typeName">The type name override.</param>
        /// <returns>The <see cref="CosmosDbValueModelContainer{TModel}"/> instance to support fluent-style method-chaining.</returns>
        public CosmosDbValueModelContainer<TModel> UseTypeName(string typeName)
        {
            TypeName = typeName;
            return this;
        }

        /// <summary>
        /// Sets the function to determine the <see cref="PartitionKey"/>; used for <see cref="GetPartitionKey(CosmosDbValue{TModel}, CosmosDbArgs)"/> (only <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="partitionKey">The function to determine the <see cref="PartitionKey"/>.</param>
        /// <returns>The <see cref="CosmosDbValueModelContainer{TModel}"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is used where there is a value and the corresponding <see cref="PartitionKey"/> needs to be dynamically determined.</remarks>
        public CosmosDbValueModelContainer<TModel> UsePartitionKey(Func<CosmosDbValue<TModel>, PartitionKey> partitionKey)
        {
            _partitionKey = partitionKey;
            return this;
        }

        /// <summary>
        /// Gets the <see cref="PartitionKey"/> from the <paramref name="model"/> (only <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="model">The model to infer <see cref="PartitionKey"/> from.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <returns>The <see cref="PartitionKey"/>.</returns>
        /// <exception cref="AuthorizationException">Will be thrown where the infered <see cref="PartitionKey"/> is not equal to <see cref="CosmosDbContainer.DbArgs"/> (where not <c>null</c>).</exception>
        public PartitionKey GetPartitionKey(CosmosDbValue<TModel> model, CosmosDbArgs dbArgs)
        {
            var dbpk = DbArgs.PartitionKey;
            var pk = _partitionKey?.Invoke(model) ?? dbArgs.PartitionKey ?? DbArgs.PartitionKey ?? PartitionKey.None;
            if (dbpk is not null && dbpk != PartitionKey.None && dbpk != pk)
                throw new AuthorizationException();

            return pk;
        }

        /// <summary>
        /// Gets the <b>value</b> from the response updating any special properties as required.
        /// </summary>
        /// <param name="resp">The response value.</param>
        /// <returns>The entity value.</returns>
        internal static CosmosDbValue<TModel>? GetResponseValue(Response<CosmosDbValue<TModel>> resp) => resp?.Resource;

        /// <inheritdoc/>
        protected override bool IsModelValid(object? model, CosmosDbArgs args, bool checkAuthorized) => IsModelValid((CosmosDbValue<TModel>?)model, args, checkAuthorized);

        /// <summary>
        /// Checks whether the <paramref name="model"/> is in a valid state for the operation.
        /// </summary>
        /// <param name="model">The model value.</param>
        /// <param name="args">The specific <see cref="CosmosDbArgs"/> for the operation.</param>
        /// <param name="checkAuthorized">Indicates whether an additional authorization check should be performed against the <paramref name="model"/>.</param>
        /// <returns><c>true</c> indicates that the model is in a valid state; otherwise, <c>false</c>.</returns>
        public bool IsModelValid(CosmosDbValue<TModel>? model, CosmosDbArgs args, bool checkAuthorized)
            => !(model == null 
                || model.Type != TypeName 
                || (args.FilterByTenantId && model.Value is ITenantId tenantId && tenantId.TenantId != args.GetTenantId()) 
                || (model.Value is ILogicallyDeleted ld && ld.IsDeleted.HasValue && ld.IsDeleted.Value)
                || (checkAuthorized && IsAuthorized(model).IsFailure));

        /// <summary>
        /// Checks the value to determine whether the user is authorized with the <see cref="CosmosDb.GetAuthorizeFilter{TModel}(string)"/>.
        /// </summary>
        /// <param name="model">The model value.</param>
        /// <remarks>Either <see cref="Result.Success"/> or <see cref="Result.AuthorizationError"/>.</remarks>
        public Result IsAuthorized(CosmosDbValue<TModel> model)
        {
            if (model != null && model.Value != default)
            {
                var filter = CosmosDb.GetAuthorizeFilter<TModel>(Container.Id);
                if (filter != null && !((IQueryable<CosmosDbValue<TModel>>)filter(new CosmosDbValue<TModel>[] { model }.AsQueryable())).Any())
                    return Result.AuthorizationError();
            }

            return Result.Success;
        }

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> Query(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) => Query(new CosmosDbArgs(DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> Query(PartitionKey? partitionKey = null, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => Query(new CosmosDbArgs(DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> Query(CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => new(this, dbArgs, query);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<CosmosDbValue<TModel>?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => await GetWithResultAsync(key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetWithResultAsync(new CosmosDbArgs(DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<CosmosDbValue<TModel>?> GetAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => await GetWithResultAsync(key, partitionKey, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => GetWithResultAsync(new CosmosDbArgs(DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public async Task<CosmosDbValue<TModel>?> GetAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => (await GetWithResultAsync(dbArgs, key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, GetCosmosId(key), dbArgs, async (_, id, args, ct) =>
        {
            try
            {
                var pk = args.PartitionKey ?? DbArgs.PartitionKey ?? PartitionKey.None;
                var resp = await Container.ReadItemAsync<CosmosDbValue<TModel>>(id, pk, args.GetItemRequestOptions(), ct).ConfigureAwait(false);
                if (!IsModelValid(resp.Resource, args, false))
                    return args.NullOnNotFound ? Result<CosmosDbValue<TModel>?>.None : Result<CosmosDbValue<TModel>?>.NotFoundError();

                return Result.Go(IsAuthorized(resp)).ThenAs(() => GetResponseValue(resp));
            }
            catch (CosmosException dcex) when (args.NullOnNotFound && dcex.StatusCode == System.Net.HttpStatusCode.NotFound) { return args.NullOnNotFound ? Result<CosmosDbValue<TModel>?>.None : Result<CosmosDbValue<TModel>?>.NotFoundError(); }
        }, cancellationToken, nameof(GetWithResultAsync));

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public async Task<CosmosDbValue<TModel>> CreateAsync(CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) => await CreateWithResultAsync(model, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Creates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public Task<Result<CosmosDbValue<TModel>>> CreateWithResultAsync(CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) => CreateWithResultAsync(new CosmosDbArgs(DbArgs), model, cancellationToken);

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public async Task<CosmosDbValue<TModel>> CreateAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) => (await CreateWithResultAsync(dbArgs, model, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created model.</returns>
        public Task<Result<CosmosDbValue<TModel>>> CreateWithResultAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, model.ThrowIfNull(nameof(model)), dbArgs, async (_, m, args, ct) =>
        {
            Cleaner.ResetTenantId(m);
            var pk = GetPartitionKey(m, dbArgs);
            return await Result
                .Go(IsAuthorized(m))
                .ThenAsAsync(async () =>
                {
                    ((ICosmosDbValue)m).PrepareBefore(dbArgs, TypeName);
                    var resp = await Container.CreateItemAsync(m, pk, args.GetItemRequestOptions(), ct).ConfigureAwait(false);
                    return GetResponseValue(resp)!;
                });
        }, cancellationToken, nameof(CreateWithResultAsync));

        /// <summary>
        /// Updates the model.
        /// </summary>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public async Task<CosmosDbValue<TModel>> UpdateAsync(CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) => await UpdateWithResultAsync(model, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Updates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public Task<Result<CosmosDbValue<TModel>>> UpdateWithResultAsync(CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) => UpdateWithResultAsync(new CosmosDbArgs(DbArgs), model, cancellationToken);

        /// <summary>
        /// Updates the model.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public async Task<CosmosDbValue<TModel>> UpdateAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) => (await UpdateWithResultAsync(dbArgs, model, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the model with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        public Task<Result<CosmosDbValue<TModel>>> UpdateWithResultAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, CancellationToken cancellationToken = default) => UpdateWithResultInternalAsync(dbArgs, model, null, cancellationToken);

        /// <summary>
        /// Updates the model with a <see cref="Result{T}"/> (internal).
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="model">The model to update.</param>
        /// <param name="modelUpdater">The action to update the model after the read.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated model.</returns>
        internal Task<Result<CosmosDbValue<TModel>>> UpdateWithResultInternalAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> model, Action<CosmosDbValue<TModel>>? modelUpdater, CancellationToken cancellationToken) => CosmosDb.Invoker.InvokeAsync(CosmosDb, model.ThrowIfNull(nameof(model)), dbArgs, async (_, m, args, ct) =>
        {
            // Where supporting etag then use IfMatch for concurrency.
            var ro = args.GetItemRequestOptions();
            if (ro.IfMatchEtag == null && m is IETag etag && etag.ETag != null)
                ro.IfMatchEtag = ETagGenerator.FormatETag(etag.ETag);

            // Must read existing to update.
            ((ICosmosDbValue)m).PrepareBefore(dbArgs, TypeName);
            var id = m.Id;
            var pk = GetPartitionKey(m, dbArgs);
            var resp = await Container.ReadItemAsync<CosmosDbValue<TModel>>(id, pk, ro, ct).ConfigureAwait(false);
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
                    ((ICosmosDbValue)resp.Resource).PrepareBefore(dbArgs, TypeName);

                    // Re-check auth to make sure not updating to something not allowed.
                    return IsAuthorized(resp);
                })
                .ThenAsAsync(async () =>
                {
                    resp = await Container.ReplaceItemAsync(resp.Resource, id, pk, ro, ct).ConfigureAwait(false);
                    return GetResponseValue(resp)!;
                });
        }, cancellationToken, nameof(UpdateWithResultAsync));

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync(CompositeKey key, CancellationToken cancellationToken = default) => (await DeleteWithResultAsync(key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => DeleteWithResultAsync(new CosmosDbArgs(DbArgs), key, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => (await DeleteWithResultAsync(key, partitionKey, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => DeleteWithResultAsync(new CosmosDbArgs(DbArgs, partitionKey), key, cancellationToken);

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task<Result> DeleteAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => (await DeleteWithResultAsync(dbArgs, key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the model for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, GetCosmosId(key), dbArgs, async (_, id, args, ct) =>
        {
            try
            {
                // Must read existing to delete and to make sure we are deleting for the correct Type; don't just trust the key.
                var ro = args.GetItemRequestOptions();
                var pk = args.PartitionKey ?? DbArgs.PartitionKey ?? PartitionKey.None;
                var resp = await Container.ReadItemAsync<CosmosDbValue<TModel>>(id, pk, ro, ct).ConfigureAwait(false);
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
                            await Container.ReplaceItemAsync(resp.Resource, id, pk, ro, ct).ConfigureAwait(false);
                            return Result.Success;
                        });
                }

                return await Result
                    .Go(IsAuthorized(resp.Resource))
                    .ThenAsync(async () =>
                    {
                        ro.SessionToken = resp.Headers?.Session;
                        await Container.DeleteItemAsync<TModel>(id, pk, ro, ct).ConfigureAwait(false);
                        return Result.Success;
                    });
            }
            catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound) { return Result.NotFoundError(); }
        }, cancellationToken, nameof(DeleteWithResultAsync));
    }
}