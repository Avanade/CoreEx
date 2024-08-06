// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Cosmos.Model;
using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides <see cref="Container"/> operations for a <see cref="CosmosDb"/> container.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public class CosmosDbContainer<T, TModel> : CosmosDbContainerBase<T, TModel, CosmosDbContainer<T, TModel>> where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
    {
        private readonly Lazy<CosmosDbModelContainer<TModel>> _modelContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbContainer{T, TModel}"/> class.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        /// <param name="dbArgs">The optional <see cref="CosmosDbArgs"/>.</param>
        public CosmosDbContainer(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null) : base(cosmosDb, containerId, dbArgs)
            => _modelContainer = new(() => new CosmosDbModelContainer<TModel>(CosmosDb, Container.Id, DbArgs));

        /// <summary>
        /// Gets the underlying <see cref="CosmosDbModelContainer{TModel}"/>.
        /// </summary>
        public CosmosDbModelContainer<TModel> ModelContainer => _modelContainer.Value;

        /// <summary>
        /// Sets the function to determine the <see cref="PartitionKey"/>; used for <see cref="CosmosDbModelContainer{TModel}.GetPartitionKey(TModel, CosmosDbArgs)"/> (only <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="partitionKey">The function to determine the <see cref="PartitionKey"/>.</param>
        /// <returns>The <see cref="CosmosDbContainer{T, TModel}"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is used where there is a value and the corresponding <see cref="PartitionKey"/> needs to be dynamically determined.</remarks>
        public CosmosDbContainer<T, TModel> UsePartitionKey(Func<TModel, PartitionKey> partitionKey)
        {
            ModelContainer.UsePartitionKey(partitionKey);
            return this;
        }

        /// <inheritdoc/>
        protected override T? MapToValue(object? model) => MapToValue((TModel?)model!);

        /// <summary>
        /// Maps <paramref name="model"/> to the entity <b>value</b> formatting/updating any special properties as required.
        /// </summary>
        /// <param>The model value.</param>
        /// <returns>The entity value.</returns>
        [return: NotNullIfNotNull(nameof(model))]
        public T? MapToValue(TModel? model)
        {
            var val = CosmosDb.Mapper.Map<TModel, T>(model, OperationTypes.Get)!;
            if (DbArgs.AutoMapETag && val is IETag et && et.ETag != null)
                et.ETag = ETagGenerator.ParseETag(et.ETag);

            return DbArgs.CleanUpResult ? Cleaner.Clean(val) : val;
        }

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(Func<IQueryable<TModel>, IQueryable<TModel>>? query) => Query(new CosmosDbArgs(DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(PartitionKey? partitionKey = null, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => Query(new CosmosDbArgs(DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbQuery{T, TModel}"/>.</returns>
        public CosmosDbQuery<T, TModel> Query(CosmosDbArgs dbArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => new(this, dbArgs, query);

        /// <inheritdoc/>
        public async override Task<Result<T?>> GetWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default)
        {
            var result = await ModelContainer.GetWithResultAsync(dbArgs, key, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(MapToValue);
        }

        /// <inheritdoc/>
        public override async Task<Result<T>> CreateWithResultAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default)
        {
            ChangeLog.PrepareCreated(value.ThrowIfNull(nameof(value)));
            TModel model = CosmosDb.Mapper.Map<T, TModel>(value, OperationTypes.Create)!;

            var result = await ModelContainer.CreateWithResultAsync(dbArgs, model, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(model => MapToValue(model)!);
        }

        /// <inheritdoc/>
        public override async Task<Result<T>> UpdateWithResultAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default)
        {
            ChangeLog.PrepareUpdated(value);
            var model = CosmosDb.Mapper.Map<T, TModel>(value.ThrowIfNull(nameof(value)), OperationTypes.Update)!;
            var result = await ModelContainer.UpdateWithResultInternalAsync(dbArgs, model, m => CosmosDb.Mapper.Map(value, m, OperationTypes.Update), cancellationToken).ConfigureAwait(false);
            return result.ThenAs(model => MapToValue(model)!);
        }

        /// <inheritdoc/>
        public override Task<Result> DeleteWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => ModelContainer.DeleteWithResultAsync(dbArgs, key, cancellationToken);
    }
}