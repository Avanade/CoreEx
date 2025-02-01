// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Concurrent;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides extended <b>CosmosDb</b> data access.
    /// </summary>
    /// <param name="database">The <see cref="Microsoft.Azure.Cosmos.Database"/>.</param>
    /// <param name="mapper">The <see cref="IMapper"/>; defaults to <see cref="Mapping.Mapper.Empty"/>.</param>
    /// <param name="invoker">Enables the <see cref="Invoker"/> to be overridden; defaults to <see cref="CosmosDbInvoker"/>.</param>
    /// <remarks>It is recommended that the <see cref="CosmosDb"/> is registered as a scoped service to enable capabilities such as <see cref="CosmosDbArgs.FilterByTenantId"/> that <i>must</i> be scoped. 
    /// Use <see cref="Microsoft.Extensions.DependencyInjection.CosmosDbServiceCollectionExtensions.AddCosmosDb{TCosmosDb}(Microsoft.Extensions.DependencyInjection.IServiceCollection, Func{IServiceProvider, TCosmosDb}, string?)"/> to 
    /// register the scoped <see cref="CosmosDb"/> instance.
    /// <para>The dependent <see cref="CosmosClient"/> should however be registered as a singleton as is <see href="https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/best-practice-dotnet">best practice</see>.</para></remarks>
    public class CosmosDb(Database database, IMapper? mapper = null, CosmosDbInvoker? invoker = null) : ICosmosDb
    {
        private static CosmosDbInvoker? _invoker;
        private readonly ConcurrentDictionary<string, CosmosDbContainer> _containers = new();

        /// <inheritdoc/>
        public Database Database { get; } = database.ThrowIfNull(nameof(database));

        /// <inheritdoc/>
        public IMapper Mapper { get; } = mapper ?? Mapping.Mapper.Empty;

        /// <inheritdoc/>
        public CosmosDbInvoker Invoker { get; } = invoker ?? (_invoker ??= new CosmosDbInvoker());

        /// <inheritdoc/>
        public CosmosDbArgs DbArgs { get; set; } = new CosmosDbArgs();

        /// <inheritdoc/>
        public Container GetCosmosContainer(string containerId) => Database.GetContainer(containerId);

        /// <summary>
        /// Gets the named <see cref="CosmosDbContainer"/> leveraging the <see cref="Container(string)"/> method.
        /// </summary>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        /// <returns>The <see cref="CosmosDbContainer"/>.</returns>
        /// <remarks>Provides indexing to the <see cref="Container(string)"/> method; note that the configuration is expected to have been previously specified where required.</remarks>
        public CosmosDbContainer this[string containerId] => Container(containerId);

        /// <inheritdoc/>
        public CosmosDbContainer Container(string containerId) => _containers.GetOrAdd(containerId.ThrowIfNullOrEmpty(nameof(containerId)), containerId => new CosmosDbContainer(this, containerId));

        /// <inheritdoc/>
        public CosmosDbContainer<T, TModel> Container<T, TModel>(string containerId) where T : class, IEntityKey, new () where TModel : class, IEntityKey, new ()
            => Container(containerId).AsTyped<T, TModel>();

        /// <inheritdoc/>
        public CosmosDbValueContainer<T, TModel> ValueContainer<T, TModel>(string containerId) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => Container(containerId).AsValueTyped<T, TModel>();

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