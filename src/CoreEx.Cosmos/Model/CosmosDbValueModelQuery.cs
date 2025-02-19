﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Encapsulates a <b>CosmosDb</b> model-only query enabling all select-like capabilities.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
    /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="query">A function to modify the underlying <see cref="IQueryable{T}"/>.</param>
    public class CosmosDbValueModelQuery<TModel>(CosmosDbContainer container, CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) : CosmosDbModelQueryBase<CosmosDbValue<TModel>, CosmosDbValueModelQuery<TModel>>(container, dbArgs) where TModel : class, IEntityKey, new()
    {
        private readonly Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? _query = query;

        /// <summary>
        /// Instantiates the <see cref="IQueryable"/>.
        /// </summary>
        private IQueryable<CosmosDbValue<TModel>> AsQueryable(bool allowSynchronousQueryExecution, bool pagingSupported)
        {
            if (!pagingSupported && Paging is not null)
                throw new NotSupportedException("Paging is not supported when accessing AsQueryable directly; paging must be applied directly to the resulting IQueryable instance.");

            IQueryable<CosmosDbValue<TModel>> query = Container.CosmosContainer.GetItemLinqQueryable<CosmosDbValue<TModel>>(allowSynchronousQueryExecution: allowSynchronousQueryExecution, requestOptions: QueryArgs.GetQueryRequestOptions());
            query = (_query == null ? query : _query(query)).WhereType(Container.Model.GetModelName<TModel>());

            var filter = Container.Model.GetValueAuthorizeFilter<TModel>();
            if (filter != null)
                query = filter(query);

            return QueryArgs.WhereModelValid(query);
        }

        /// <summary>
        /// Gets a pre-prepared <see cref="IQueryable"/> with filtering applied as applicable.
        /// </summary>
        /// <returns>The <see cref="IQueryable"/>.</returns>
        /// <remarks>The <see cref="CosmosDbModelQueryBase{TModel, TSelf}.Paging"/> is not supported. The query will <i>not</i> be automatically included within an <see cref="CosmosDb.Invoker"/> execution.</remarks>
        public IQueryable<CosmosDbValue<TModel>> AsQueryable() => AsQueryable(true, false);

        /// <inheritdoc/>
        public override Task<Result> SelectQueryWithResultAsync<TColl>(TColl coll, CancellationToken cancellationToken = default) => Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, coll, async (_, items, ct) =>
        {
            var q = AsQueryable(false, true);

            using var iterator = q.WithPaging(Paging).ToFeedIterator();
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync(ct).ConfigureAwait(false))
                {
                    items.Add(item);
                }
            }

            if (Paging != null && Paging.IsGetCount)
                Paging.TotalCount = (await q.CountAsync(cancellationToken).ConfigureAwait(false)).Resource;

            return Result.Success;
        }, cancellationToken, nameof(SelectQueryWithResultAsync));
    }
}