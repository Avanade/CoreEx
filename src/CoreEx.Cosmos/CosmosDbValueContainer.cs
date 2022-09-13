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
    /// Provides <see cref="Container"/> <see cref="CosmosDbValue{TModel}"/> operations for a specified <see cref="CosmosDb"/> container.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <remarks>Represents a special-purpose <b>CosmosDb/DocumentDb</b> <see cref="Container"/> that houses an underlying <see cref="CosmosDbValue{TModel}.Value"/>, including <see cref="CosmosDbValue{TModel}.Type"/> name, and flexible <see cref="IIdentifier"/>, for persistence.</remarks>
    public class CosmosDbValueContainer<T, TModel> : CosmosDbContainerBase<T, TModel, CosmosDbValueContainer<T, TModel>> where T : class, new() where TModel : class, IIdentifier, new()
    {
        private readonly string _typeName = typeof(TModel).Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValueContainer{T, TModel}"/> class.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        public CosmosDbValueContainer(ICosmosDb cosmosDb, string containerId) : base(cosmosDb, containerId) { }

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
        public CosmosDbValueQuery<T, TModel> Query(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) => Query(new CosmosDbArgs(CosmosDb.DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> Query(PartitionKey? partitionKey = null, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => Query(new CosmosDbArgs(CosmosDb.DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> Query(CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => new(this, dbArgs, query);

        /// <summary>
        /// Gets the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="ICosmosDb.PartitionKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFoundResponse"/>).</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1061:Do not hide base class methods", Justification = "By design, to enable identifier flexibility.")]
        public Task<T?> GetAsync(object? id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => GetAsync(id, new CosmosDbArgs(CosmosDb.DbArgs, partitionKey), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFoundResponse"/>).</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1061:Do not hide base class methods", Justification = "By design, to enable identifier flexibility.")]
        public Task<T?> GetAsync(object? id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => GetInternalAsync(CosmosDb.FormatIdentifier(id), dbArgs, cancellationToken);

        /// <inheritdoc/>
        public override Task<T?> GetAsync(string id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => GetInternalAsync(id, dbArgs, cancellationToken);

        /// <summary>
        /// Performs the actual get.
        /// </summary>
        public Task<T?> GetInternalAsync(string id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, id ?? throw new ArgumentNullException(nameof(id)), dbArgs, async (key, args, ct) =>
        {
            try
            {
                var val = await Container.ReadItemAsync<CosmosDbValue<TModel>>(key, args.PartitionKey ?? CosmosDb.PartitionKey ?? PartitionKey.None, CosmosDb.GetItemRequestOptions<T, TModel>(args), ct).ConfigureAwait(false);

                // Check that the TypeName is the same.
                if (val?.Resource == null || val.Resource.Type != _typeName)
                {
                    if (args.NullOnNotFoundResponse)
                        return null;
                    else
                        throw new NotFoundException();
                }

                CheckAuthorized(val);
                return GetResponseValue(val);
            }
            catch (CosmosException dcex) when (args.NullOnNotFoundResponse && dcex.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
        }, cancellationToken);

        /// <inheritdoc/>
        public override Task<T> CreateAsync(T value, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, value ?? throw new ArgumentNullException(nameof(value)), dbArgs, async (v, args, ct) =>
        {
            var pk = GetPartitionKey(v);
            ChangeLog.PrepareCreated(v);
            TModel model = CosmosDb.Mapper.Map<T, TModel>(v, OperationTypes.Create)!;

            var cvm = new CosmosDbValue<TModel>(model!);
            CheckAuthorized(cvm);
            ((ICosmosDbValue)cvm).PrepareBefore(CosmosDb);

            var resp = await Container.CreateItemAsync(cvm, pk, CosmosDb.GetItemRequestOptions<T, TModel>(args), ct).ConfigureAwait(false);
            return GetResponseValue(resp)!;
        }, cancellationToken);

        /// <inheritdoc/>
        public override Task<T> UpdateAsync(T value, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, value ?? throw new ArgumentNullException(nameof(value)), dbArgs, async (v, args, ct) =>
        {
            var key = GetCosmosKey(v);
            var pk = GetPartitionKey(v);

            // Where supporting etag then use IfMatch for concurreny.
            var ro = CosmosDb.GetItemRequestOptions<T, TModel>(args);
            if (ro.IfMatchEtag == null && v is IETag etag && etag.ETag != null)
                ro.IfMatchEtag = ETagGenerator.FormatETag(etag.ETag);

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
        }, cancellationToken);

        /// <summary>
        /// Deleted the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="ICosmosDb.PartitionKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1061:Do not hide base class methods", Justification = "By design, to enable identifier flexibility.")]
        public Task DeleteAsync(object? id, PartitionKey? partitionKey = null, CancellationToken cancellationToken = default) => DeleteAsync(id, new CosmosDbArgs(CosmosDb.DbArgs, partitionKey), cancellationToken);

        /// <summary>
        /// Deleted the entity for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1061:Do not hide base class methods", Justification = "By design, to enable identifier flexibility.")]
        public Task DeleteAsync(object? id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => DeleteInternalAsync(CosmosDb.FormatIdentifier(id), dbArgs, cancellationToken);

        /// <inheritdoc/>
        public override Task DeleteAsync(string id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => DeleteInternalAsync(id, dbArgs, cancellationToken);

        /// <summary>
        /// Performs the actual delete.
        /// </summary>
        private Task DeleteInternalAsync(string id, CosmosDbArgs dbArgs, CancellationToken cancellationToken = default) => CosmosDb.Invoker.InvokeAsync(CosmosDb, id ?? throw new ArgumentNullException(nameof(id)), dbArgs, async (key, args, ct) =>
        {
            try
            {
                // Must read existing to delete and to make sure we are deleting for the correct Type; don't just trust the key.
                var ro = CosmosDb.GetItemRequestOptions<T, TModel>(args);
                var pk = dbArgs.PartitionKey ?? CosmosDb.PartitionKey ?? PartitionKey.None;
                var resp = await Container.ReadItemAsync<CosmosDbValue<TModel>>(key, pk, ro, ct).ConfigureAwait(false);
                if (resp?.Resource == null || resp.Resource.Type != _typeName)
                    throw new NotFoundException();

                CheckAuthorized(resp.Resource);
                ro.SessionToken = resp.Headers?.Session;
                await Container.DeleteItemAsync<T>(key, pk, ro, ct).ConfigureAwait(false);
            }
            catch (CosmosException cex) when (cex.StatusCode == System.Net.HttpStatusCode.NotFound) { throw new NotFoundException(); }
        }, cancellationToken);
    }
}