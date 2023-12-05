// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
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
    /// Enables the common <see cref="ODataItem"/>-collection functionality.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    public class ODataItemCollection<T> where T : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataItemCollection{T}"/> class.
        /// </summary>
        /// <param name="client">The owning <see cref="Owner"/>.</param>
        /// <param name="args">The <see cref="ODataArgs"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        internal ODataItemCollection(ODataClient client, ODataArgs args, string collectionName)
        {
            Owner = client ?? throw new ArgumentNullException(nameof(client));
            CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
            Args = args;
        }

        /// <summary>
        /// Gets the owning <see cref="ODataClient"/>.
        /// </summary>
        public ODataClient Owner { get; }

        /// <summary>
        /// Gets the <see cref="ODataArgs"/>.
        /// </summary>
        public ODataArgs Args { get; }

        /// <summary>
        /// Gets the collection name.
        /// </summary>
        public string CollectionName { get; }

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
                .WhenAs<ODataItem?, T?>(entity => entity is null, _ => default!, entity => MapToValue(entity!));
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
        /// Maps from the entity to the value.
        /// </summary>
        private T MapToValue(ODataItem entity)
        {
            var result = Owner.Mapper.Map<T>(entity, OperationTypes.Get);
            return (result is not null) ? ODataClient.CleanUpResult(Args, result) : throw new InvalidOperationException("Mapping from the OData entity must not result in a null value.");
        }
    }
}