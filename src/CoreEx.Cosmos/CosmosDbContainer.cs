// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Mapping;
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
    public class CosmosDbContainer<T, TModel> : ICosmosDbContainer<T, TModel> where T : class, new() where TModel : class, IIdentifier<string>, new()
    {
        private Func<T, PartitionKey>? _partitionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbContainer{T, TModel}"/> class.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        public CosmosDbContainer(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null)
        {
            CosmosDb = cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb));
            Container = cosmosDb.GetCosmosContainer(containerId);
            DbArgs = dbArgs;
        }

        /// <inheritdoc/>
        public ICosmosDb CosmosDb { get; }

        /// <inheritdoc/>
        public CosmosDbArgs? DbArgs { get; }

        /// <inheritdoc/>
        public Container Container { get; }

        /// <inheritdoc/>
        public PartitionKey GetPartitionKey(T value) => _partitionKey?.Invoke(value) ?? CosmosDb.PartitionKey ?? PartitionKey.None;

        /// <summary>
        /// Sets the function to determine the <see cref="PartitionKey"/>; used for <see cref="GetPartitionKey(T)"/>.
        /// </summary>
        /// <param name="partitionKey">The function to determine the <see cref="PartitionKey"/>.</param>
        /// <returns>The <see cref="CosmosDbContainer{T, TModel}"/> to support fluent-style method-chaining.</returns>
        public CosmosDbContainer<T, TModel> UsePartitionKey(Func<T, PartitionKey> partitionKey)
        {
            _partitionKey = partitionKey;
            return this;
        }

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

            return val;
        }

        /// <summary>
        /// Check the value to determine whether users are authorised using the CosmosDbArgs.AuthorizationFilter.
        /// </summary>
        private void CheckAuthorized(TModel model)
        {
            if (model != default)
            {
                var filter = CosmosDb.GetAuthorizeFilter<TModel>(Container.Id);
                if (filter != null && !((IQueryable<TModel>)filter(new TModel[] { model }.AsQueryable())).Any())
                    throw new AuthorizationException();
            }
        }

        /// <summary>
        /// Gets the <b>CosmosDb/DocumentDb</b> key from the <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns>The <b>CosmosDb/DocumentDb</b> key.</returns>
        public string? GetCosmosKey(CompositeKey key) => key.Args.Length == 1 && key.Args[0] is string k ? k : throw new NotSupportedException("Only a single key value that is a string is supported.");

        /// <summary>
        ///  Gets the <b>CosmosDb/DocumentDb</b> key from the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The entity value.</param>
        /// <returns>The <b>CosmosDb/DocumentDb</b> key.</returns>
        public string? GetCosmosKey(T value) => value switch
        {
            IIdentifier<string> si => si.Id,
            IPrimaryKey pk => pk.PrimaryKey.Args.Length == 1 && pk.PrimaryKey.Args[0] is string k ? k : throw new NotSupportedException("Only a single key value that is a string is supported."),
            _ => throw new NotSupportedException("Only a value that implements IIdentifier<string> or IPrimaryKey is supported")
        };

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => new(this, query);

        /// <summary>
        /// Creates a <see cref="CosmosDbQuery{T, TModel}"/> and returns the corresponding <see cref="CosmosDbQuery{T, TModel}.AsQueryable()"/> to enable ad-hoc LINQ-style queries.
        /// </summary>
        /// <returns>An <see cref="IQueryable{T}"/>.</returns>
        public IQueryable<TModel> AsQueryable() => new CosmosDbQuery<T, TModel>(this, null).AsQueryable();

        /// <inheritdoc/>
        public async Task<T?> GetAsync(string id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => await CosmosDb.Invoker.InvokeAsync(CosmosDb, id, async (key, ct) =>
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            try
            {
                var val = await Container.ReadItemAsync<TModel>(key, partitionKey ?? CosmosDb.PartitionKey ?? PartitionKey.None, CosmosDb.GetItemRequestOptions<T, TModel>(DbArgs), ct).ConfigureAwait(false);
                CheckAuthorized(val);
                return GetResponseValue(val);
            }
            catch (CosmosException dcex)
            {
                if ((!DbArgs.HasValue || DbArgs.Value.NullOnNotFoundResponse) && dcex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                throw;
            }
        }, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc/>
        public async Task<T> CreateAsync(T value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return await CosmosDb.Invoker.InvokeAsync(CosmosDb, value, async (v, ct) =>
            {
                ChangeLog.PrepareCreated(v);
                TModel model = CosmosDb.Mapper.Map<T, TModel>(v, OperationTypes.Create)!;

                Cleaner.ResetTenantId(model);
                CheckAuthorized(model);

                var resp = await Container.CreateItemAsync(model, GetPartitionKey(v), CosmosDb.GetItemRequestOptions<T, TModel>(DbArgs), ct).ConfigureAwait(false);
                return GetResponseValue(resp!)!;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<T> UpdateAsync(T value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return await CosmosDb.Invoker.InvokeAsync(CosmosDb, value, async (v, ct) =>
            {
                // Where supporting etag then use IfMatch for concurreny.
                var ro = CosmosDb.GetItemRequestOptions<T, TModel>(DbArgs);
                if (ro.IfMatchEtag == null && v is IETag etag && etag.ETag != null)
                    ro.IfMatchEtag = ETagGenerator.FormatETag(etag.ETag);

                // Must read existing to update.
                var key = GetCosmosKey(v);
                var pk = GetPartitionKey(v);
                var resp = await Container.ReadItemAsync<TModel>(key, pk, ro, ct).ConfigureAwait(false);

                CheckAuthorized(resp);
                if (v is IETag etag2 && etag2.ETag != null && ETagGenerator.FormatETag(etag2.ETag) != resp.ETag)
                    throw new ConcurrencyException();

                ro.SessionToken = resp.Headers?.Session;
                ChangeLog.PrepareUpdated(v);
                CosmosDb.Mapper.Map(v, resp.Resource, OperationTypes.Update);
                Cleaner.ResetTenantId(resp.Resource);

                // Re-check auth to make sure not updating to something not allowed.
                CheckAuthorized(resp);

                resp = await Container.ReplaceItemAsync(resp.Resource, key, pk, ro, ct).ConfigureAwait(false);
                return GetResponseValue(resp)!;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => await CosmosDb.Invoker.InvokeAsync(CosmosDb, id, async (key, ct) =>
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            try
            {
                // Must read the existing to validate.
                var ro = CosmosDb.GetItemRequestOptions<T, TModel>(DbArgs);
                var pk = partitionKey ?? CosmosDb.PartitionKey ?? PartitionKey.None;
                var resp = await Container.ReadItemAsync<TModel>(key, pk, ro, ct).ConfigureAwait(false);
                if (resp?.Resource == null)
                    throw new NotFoundException();

                CheckAuthorized(resp.Resource);
                ro.SessionToken = resp.Headers?.Session;
                await Container.DeleteItemAsync<T>(key, pk, ro, ct).ConfigureAwait(false);
            }
            catch (CosmosException cex)
            {
                if (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new NotFoundException();

                throw;
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}