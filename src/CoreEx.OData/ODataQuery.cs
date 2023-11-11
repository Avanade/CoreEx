// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using System;
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
            ODataClient = odata ?? throw new ArgumentNullException(nameof(odata));
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

        ///// <summary>
        ///// Manages the underlying query construction and lifetime.
        ///// </summary>
        //private async readonly Task<Result<TModel>> ExecuteQueryAsync(Action<Soc.IBoundClient<TModel>> execute)
        //{
        //    ODataClient.Invoker.Invoke(ODataClient, ODataClient, CollectionName, _query, execute, (args, odata, collection, query, execute) =>
        //    {
        //        var q = odata.Client.For<TModel>(collection);
        //        execute((query == null) ? q : query(q));
        //    }, ODataClient);
        //}

        /// <summary>
        /// Manages the underlying query construction and lifetime.
        /// </summary>
        private async readonly Task<Result<TModel>> ExecuteQueryAsync(Func<Soc.IBoundClient<TModel>, CancellationToken, Task<Result<TModel>>> executeAsync, CancellationToken cancellationToken)
            => await ODataClient.Invoker.InvokeAsync(ODataClient, ODataClient, CollectionName, _query, async (args, odata, collection, query, ct) =>
            {
                var q = odata.Client.For<TModel>(collection);
                return await executeAsync((query == null) ? q : query(q), ct).ConfigureAwait(false);
            }, ODataClient, cancellationToken);
 
        /// <summary>
        /// Maps from the model to the value.
        /// </summary>
        private readonly T? MapToValue(TModel? model)
        {
            if (model is null)
                return default;

            var result = Mapper.Map<T>(model, OperationTypes.Get);
            return (result is not null) ? CleanUpResult(result) : throw new InvalidOperationException("Mapping from the OData model must not result in a null value.");
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
             => (await SelectSingleWithResultAsync(cancellationToken).ConfigureAwait(false)).Value!;

        /// <summary>
        /// Selects a single item with a <see cref="Result{T}"/>.
        /// </summary>
        /// <returns>The single item.</returns>
        public async readonly Task<Result<T>> SelectSingleWithResultAsync(CancellationToken cancellationToken = default) => MapToValue(await ExecuteQueryAsync(async (q, ct) =>
        {
            var coll = await q.Skip(0).Top(2).FindEntriesAsync(ct).ConfigureAwait(false);
            return coll.Single();
        }, cancellationToken).ConfigureAwait(false))!;
        
    }
}