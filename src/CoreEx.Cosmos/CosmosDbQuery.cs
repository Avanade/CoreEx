// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Linq;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Encapsulates a <b>CosmosDb/DocumentDb</b> query enabling all select-like capabilities.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public class CosmosDbQuery<T, TModel> : CosmosDbQueryBase<T, TModel> where T : class, new () where TModel : class, IIdentifier<string>, new()
    {
        private readonly Func<IQueryable<TModel>, IQueryable<TModel>>? _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbQuery{T, TModel}"/> class.
        /// </summary>
        /// <param name="container">The <see cref="CosmosDbContainer{T, TModel}"/>.</param>
        /// <param name="query">A function to modify the underlying <see cref="IQueryable{T}"/>.</param>
        public CosmosDbQuery(CosmosDbContainer<T, TModel> container, Func<IQueryable<TModel>, IQueryable<TModel>>? query) : base(container) => _query = query;

        /// <summary>
        /// Gets the <see cref="CosmosDbContainer{T, TModel}"/>.
        /// </summary>
        public new CosmosDbContainer<T, TModel> Container => (CosmosDbContainer<T, TModel>)base.Container;

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/> to suport fluent-style method-chaining.</returns>
        public CosmosDbQuery<T, TModel> WithPaging(PagingArgs paging)
        {
            Paging = paging == null ? null : (paging is PagingResult pr ? pr : new PagingResult(paging));
            return this;
        }

        /// <summary>
        /// Adds <see cref="PagingArgs"/> to the query.
        /// </summary>
        /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
        /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/> to suport fluent-style method-chaining.</returns>
        public CosmosDbQuery<T, TModel> WithPaging(long skip, long? take = null) => WithPaging(PagingArgs.CreateSkipAndTake(skip, take));

        /// <summary>
        /// Actually manage the underlying query construction and lifetime.
        /// </summary>
        private IQueryable<TModel> ExecuteQueryInternal(Action<IQueryable<TModel>>? execute)
        {
            IQueryable<TModel> q = Container.Container.GetItemLinqQueryable<TModel>(allowSynchronousQueryExecution: true, requestOptions: Container.CosmosDb.GetQueryRequestOptions<T, TModel>(QueryArgs));
            q = _query == null ? q : _query(q);

            var filter = Container.CosmosDb.GetAuthorizeFilter<TModel>(Container.Container.Id);
            if (filter != null)
                q = (IQueryable<TModel>)filter(q);

            execute?.Invoke(q);
            return q;
        }

        /// <summary>
        /// Manages the underlying query construction and lifetime.
        /// </summary>
        private TModel ExecuteQuery(Func<IQueryable<TModel>, TModel> execute) => Container.CosmosDb.Invoker.Invoke(Container.CosmosDb, () => execute(AsQueryable(false)));

        /// <summary>
        /// Gets a prepared <see cref="IQueryable{TModel}"/> with any <see cref="CosmosDbValue{TModel}"/> filtering as applicable.
        /// </summary>
        /// <remarks>The <see cref="CosmosDbQueryBase{T, TModel}.Paging"/> is not supported.</remarks>
        public IQueryable<TModel> AsQueryable() => AsQueryable(true);

        /// <summary>
        /// Initiate the IQueryable.
        /// </summary>
        private IQueryable<TModel> AsQueryable(bool checkPaging)
        {
            if (checkPaging && Paging != null)
                throw new NotSupportedException("The Paging must be null for an AsQueryable(); this is a limitation of the Microsoft.Azure.Cosmos SDK in that the paging must be applied last.");

            return ExecuteQueryInternal(null);
        }

        /// <inheritdoc/>
        public override T SelectSingle() => Container.GetValue(ExecuteQuery(q => q.WithPaging(0, 2).AsEnumerable().Single()));

        /// <inheritdoc/>
        public override T? SelectSingleOrDefault() => Container.GetValue(ExecuteQuery(q => q.WithPaging(0, 2).AsEnumerable().SingleOrDefault()));

        /// <inheritdoc/>
        public override T SelectFirst() => Container.GetValue(ExecuteQuery(q => q.WithPaging(0, 1).AsEnumerable().First()));

        /// <inheritdoc/>
        public override T? SelectFirstOrDefault() => Container.GetValue(ExecuteQuery(q => q.WithPaging(0, 1).AsEnumerable().FirstOrDefault()));

        /// <inheritdoc/>
        public override void SelectQuery<TColl>(TColl coll) => ExecuteQuery(query =>
        { 
            foreach (var item in query.WithPaging(Paging).AsEnumerable())
            {
                coll.Add(Container.GetValue(item));
            }

            if (Paging != null && Paging.IsGetCount)
                Paging.TotalCount = query.Count();
        });

        /// <summary>
        /// Manages the underlying query construction and lifetime.
        /// </summary>
        private void ExecuteQuery(Action<IQueryable<TModel>> execute) => Container.CosmosDb.Invoker.Invoke(Container.CosmosDb, () => ExecuteQueryInternal(execute));
    }
}