// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides extended <b>CosmosDb</b> data access.
    /// </summary>
    /// <param name="database">The <see cref="Microsoft.Azure.Cosmos.Database"/>.</param>
    /// <param name="mapper">The <see cref="IMapper"/>.</param>
    /// <param name="invoker">Enables the <see cref="Invoker"/> to be overridden; defaults to <see cref="CosmosDbInvoker"/>.</param>
    public class CosmosDb(Database database, IMapper mapper, CosmosDbInvoker? invoker = null) : ICosmosDb
    {
        private static CosmosDbInvoker? _invoker;
        private Action<RequestOptions>? _updateRequestOptionsAction;
        private Action<QueryRequestOptions>? _updateQueryRequestOptionsAction;
        private readonly ConcurrentDictionary<Key, Func<IQueryable, IQueryable>> _filters = new();
        private PartitionKey? _partitionKey;

        /// <summary>
        /// Provides key as combination of model type and container identifier.
        /// </summary>
        private readonly struct Key(Type modelType, string containerId)
        {
            public Type ModelType { get; } = modelType;

            public string ContainerId { get; } = containerId;
        }

        /// <inheritdoc/>
        public Database Database { get; } = database.ThrowIfNull(nameof(database));

        /// <inheritdoc/>
        public IMapper Mapper { get; } = mapper.ThrowIfNull(nameof(mapper));

        /// <inheritdoc/>
        public CosmosDbInvoker Invoker { get; } = invoker ?? (_invoker ??= new CosmosDbInvoker());

        /// <inheritdoc/>
        public virtual PartitionKey? PartitionKey => _partitionKey;

        /// <inheritdoc/>
        public CosmosDbArgs DbArgs { get; set; } = new CosmosDbArgs();

        /// <summary>
        /// Uses (sets) the <see cref="PartitionKey"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
        /// <returns>The <see cref="CosmosDb"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>As the <see cref="PartitionKey"/> property can be overridden by an inheritor this may have no affect.</remarks>
        public CosmosDb UsePartitionKey(PartitionKey? partitionKey)
        {
            _partitionKey = partitionKey;
            return this;
        }

        /// <inheritdoc/>
        public Container GetCosmosContainer(string containerId) => Database.GetContainer(containerId);

        /// <inheritdoc/>
        public CosmosDbContainer<T, TModel> Container<T, TModel>(string containerId) where T : class, IEntityKey, new() where TModel : class, IIdentifier<string>, new() => new(this, containerId);

        /// <inheritdoc/>
        public CosmosDbValueContainer<T, TModel> ValueContainer<T, TModel>(string containerId) where T : class, IEntityKey, new() where TModel : class, IIdentifier, new() => new(this, containerId);

        /// <inheritdoc/>
        public CosmosDbModelQuery<TModel> ModelQuery<TModel>(string containerId, CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query) where TModel : class, IIdentifier<string>, new()
            => new(new CosmosDbModelContainer(this, GetCosmosContainer(containerId)), dbArgs, query);

        /// <inheritdoc/>
        public CosmosDbValueModelQuery<TModel> ValueModelQuery<TModel>(string containerId, CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) where TModel : class, IIdentifier<string>, new()
            => new(new CosmosDbModelContainer(this, GetCosmosContainer(containerId)), dbArgs, query);

        /// <summary>
        /// Sets the filter for all operations performed on the <typeparamref name="TModel"/> for the specified <paramref name="containerId"/> to ensure authorisation is applied. Applies automatically 
        /// to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <typeparam name="TModel">The model <see cref="Type"/> persisted within the container.</typeparam>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        /// <param name="filter">The filter query.</param>
        /// <remarks>The <see cref="CosmosDb"/> instance to support fluent-style method-chaining.</remarks>
        public CosmosDb UseAuthorizeFilter<TModel>(string containerId, Func<IQueryable, IQueryable> filter)
        {
            if (!_filters.TryAdd(new Key(typeof(TModel), containerId.ThrowIfNull(nameof(containerId))), filter.ThrowIfNull(nameof(filter))))
                throw new InvalidOperationException("A filter cannot be overridden.");

            return this;
        }

        /// <inheritdoc/>
        public Func<IQueryable, IQueryable>? GetAuthorizeFilter<TModel>(string containerId) => _filters.TryGetValue(new Key(typeof(TModel), containerId.ThrowIfNull(nameof(containerId))), out var filter) ? filter : null;

        /// <summary>
        /// Sets the <see cref="Action"/> to update the <see cref="ItemRequestOptions"/> for the selected operation.
        /// </summary>
        /// <param name="updateItemRequestOptionsAction">The <see cref="Action"/> to update the <see cref="Microsoft.Azure.Cosmos.ItemRequestOptions"/>.</param>
        /// <returns>This <see cref="CosmosDb"/> instance to support fluent-style method-chaining.</returns>
        public CosmosDb ItemRequestOptions(Action<RequestOptions> updateItemRequestOptionsAction)
        {
            _updateRequestOptionsAction = updateItemRequestOptionsAction.ThrowIfNull(nameof(updateItemRequestOptionsAction));
            return this;
        }

        /// <summary>
        /// Updates the <paramref name="itemRequestOptions"/> using the <see cref="Action"/> set with <see cref="ItemRequestOptions(Action{RequestOptions})"/>.
        /// </summary>
        /// <param name="itemRequestOptions">The <see cref="Microsoft.Azure.Cosmos.ItemRequestOptions"/>.</param>
        public void UpdateItemRequestOptions(RequestOptions itemRequestOptions) => _updateRequestOptionsAction?.Invoke(itemRequestOptions.ThrowIfNull(nameof(itemRequestOptions)));

        /// <inheritdoc/>
        ItemRequestOptions ICosmosDb.GetItemRequestOptions<T, TModel>(CosmosDbArgs dbArgs) where T : class where TModel : class
        {
            var iro = dbArgs.ItemRequestOptions ?? new ItemRequestOptions();
            UpdateItemRequestOptions(iro);
            return iro;
        }

        /// <summary>
        /// Sets the <see cref="Action"/> to update the <see cref="QueryRequestOptions"/> for the selected operation.
        /// </summary>
        /// <param name="updateQueryRequestOptionsAction">The <see cref="Action"/> to update the <see cref="QueryRequestOptions"/>.</param>
        /// <returns>This <see cref="CosmosDb"/> instance to support fluent-style method-chaining.</returns>
        public CosmosDb QueryRequestOptions(Action<QueryRequestOptions> updateQueryRequestOptionsAction)
        {
            _updateQueryRequestOptionsAction = updateQueryRequestOptionsAction.ThrowIfNull(nameof(updateQueryRequestOptionsAction));
            return this;
        }

        /// <summary>
        /// Updates the <paramref name="queryRequestOptions"/> using the <see cref="Action"/> set with <see cref="QueryRequestOptions(Action{QueryRequestOptions})"/>.
        /// </summary>
        /// <param name="queryRequestOptions">The <see cref="Microsoft.Azure.Cosmos.QueryRequestOptions"/>.</param>
        public void UpdateQueryRequestOptions(QueryRequestOptions queryRequestOptions) => _updateQueryRequestOptionsAction?.Invoke(queryRequestOptions.ThrowIfNull(nameof(queryRequestOptions)));

        /// <inheritdoc/>
        QueryRequestOptions ICosmosDb.GetQueryRequestOptions<T, TModel>(CosmosDbArgs dbArgs) where T : class where TModel : class
        {
            var ro = dbArgs.QueryRequestOptions ?? new QueryRequestOptions();
            ro.PartitionKey ??= dbArgs.PartitionKey ?? PartitionKey;

            UpdateQueryRequestOptions(ro);
            return ro;
        }

        /// <inheritdoc/>
        QueryRequestOptions ICosmosDb.GetQueryRequestOptions<TModel>(CosmosDbArgs dbArgs) where TModel : class
        {
            var ro = dbArgs.QueryRequestOptions ?? new QueryRequestOptions();
            ro.PartitionKey ??= dbArgs.PartitionKey ?? PartitionKey;

            UpdateQueryRequestOptions(ro);
            return ro;
        }

        /// <inheritdoc/>
        public Result? HandleCosmosException(CosmosException cex) => OnCosmosException(cex);

        /// <summary>
        /// Provides the <see cref="CosmosException"/> handling as a result of <see cref="HandleCosmosException(CosmosException)"/>.
        /// </summary>
        /// <param name="cex">The <see cref="CosmosException"/>.</param>
        /// <returns>The <see cref="Result"/> containing the appropriate <see cref="IResult.Error"/>.</returns>
        /// <remarks>Where overridding and the <see cref="CosmosException"/> is not specifically handled then invoke the base to ensure any standard handling is executed.</remarks>
        protected virtual Result? OnCosmosException(CosmosException cex) => cex.ThrowIfNull(nameof(cex)).StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => Result.Fail(new NotFoundException(null, cex)),
            System.Net.HttpStatusCode.Conflict => Result.Fail(new DuplicateException(null, cex)),
            System.Net.HttpStatusCode.PreconditionFailed => Result.Fail(new ConcurrencyException(null, cex)),
            _ => Result.Fail(cex)
        };

        /// <inheritdoc/>
        public virtual string FormatIdentifier(object? id) => id.ThrowIfNull(nameof(id)) switch
        {
            string si => si,
            int ii => ii.ToString(System.Globalization.CultureInfo.InvariantCulture),
            long li => li.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Guid gi => gi.ToString(),
            _ => throw new NotSupportedException("An identifier must be one of the following Types: string, int, long, or Guid.")
        };

        /// <inheritdoc/>
        public virtual object? ParseIdentifier(Type type, string? id) => type.ThrowIfNull(nameof(type)) switch
        {
            Type t when t == typeof(string) => id,
            Type t when t == typeof(int) => id == null ? 0 : int.Parse(id, System.Globalization.CultureInfo.InvariantCulture),
            Type t when t == typeof(long) => id == null ? 0 : long.Parse(id, System.Globalization.CultureInfo.InvariantCulture),
            Type t when t == typeof(Guid) => id == null ? Guid.Empty : Guid.Parse(id),
            _ => throw new NotSupportedException("An identifier must be one of the following Types: string, int, long, or Guid.")
        };
    }
}