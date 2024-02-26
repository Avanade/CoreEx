// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Soc = Simple.OData.Client;

namespace CoreEx.OData
{
    /// <summary>
    /// Encapsulates an <b>OData</b> query enabling select-like capabilities.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The OData model <see cref="Type"/>.</typeparam>
    public struct ODataQuery<T, TModel> where T : class, new() where TModel : class, new()
    {
        private readonly Func<Soc.IBoundClient<TModel>, Soc.IBoundClient<TModel>>? _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataQuery{T, TModel}"/> struct.
        /// </summary>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="args">The <see cref="ODataArgs"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="query">A function to modify the underlying <see cref="Soc.IBoundClient{TModel}"/> query.</param>
        /// <exception cref="ArgumentNullException"></exception>
        internal ODataQuery(IOData odata, ODataArgs args, string? collectionName, Func<Soc.IBoundClient<TModel>, Soc.IBoundClient<TModel>>? query)
        {
            ODataClient = odata.ThrowIfNull(nameof(odata));
            Args = args;
            CollectionName = collectionName;
            _query = query;
        }

        /// <summary>
        /// Gets the <see cref="IOData"/>.
        /// </summary>
        public IOData ODataClient { get; }

        /// <summary>
        /// Gets the <see cref="ODataArgs"/>.
        /// </summary>
        public ODataArgs Args { get; }

        /// <summary>
        /// Gets the optional collection name override.
        /// </summary>
        public string? CollectionName { get; }

        /// <summary>
        /// Gets the <see cref="IMapper"/>.
        /// </summary>
        public readonly IMapper Mapper => ODataClient.Mapper;

        /// <summary>
        /// Gets the <see cref="PagingResult"/>.
        /// </summary>
        public PagingResult? Paging { get; private set; }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="ODataQuery{T, TModel}"/> to suport fluent-style method-chaining.</returns>
        public ODataQuery<T, TModel> WithPaging(PagingArgs? paging)
        {
            Paging = paging == null ? null : (paging is PagingResult pr ? pr : new PagingResult(paging));
            return this;
        }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
        /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
        /// <returns>The <see cref="ODataQuery{T, TModel}"/> to suport fluent-style method-chaining.</returns>
        public ODataQuery<T, TModel> WithPaging(long skip, long? take = null) => WithPaging(PagingArgs.CreateSkipAndTake(skip, take));

        /// <summary>
        /// Manages the underlying query construction and lifetime.
        /// </summary>
        private async readonly Task<Result<TResult?>> ExecuteQueryAsync<TResult>(Func<Soc.IBoundClient<TModel>, CancellationToken, Task<TResult?>> executeAsync, string memeberName, CancellationToken cancellationToken)
            => await ODataClient.Invoker.InvokeAsync(ODataClient, ODataClient, CollectionName, _query, async (args, odata, name, query, ct) =>
            {
                var q = odata.Client.For<TModel>(name);
                return await executeAsync((query == null) ? q : query(q), ct).ConfigureAwait(false);
            }, ODataClient, cancellationToken, memeberName);

