// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.OData.Mapping;
using CoreEx.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Soc = Simple.OData.Client;

namespace CoreEx.OData
{
    /// <summary>
    /// Enables the common <see cref="ODataItem"/>-collection CRUD functionality.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <param name="client">The owning <see cref="Owner"/>.</param>
    /// <param name="args">The <see cref="ODataArgs"/>.</param>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="mapper">The specific <see cref="IODataMapper{TSource}"/>.</param>
    public class ODataItemCollection<T>(ODataClient client, ODataArgs args, string collectionName, IODataMapper<T> mapper) where T : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataItemCollection{T}"/> class.
        /// </summary>
        /// <param name="client">The owning <see cref="Owner"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="mapper">The specific <see cref="IODataMapper{TSource}"/>.</param>
        public ODataItemCollection(ODataClient client, string collectionName, IODataMapper<T> mapper) : this(client, new ODataArgs(client.ThrowIfNull().Args), collectionName, mapper) { }

        /// <summary>
        /// Gets the owning <see cref="ODataClient"/>.
        /// </summary>
        public ODataClient Owner { get; } = client.ThrowIfNull(nameof(client));

        /// <summary>
        /// Gets the <see cref="ODataArgs"/>.
        /// </summary>
        public ODataArgs Args { get; } = args;

        /// <summary>
        /// Gets the collection name.
        /// </summary>
        public string CollectionName { get; } = collectionName.ThrowIfNull(nameof(collectionName));

