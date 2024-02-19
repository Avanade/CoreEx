// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Encapsulates a <b>CosmosDb</b> model-only query enabling all select-like capabilities.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
    /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="query">A function to modify the underlying <see cref="IQueryable{T}"/>.</param>
    public class CosmosDbModelQuery<TModel>(ICosmosDbContainer container, CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query) : CosmosDbModelQueryBase<TModel, CosmosDbModelQuery<TModel>>(container, dbArgs) where TModel : class, IIdentifier<string>, new()
    {
        private readonly Func<IQueryable<TModel>, IQueryable<TModel>>? _query = query;

        /// <summary>
        /// Instantiates the <see cref="IQueryable"/>.
        /// </summary>
        private IQueryable<TModel> AsQueryable(bool allowSynchronousQueryExecution, bool pagingSupported)
        {
            if (!pagingSupported && Paging is not null)
                throw new NotSupportedException("Paging is not supported when accessing AsQueryable directly; paging must be applied directly to the resulting IQueryable instance.");

            IQueryable<TModel> query = Container.Container.GetItemLinqQueryable<TModel>(allowSynchronousQueryExecution: allowSynchronousQueryExecution, requestOptions: Container.CosmosDb.GetQueryRequestOptions<TModel>(QueryArgs));
            query = _query == null ? query : _query(query);

            var filter = Container.CosmosDb.GetAuthorizeFilter<TModel>(Container.Container.Id);
            if (filter != null)
                query = (IQueryable<TModel>)filter(query);

            return query;
        }

        /// <summary>
        /// Gets a pre-prepared <see cref="IQueryable"/> with filtering applied as applicable.
        /// </summary>
        /// <returns>The <see cref="IQueryable"/>.</returns>
        /// <remarks>The <see cref="CosmosDbModelQueryBase{TModel, TSelf}.Paging"/> is not supported. The query will <i>not</i> be automatically included within an <see cref="CosmosDb.Invoker"/> execution.</remarks>
        public IQueryable<TModel> AsQueryable() => AsQueryable(true, false);

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