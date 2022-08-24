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
    /// Provides <see cref="Container"/> <see cref="CosmosDbValue{TModel}"/> operations for a specified <see cref="CosmosDb"/> and <see cref="DbArgs"/>.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <remarks>Represents a special-purpose <b>CosmosDb/DocumentDb</b> <see cref="Container"/> that houses an underlying <see cref="CosmosDbValue{TModel}.Value"/>, including <see cref="CosmosDbValue{TModel}.Type"/> name, and flexible <see cref="IIdentifier"/>, for persistence.</remarks>
    public class CosmosDbValueContainer<T, TModel> : ICosmosDbContainer<T, TModel> where T : class, new() where TModel : class, IIdentifier, new()
    {
        private readonly string _typeName = typeof(TModel).Name;
        private Func<T, PartitionKey>? _partitionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValueContainer{T, TModel}"/> class.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        public CosmosDbValueContainer(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null)
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
        /// <returns>The <see cref="CosmosDbValueContainer{T, TModel}"/> to support fluent-style method-chaining.</returns>
        public CosmosDbValueContainer<T, TModel> UsePartitionKey(Func<T, PartitionKey> partitionKey)
        {
            _partitionKey = partitionKey;
            return this;
        }

        /// <summary>
        /// Gets the <b>value</b> from the response updating any special properties as required.
        /// </summary>
        /// <param name="resp">The response value.</param>
        /// <returns>The entity value.</returns>
        internal T? GetResponseValue(Response<CosmosDbValue<TModel>> resp)
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
        internal T GetValue(CosmosDbValue<TModel> model)
        {
            ((ICosmosDbValue)model).PrepareAfter(CosmosDb);
            return CosmosDb.Mapper.Map<TModel, T>(model.Value, OperationTypes.Get)!;
        }

        /// <summary>
        /// Check the value to determine whether users are authorised using the CosmosDbArgs.AuthorizationFilter.
        /// </summary>
        private void CheckAuthorized(CosmosDbValue<TModel> model)
        {
            if (model != null && model.Value != default)
            {
                var filter = CosmosDb.GetAuthorizeFilter<TModel>(Container.Id);
                if (filter != null && !((IQueryable<CosmosDbValue<TModel>>)filter(new CosmosDbValue<TModel>[] { model }.AsQueryable())).Any())
                    throw new AuthorizationException();
            }
        }

        /// <summary>
        /// Gets the <b>CosmosDb/DocumentDb</b> key from the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The entity value.</param>
        /// <returns>The <b>CosmosDb/DocumentDb</b> key.</returns>
        public string? GetCosmosKey(T value) => value switch
        {
            IIdentifier si => CosmosDb.FormatIdentifier(si.Id),
            IPrimaryKey pk => pk.PrimaryKey.Args.Length == 1 ? CosmosDb.FormatIdentifier(pk.PrimaryKey.Args[0]) : throw new NotSupportedException("Only a single key value is supported."),
            _ => throw new NotSupportedException("Only a value that implements IIdentifier or IPrimaryKey is supported")
        };

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> Query(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => new(this, query);

        /// <summary>
        /// Creates a <see cref="CosmosDbQuery{T, TModel}"/> and returns the corresponding <see cref="CosmosDbQuery{T, TModel}.AsQueryable()"/> to enable ad-hoc LINQ-style queries.
        /// </summary>
        /// <returns>An <see cref="IQueryable{T}"/>.</returns>
        public IQueryable<CosmosDbValue<TModel>> AsQueryable() => new CosmosDbValueQuery<T, TModel>(this, null).AsQueryable();

        /// <summary>
        /// Gets the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="ICosmosDb.PartitionKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFoundResponse"/>).</returns>
        public Task<T?> GetAsync(object id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => GetAsync(CosmosDb.FormatIdentifier(id)!, partitionKey, cancellationToken);

        /// <inheritdoc/>
        public async Task<T?> GetAsync(string id, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => await CosmosDb.Invoker.InvokeAsync(CosmosDb, id, async (key, ct) =>
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            try
            {
                var val = await Container.ReadItemAsync<CosmosDbValue<TModel>>(key, partitionKey ?? CosmosDb.PartitionKey ?? PartitionKey.None, CosmosDb.GetItemRequestOptions<T, TModel>(DbArgs), ct).ConfigureAwait(false);

                // Check that the TypeName is the same.
                if (val?.Resource == null || val.Resource.Type != _typeName)
                {
                    if (!DbArgs.HasValue || DbArgs.Value.NullOnNotFoundResponse)
                        return null;
                    else
                        throw new NotFoundException();
                }

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

                var cvm = new CosmosDbValue<TModel>(model!);
                CheckAuthorized(cvm);
                ((ICosmosDbValue)cvm).PrepareBefore(CosmosDb);

                var resp = await Container.CreateItemAsync(cvm, GetPartitionKey(v), CosmosDb.GetItemRequestOptions<T, TModel>(DbArgs), ct).ConfigureAwait(false);
                return GetResponseValue(resp)!;
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

                var key = GetCosmosKey(v);
                var pk = GetPartitionKey(v);

                // Must read existing to update.
                var resp = await Container.ReadItemAsync<CosmosDbValue<TModel>>(key, pk, ro, ct).ConfigureAwait(false);
                if (resp?.Resource == null || resp.Resource.Type != _typeName)
                    throw new NotFoundException();

                CheckAuthorized(resp.Resource);
                if (v is IETag etag2 && etag2.ETag != null && ETagGenerator.FormatETag(etag2.ETag) != resp.ETag)
                    throw new ConcurrencyException();

                ro.SessionToken = resp.Headers?.Session;
                ChangeLog.PrepareUpdated(v);
                CosmosDb.Mapper.Map(v, resp.Resource.Value, OperationTypes.Update);
                ((ICosmosDbValue)resp.Resource).PrepareBefore(CosmosDb);

                // Re-check auth to make sure not updating to something not allowed.
                CheckAuthorized(resp);

                resp = await Container.ReplaceItemAsync(resp.Resource, key, pk, ro, ct).ConfigureAwait(false);
                return GetResponseValue(resp)!;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deleted the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="ICosmosDb.PartitionKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(object id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => DeleteAsync(CosmosDb.FormatIdentifier(id)!, partitionKey, cancellationToken);

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => await CosmosDb.Invoker.InvokeAsync(CosmosDb, id, async (key, ct) =>
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            try
            {
                // Must read existing to delete and to make sure we are deleting for the correct Type; don't just trust the key.
                var ro = CosmosDb.GetItemRequestOptions<T, TModel>(DbArgs);
                var pk = partitionKey ?? CosmosDb.PartitionKey ?? PartitionKey.None;
                var resp = await Container.ReadItemAsync<CosmosDbValue<TModel>>(key, pk, ro, ct).ConfigureAwait(false);
                if (resp?.Resource == null || resp.Resource.Type != _typeName)
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