// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Cosmos.Model;
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
        private readonly ConcurrentDictionary<Key, Func<IQueryable, IQueryable>> _filters = new();

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
        public CosmosDbArgs DbArgs { get; set; } = new CosmosDbArgs();

        /// <inheritdoc/>
        public Container GetCosmosContainer(string containerId) => Database.GetContainer(containerId);

        /// <inheritdoc/>
        public CosmosDbContainer<T, TModel> Container<T, TModel>(string containerId, CosmosDbArgs? dbArgs = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() => new(this, containerId, dbArgs);

        /// <inheritdoc/>
        public CosmosDbValueContainer<T, TModel> ValueContainer<T, TModel>(string containerId, CosmosDbArgs? dbArgs = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() => new(this, containerId, dbArgs);

        /// <inheritdoc/>
        public CosmosDbModelContainer<TModel> ModelContainer<TModel>(string containerId, CosmosDbArgs? dbArgs = null) where TModel : class, IEntityKey, new() => new(this, containerId, dbArgs);

        /// <inheritdoc/>
        public CosmosDbValueModelContainer<TModel> ValueModelContainer<TModel>(string containerId, CosmosDbArgs? dbArgs = null) where TModel : class, IEntityKey, new() => new(this, containerId, dbArgs);

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
    }
}