        /// <summary>
        /// Gets the <see cref="IODataMapper{TSource}"/>.
        /// </summary>
        public IODataMapper<T> Mapper { get; } = mapper.ThrowIfNull(nameof(mapper));

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from the <see cref="ODataItem"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<T?> GetAsync(params object[] key) => GetAsync(key, default);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from the <see cref="ODataItem"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public async Task<T?> GetAsync(object[] key, CancellationToken cancellationToken = default) => (await GetWithResultAsync(key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from the <see cref="ODataItem"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<Result<T?>> GetWithResultAsync(params object[] key) => GetWithResultAsync(key, default);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from the <see cref="ODataItem"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public async Task<Result<T?>> GetWithResultAsync(object[] key, CancellationToken cancellationToken = default) => await Owner.Invoker.InvokeAsync(this, async (_, ct) =>
        {
            return (await GetItemAsync(key, ct).ConfigureAwait(false))
                .WhenAs<ODataItem?, T?>(entity => entity is null, _ => default!, entity => MapFromOData(entity!, OperationTypes.Get));
        }, Owner, cancellationToken);

        /// <summary>
        /// Creates the entity with a <see cref="Result{T}"/> mapping from/to an intermediary untyped <see cref="ODataItem"/>.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public async Task<T> CreateAsync(T value, CancellationToken cancellationToken = default) => (await CreateWithResultAsync(value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Creates the entity with a <see cref="Result{T}"/> mapping from/to an intermediary untyped <see cref="ODataItem"/>.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public async Task<Result<T>> CreateWithResultAsync(T value, CancellationToken cancellationToken = default) => await Owner.Invoker.InvokeAsync(this, async (_, ct) =>
        {
            var item = ODataItem.MapFrom(Mapper, Cleaner.PrepareCreate(value.ThrowIfNull()), OperationTypes.Create);
            Mapper.MapToOData(value, item, OperationTypes.Create);
            var created = await Owner.Client.For(CollectionName).Set(item.Attributes).InsertEntryAsync(true, ct).ConfigureAwait(false);
            return created is null ? Result<T>.NotFoundError() : Result<T>.Ok(MapFromOData(new ODataItem(created), OperationTypes.Get));
        }, Owner, cancellationToken);

        /// <summary>
        /// Updates the entity with a <see cref="Result{T}"/> mapping from/to an intermediary untyped <see cref="ODataItem"/>.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public async Task<T> UpdateAsync(T value, CancellationToken cancellationToken = default) => (await UpdateWithResultAsync(value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Updates the entity with a <see cref="Result{T}"/> mapping from/to an intermediary untyped <see cref="ODataItem"/>.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public async Task<Result<T>> UpdateWithResultAsync(T value, CancellationToken cancellationToken = default) => await Owner.Invoker.InvokeAsync(this, async (_, ct) =>
        {
            ODataItem item;
            var key = Mapper.GetODataKey(Cleaner.PrepareUpdate(value.ThrowIfNull()), OperationTypes.Update);

            if (Args.PreReadOnUpdate)
            {
                var get = (await GetItemAsync(key, ct).ConfigureAwait(false))
                    .When(v => v is null, _ => Result.NotFoundError());

                if (get.IsFailure)
                    return get.AsResult();

                item = get.Value;
                Mapper.MapToOData(value, get.Value, OperationTypes.Update);
            }
            else
                item = ODataItem.MapFrom(Mapper, value, OperationTypes.Update);

            var updated = await Owner.Client.For(CollectionName).Key(key).Set(item.Attributes).UpdateEntryAsync(true, ct).ConfigureAwait(false);
            return updated is null ? Result<T>.NotFoundError() : Result<T>.Ok(MapFromOData(new ODataItem(updated), OperationTypes.Get));
        }, Owner, cancellationToken);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        public Task DeleteAsync(params object[] key) => DeleteAsync(key, default);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task DeleteAsync(object[] key, CancellationToken cancellationToken = default) => (await DeleteWithResultAsync(key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        public Task<Result> DeleteWithResultAsync(params object[] key) => DeleteWithResultAsync(key, default);

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task<Result> DeleteWithResultAsync(object[] key, CancellationToken cancellationToken = default) => await Owner.Invoker.InvokeAsync(this, async (_, ct) =>
        {
            await Owner.Client.For(CollectionName).Key(key).DeleteEntryAsync(ct).ConfigureAwait(false);
            return Result.Success;
        }, Owner, cancellationToken);

        /// <summary>
        /// Gets (reads) the entity.
        /// </summary>
        private async Task<Result<ODataItem?>> GetItemAsync(object[] key, CancellationToken cancellationToken = default)
        {
            try
            {
                return Result.Go(await Owner.Client.For(CollectionName).Key(key).FindEntryAsync(cancellationToken).ConfigureAwait(false))
                    .WhenAs<IDictionary<string, object>, ODataItem?>(d => d is null, _ => Args.NullOnNotFound ? default! : Result.NotFoundError(), d => new ODataItem(d));
            }
            catch (Soc.WebRequestException odex) when (odex.Code == HttpStatusCode.NotFound && Args.NullOnNotFound) { return default!; }
        }

        /// <summary>
        /// Maps from the <paramref name="item"/> to the value.
        /// </summary>
        /// <param name="item">The <see cref="ODataItem"/>.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The value.</returns>
        public T MapFromOData(ODataItem item, OperationTypes operationType = OperationTypes.Unspecified) => item.MapTo(Mapper, operationType);

        /// <summary>
        /// Maps from the <paramref name="value"/> to the <see cref="ODataItem"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The <see cref="ODataItem"/>.</returns>
        public ODataItem MapToOData(T value, OperationTypes operationType = OperationTypes.Unspecified) => ODataItem.MapFrom(Mapper, value.ThrowIfNull(), operationType);

        /// <summary>
        /// Invokes a <paramref name="clientFunc"/> function (wrapping with the underlying <see cref="ODataClient.Invoker"/>).
        /// </summary>
        /// <param name="clientFunc">The customized untyped <see cref="Soc.IBoundClient{T}"/> function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task InvokeAsync(Func<Soc.IBoundClient<IDictionary<string, object>>, Task> clientFunc, CancellationToken cancellationToken = default)
            => InvokeWithResultAsync(async client => { await clientFunc(client).ConfigureAwait(false); return Result.Success; }, cancellationToken);

        /// <summary>
        /// Invokes a <paramref name="clientFunc"/> function (wrapping with the underlying <see cref="ODataClient.Invoker"/>) returning a value with a <see cref="Type"/> of <typeparamref name="TValue"/>
        /// </summary>
        /// <typeparam name="TValue">The returning value <see cref="Type"/>.</typeparam>
        /// <param name="clientFunc">The customized untyped <see cref="Soc.IBoundClient{T}"/> function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value.</returns>
        public async Task<TValue> InvokeAsync<TValue>(Func<Soc.IBoundClient<IDictionary<string, object>>, Task<TValue>> clientFunc, CancellationToken cancellationToken = default)
            => (await InvokeWithResultAsync<TValue>(async client => { var v = await clientFunc(client).ConfigureAwait(false); return Result.Ok<TValue>(v); }, cancellationToken)).Value;

        /// <summary>
        /// Invokes a <paramref name="clientFunc"/> function (wrapping with the underlying <see cref="ODataClient.Invoker"/>).
        /// </summary>
        /// <param name="clientFunc">The customized untyped <see cref="Soc.IBoundClient{T}"/> function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task<Result> InvokeWithResultAsync(Func<Soc.IBoundClient<IDictionary<string, object>>, Task<Result>> clientFunc, CancellationToken cancellationToken = default) => await Owner.Invoker.InvokeAsync(this, async (_, ct) =>
        {
            var client = Owner.Client.For(CollectionName);
            return await clientFunc(client).ConfigureAwait(false);
        }, Owner, cancellationToken);

        /// <summary>
        /// Invokes a <paramref name="clientFunc"/> function (wrapping with the underlying <see cref="ODataClient.Invoker"/>) returning a value with a <see cref="Type"/> of <typeparamref name="TValue"/>
        /// </summary>
        /// <typeparam name="TValue">The returning value <see cref="Type"/>.</typeparam>
        /// <param name="clientFunc">The customized untyped <see cref="Soc.IBoundClient{T}"/> function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value.</returns>
        public async Task<Result<TValue>> InvokeWithResultAsync<TValue>(Func<Soc.IBoundClient<IDictionary<string, object>>, Task<Result<TValue>>> clientFunc, CancellationToken cancellationToken = default) => await Owner.Invoker.InvokeAsync(this, async (_, ct) =>
        {
            var client = Owner.Client.For(CollectionName);
            return await clientFunc(client).ConfigureAwait(false);
        }, Owner, cancellationToken);
    }
}