        /// <summary>
        /// Executes the query and maps.
        /// </summary>
        private async readonly Task<Result<T?>> ExecuteQueryAndMapAsync<TResult>(Func<Soc.IBoundClient<TModel>, CancellationToken, Task<TResult?>> executeAsync, string memeberName, CancellationToken cancellationToken)
        {
            var result = await ExecuteQueryAsync(executeAsync, memeberName, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
                return result.AsResult();

            var val = result.Value == null ? default! : Mapper.Map<T>(result.Value, OperationTypes.Get);
            return Args.CleanUpResult ? Cleaner.Clean(val) : val;
        }

        /// <summary>
        /// Cleans up the result where specified within the args.
        /// </summary>
        private readonly T CleanUpResult(T value) => Args.CleanUpResult ? Cleaner.Clean(value) : value;

        /// <summary>
        /// Selects a single item.
        /// </summary>
        /// <returns>The single item.</returns>
        public async readonly Task<T> SelectSingleAsync(CancellationToken cancellationToken = default)
             => (await ExecuteQueryAndMapAsync(async (q, ct) => (await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false)).Single(), nameof(SelectSingleAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects a single item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <returns>The single item.</returns>
        public async readonly Task<Result<T>> SelectSingleWithResultAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryAndMapAsync(async (q, ct) => (await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false)).Single(), nameof(SelectSingleWithResultAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects a single item or default.
        /// </summary>
        /// <returns>The single item or default.</returns>
        public async readonly Task<T?> SelectSingleOrDefaultAsync(CancellationToken cancellationToken = default)
             => (await ExecuteQueryAndMapAsync(async (q, ct) => (await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false)).SingleOrDefault()!, nameof(SelectSingleOrDefaultAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects a single item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <returns>The single item or default.</returns>
        public async readonly Task<Result<T?>> SelectSingleOrDefaultWithResultAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryAndMapAsync(async (q, ct) => (await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false)).SingleOrDefault()!, nameof(SelectSingleOrDefaultWithResultAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects first item.
        /// </summary>
        /// <returns>The first item.</returns>
        public async readonly Task<T> SelectFirstAsync(CancellationToken cancellationToken = default)
             => (await ExecuteQueryAndMapAsync(async (q, ct) => (await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false)).First(), nameof(SelectFirstAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects first item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <returns>The first item.</returns>
        public async readonly Task<Result<T>> SelectFirstWithResultAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryAndMapAsync(async (q, ct) => (await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false)).First(), nameof(SelectFirstWithResultAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects first item or default.
        /// </summary>
        /// <returns>The first item or default.</returns>
        public async readonly Task<T?> SelectFirstOrDefaultAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryAndMapAsync(async (q, ct) => (await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false)).FirstOrDefault()!, nameof(SelectFirstOrDefaultAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Selects first item or default with a <see cref="Result{T}"/>.
        /// </summary>
        /// <returns>The first item or default.</returns>
        public async readonly Task<Result<T?>> SelectFirstOrDefaultWithResultAsync(CancellationToken cancellationToken = default)
            => (await ExecuteQueryAndMapAsync(async (q, ct) => (await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false)).FirstOrDefault()!, nameof(SelectFirstOrDefaultWithResultAsync), cancellationToken).ConfigureAwait(false))!;

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async readonly Task<TCollResult> SelectResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() => new TCollResult
        {
            Paging = Paging,
            Items = (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectResultAsync), cancellationToken).ConfigureAwait(false)).Value
        };

        /// <summary>
        /// Executes the query command creating a <typeparamref name="TCollResult"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/>.</returns>
        public async readonly Task<Result<TCollResult>> SelectResultWithResultAsync<TCollResult, TColl>(CancellationToken cancellationToken = default) where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() => new TCollResult
        {
            Paging = Paging,
            Items = (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectResultWithResultAsync), cancellationToken).ConfigureAwait(false)).Value
        };

        /// <summary>
        /// Executes the query command creating a resultant collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        public async readonly Task<TColl> SelectQueryAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
            => (await SelectQueryWithResultInternalAsync<TColl>(nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Executes the query command creating a resultant collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <returns>A resultant collection.</returns>
        public readonly Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(CancellationToken cancellationToken = default) where TColl : ICollection<T>, new()
            => SelectQueryWithResultInternalAsync<TColl>(nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes a query adding to the passed collection.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="collection">The collection to add items to.</param>
        /// <returns>The <paramref name="collection"/>.</returns>
        public async readonly Task<TColl> SelectQueryAsync<TColl>(TColl collection, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => (await SelectQueryWithResultInternalAsync(collection, nameof(SelectQueryAsync), cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Executes a query adding to the passed collection with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="collection">The collection to add items to.</param>
        /// <returns>The <paramref name="collection"/>.</returns>
        public readonly Task<Result<TColl>> SelectQueryWithResultAsync<TColl>(TColl collection, CancellationToken cancellationToken = default) where TColl : ICollection<T>
            => SelectQueryWithResultInternalAsync(collection, nameof(SelectQueryWithResultAsync), cancellationToken);

        /// <summary>
        /// Executes the query command creating a resultant collection with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async readonly Task<Result<TColl>> SelectQueryWithResultInternalAsync<TColl>(string memberName, CancellationToken cancellationToken) where TColl : ICollection<T>, new()
        {
            var coll = new TColl();
            return await SelectQueryWithResultInternalAsync(coll, memberName, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a query adding to the passed collection with a <see cref="Result{T}"/> internal.
        /// </summary>
        private async readonly Task<Result<TColl>> SelectQueryWithResultInternalAsync<TColl>(TColl collection, string memberName, CancellationToken cancellationToken) where TColl : ICollection<T>
        {
            collection.ThrowIfNull(nameof(collection));

            var paging = Paging;
            var mapper = Mapper;
            var args = Args;

            return await ExecuteQueryAsync(async (q, ct) =>
            {
                Soc.ODataFeedAnnotations ann = null!;

                if (paging is not null)
                {
                    if (paging.Option == PagingOption.TokenAndTake)
                        throw new InvalidOperationException("PagingOption.TokenAndTake is not supported for OData.");

                    q = q.Skip(paging.Skip!.Value).Top(paging.Take);
                    if (paging.IsGetCount && args.IsPagingGetCountSupported)
                        ann = new Soc.ODataFeedAnnotations();
                }

                foreach (var item in await (ann is null ? q.FindEntriesAsync(ct) : q.FindEntriesAsync(ann, ct)).ConfigureAwait(false))
                {
                    var val = mapper.Map<TModel, T>(item, OperationTypes.Get) ?? throw new InvalidOperationException("Mapping from the ODATA model must not result in a null value.");
                    collection.Add(args.CleanUpResult ? Cleaner.Clean(val) : val);
                }

                if (ann != null)
                    paging!.TotalCount = ann.Count;

                return Result<TColl>.Ok(collection);
            }, memberName, cancellationToken).ConfigureAwait(false);
        }
    }
}