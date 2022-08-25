// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Encapsulates a <b>CosmosDb/DocumentDb</b> query enabling all select-like capabilities.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="CosmosDbValue{T}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public class CosmosDbValueQuery<T, TModel> : CosmosDbQueryBase<T, TModel, CosmosDbValueQuery<T, TModel>> where T : class, new() where TModel : class, IIdentifier, new()
    {
        private readonly Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValueQuery{T, TModel}"/> class.
        /// </summary>
        /// <param name="container">The <see cref="CosmosDbValueContainer{T, TModel}"/>.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">A function to modify the underlying <see cref="IQueryable{T}"/>.</param>
        public CosmosDbValueQuery(CosmosDbValueContainer<T, TModel> container, CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) : base(container, dbArgs) => _query = query;

        /// <summary>
        /// Gets the <see cref="CosmosDbValueContainer{T, TModel}"/>.
        /// </summary>
        public new CosmosDbValueContainer<T, TModel> Container => (CosmosDbValueContainer<T, TModel>)base.Container;

        /// <inheritdoc/>
        public override async Task SelectQueryAsync<TColl>(TColl coll, CancellationToken cancellationToken = default)
        {
            IQueryable<CosmosDbValue<TModel>> query = Container.Container.GetItemLinqQueryable<CosmosDbValue<TModel>>(requestOptions: Container.CosmosDb.GetQueryRequestOptions<T, TModel>(QueryArgs));
            query = (_query == null ? query : _query(query)).WhereType(typeof(TModel));

            var filter = Container.CosmosDb.GetAuthorizeFilter<TModel>(Container.Container.Id);
            if (filter != null)
                query = (IQueryable<CosmosDbValue<TModel>>)filter(query);

            await Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, query, coll, async (q, items, ct) =>
            {
                using var iterator = q.WithPaging(Paging).ToFeedIterator();
                while (iterator.HasMoreResults)
                {
                    foreach (var item in await iterator.ReadNextAsync(ct).ConfigureAwait(false))
                    {
                        items.Add(Container.GetValue(item));
                    }
                }
            }, cancellationToken).ConfigureAwait(false);

            if (Paging != null && Paging.IsGetCount)
            {
                IQueryable<CosmosDbValue<TModel>> query2 = Container.Container.GetItemLinqQueryable<CosmosDbValue<TModel>>(requestOptions: Container.CosmosDb.GetQueryRequestOptions<T, TModel>(QueryArgs));
                query2 = _query == null ? query2 : _query(query2).WhereType(typeof(TModel));
                if (filter != null)
                    query2 = (IQueryable<CosmosDbValue<TModel>>)filter(query);

                Paging.TotalCount = (await query2.CountAsync(cancellationToken).ConfigureAwait(false)).Resource;
            }
        }
    }
}