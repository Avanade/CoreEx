// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides <see cref="Container"/> operations for a <see cref="CosmosDb"/> container.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
    /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
    /// <param name="dbArgs">The optional <see cref="CosmosDbArgs"/>.</param>
    public class CosmosDbContainer<T, TModel>(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null) : CosmosDbContainerBase<T, TModel, CosmosDbContainer<T, TModel>>(cosmosDb, containerId, dbArgs) where T : class, IEntityKey, new() where TModel : class, IIdentifier<string>, new()
    {
        /// <summary>
        /// Gets the <b>value</b> from the response updating any special properties as required.
        /// </summary>
        /// <param name="resp">The response value.</param>
        /// <returns>The entity value.</returns>
        internal T? GetResponseValue(Response<TModel> resp)
        {
            if (resp?.Resource == null)
                return default;

            return GetValue(resp.Resource);
        }

        /// <summary>
        /// Gets the <b>value</b> formatting/updating any special properties as required.
        /// </summary>
        /// <param>The model value.</param>
        /// <returns>The entity value.</returns>
        internal T GetValue(TModel model)
        {
            var val = CosmosDb.Mapper.Map<TModel, T>(model, OperationTypes.Get)!;
            if (val is IETag et && et.ETag != null)
                et.ETag = ETagGenerator.ParseETag(et.ETag);

            return DbArgs.CleanUpResult ? Cleaner.Clean(val) : val;
        }

        /// <summary>
        /// Check the value to determine whether users are authorised using the CosmosDb.AuthorizationFilter.
        /// </summary>
        private Result CheckAuthorized(TModel model)
        {
            if (model != default)
            {
                var filter = CosmosDb.GetAuthorizeFilter<TModel>(Container.Id);
                if (filter != null && !((IQueryable<TModel>)filter(new TModel[] { model }.AsQueryable())).Any())
                    return Result.AuthorizationError();
            }

            return Result.Success;
        }

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(Func<IQueryable<TModel>, IQueryable<TModel>>? query) => Query(new CosmosDbArgs(DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(PartitionKey? partitionKey = null, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => Query(new CosmosDbArgs(DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => new(this, dbArgs, query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> ModelQuery(Func<IQueryable<TModel>, IQueryable<TModel>>? query) => ModelQuery(new CosmosDbArgs(DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> ModelQuery(PartitionKey? partitionKey = null, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => ModelQuery(new CosmosDbArgs(DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbModelQuery{TModel}"/>.</returns>
        public CosmosDbModelQuery<TModel> ModelQuery(CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => new(this, dbArgs, query);

        /// <inheritdoc/>
        public override Task<Result<T?>> GetWithResultAsync(object? id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, GetCosmosId(id), dbArgs, async (_, key, args, ct) =>
        {
            try
            {
                var resp = await Container.ReadItemAsync<TModel>(key, args.PartitionKey ?? DbArgs.PartitionKey ?? PartitionKey.None, CosmosDb.GetItemRequestOptions<T, TModel>(args), ct).ConfigureAwait(false);
                if (resp.Resource == null || (args.FilterByTenantId && resp.Resource is ITenantId tenantId && tenantId.TenantId != DbArgs.GetTenantId()) || (resp.Resource is ILogicallyDeleted ld && ld.IsDeleted.HasValue && ld.IsDeleted.Value))
                    return args.NullOnNotFound ? Result<T?>.None : Result<T?>.NotFoundError();

                return Result.Go(CheckAuthorized(resp)).ThenAs(() => GetResponseValue(resp));
            }
            catch (CosmosException dcex) when (args.NullOnNotFound && dcex.StatusCode == System.Net.HttpStatusCode.NotFound) { return args.NullOnNotFound ? Result<T?>.None : Result<T?>.NotFoundError(); }
        }, cancellationToken, nameof(GetWithResultAsync));

        /// <inheritdoc/>
        public override Task<Result<T>> CreateWithResultAsync(T value, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, value.ThrowIfNull(nameof(value)), dbArgs, async (_, v, args, ct) =>
        {
            var pk = GetPartitionKey(v);
            ChangeLog.PrepareCreated(v);
            TModel model = CosmosDb.Mapper.Map<T, TModel>(v, OperationTypes.Create)!;

            Cleaner.ResetTenantId(model);

            return await Result
                .Go(CheckAuthorized(model))
                .ThenAsAsync(() => Container.CreateItemAsync(model, pk, CosmosDb.GetItemRequestOptions<T, TModel>(args), ct))
                .ThenAs(resp => GetResponseValue(resp!)!);
        }, cancellationToken, nameof(CreateWithResultAsync));

        /// <inheritdoc/>
        public override Task<Result<T>> UpdateWithResultAsync(T value, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, value.ThrowIfNull(nameof(value)), dbArgs, async (_,v, args, ct) =>
        {
            var key = GetCosmosId(v);
            var pk = GetPartitionKey(v);
            
            // Where supporting etag then use IfMatch for concurreny.
            var ro = CosmosDb.GetItemRequestOptions<T, TModel>(args);
            if (ro.IfMatchEtag == null && v is IETag etag && etag.ETag != null)
                ro.IfMatchEtag = ETagGenerator.FormatETag(etag.ETag);

            // Must read existing to update.
            var resp = await Container.ReadItemAsync<TModel>(key, pk, ro, ct).ConfigureAwait(false);
            if (resp.Resource == null || (args.FilterByTenantId && resp.Resource is ITenantId tenantId && tenantId.TenantId != DbArgs.GetTenantId()) || (resp.Resource is ILogicallyDeleted ld && ld.IsDeleted.HasValue && ld.IsDeleted.Value))
                return Result<T>.NotFoundError();

            return await Result
                .Go(CheckAuthorized(resp))
                .When(() => v is IETag etag2 && etag2.ETag != null && ETagGenerator.FormatETag(etag2.ETag) != resp.ETag, () => Result.ConcurrencyError())
                .Then(() =>
                {
                    ro.SessionToken = resp.Headers?.Session;
                    ChangeLog.PrepareUpdated(v);
                    CosmosDb.Mapper.Map(v, resp.Resource, OperationTypes.Update);
                    Cleaner.ResetTenantId(resp.Resource);

                    // Re-check auth to make sure not updating to something not allowed.
                    return CheckAuthorized(resp);
                })
                .ThenAsAsync(async () =>
                {
                    resp = await Container.ReplaceItemAsync(resp.Resource, key, pk, ro, ct).ConfigureAwait(false);
                    return GetResponseValue(resp)!;
                });
        }, cancellationToken, nameof(UpdateWithResultAsync));

        /// <inheritdoc/>
        public override Task<Result> DeleteWithResultAsync(object? id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, GetCosmosId(id), dbArgs, async (_, key, args, ct) =>
        {
            try
            {
                // Must read the existing to validate.
                var ro = CosmosDb.GetItemRequestOptions<T, TModel>(args);
                var pk = dbArgs.PartitionKey ?? DbArgs.PartitionKey ?? PartitionKey.None;
                var resp = await Container.ReadItemAsync<TModel>(key, pk, ro, ct).ConfigureAwait(false);
                if (resp.Resource == null || (args.FilterByTenantId && resp.Resource is ITenantId tenantId && tenantId.TenantId != DbArgs.GetTenantId()) || (resp.Resource is ILogicallyDeleted ld && ld.IsDeleted.HasValue && ld.IsDeleted.Value))
                    return Result.Success;

                // Delete; either logically or physically.
                if (resp.Resource is ILogicallyDeleted ild)
                {
                    ild.IsDeleted = true;
                    return await Result
                        .Go(CheckAuthorized(resp.Resource))
                        .ThenAsync(async () =>
                        {
                            ro.SessionToken = resp.Headers?.Session;
                            await Container.ReplaceItemAsync(resp.Resource, key, pk, ro, ct).ConfigureAwait(false);
                            return Result.Success;
                        });
                }

                return await Result
                    .Go(CheckAuthorized(resp.Resource))
                    .ThenAsync(async () =>
                    {
                        ro.SessionToken = resp.Headers?.Session;
                        await Container.DeleteItemAsync<T>(key, pk, ro, ct).ConfigureAwait(false);
                        return Result.Success;
                    });
            }
            catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound) { return Result.NotFoundError(); }
        }, cancellationToken, nameof(DeleteWithResultAsync));
    }
}