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
    /// Encapsulates a <b>CosmosDb</b> query enabling all select-like capabilities.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <param name="container">The <see cref="CosmosDbContainer{T, TModel}"/>.</param>
    /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
    /// <param name="query">A function to modify the underlying <see cref="IQueryable{T}"/>.</param>
    public class CosmosDbQuery<T, TModel>(CosmosDbContainer<T, TModel> container, CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query) : CosmosDbQueryBase<T, TModel, CosmosDbQuery<T, TModel>>(container, dbArgs) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
    {
        private readonly Func<IQueryable<TModel>, IQueryable<TModel>>? _query = query;

        /// <summary>
        /// Gets the <see cref="CosmosDbContainer{T, TModel}"/>.
        /// </summary>
        public new CosmosDbContainer<T, TModel> Container => (CosmosDbContainer<T, TModel>)base.Container;

        /// <summary>
        /// Instantiates the <see cref="IQueryable"/>.
        /// </summary>
        private IQueryable<TModel> AsQueryable(bool allowSynchronousQueryExecution, bool pagingSupported)
        {
            if (!pagingSupported && Paging is not null)
                throw new NotSupportedException("Paging is not supported when accessing AsQueryable directly; paging must be applied directly to the resulting IQueryable instance.");

            IQueryable<TModel> query = Container.Container.GetItemLinqQueryable<TModel>(allowSynchronousQueryExecution: allowSynchronousQueryExecution, requestOptions: QueryArgs.GetQueryRequestOptions());
            query = _query == null ? query : _query(query);

            var filter = Container.CosmosDb.GetAuthorizeFilter<TModel>(Container.Container.Id);
            if (filter != null)
                query = (IQueryable<TModel>)filter(query);

            if (QueryArgs.FilterByTenantId && typeof(ITenantId).IsAssignableFrom(typeof(TModel)))
                query = query.Where(x => ((ITenantId)x).TenantId == QueryArgs.GetTenantId());

            if (typeof(ILogicallyDeleted).IsAssignableFrom(typeof(TModel)))
                query = query.Where(x => !((ILogicallyDeleted)x).IsDeleted.IsDefined() || ((ILogicallyDeleted)x).IsDeleted == null || ((ILogicallyDeleted)x).IsDeleted == false);

            return query;
        }

        /// <inheritdoc/>
        public override Task<Result> SelectQueryWithResultAsync<TColl>(TColl coll, CancellationToken cancellationToken = default) => Container.CosmosDb.Invoker.InvokeAsync(Container.CosmosDb, coll, async (_, items, ct) =>
        {
            var q = AsQueryable(false, true);

            using var iterator = q.WithPaging(Paging).ToFeedIterator();
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync(ct).ConfigureAwait(false))
                {
                    if (item is not null)
                        items.Add(Container.MapToValue(item));
                }
            }

            if (Paging != null && Paging.IsGetCount)
                Paging.TotalCount = (await q.CountAsync(cancellationToken).ConfigureAwait(false)).Resource;

            return Result.Success;
        }, cancellationToken, nameof(SelectQueryWithResultAsync));
    }
}