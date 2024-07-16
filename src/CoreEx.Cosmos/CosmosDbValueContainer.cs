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
    /// Provides <see cref="Container"/> <see cref="CosmosDbValue{TModel}"/> operations for a specified <see cref="CosmosDb"/> container.
    /// </summary>
    /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    /// <remarks>Represents a special-purpose <b>CosmosDb</b> <see cref="Container"/> that houses an underlying <see cref="CosmosDbValue{TModel}.Value"/>, including <see cref="CosmosDbValue{TModel}.Type"/> name, and flexible <see cref="IEntityKey"/>, for persistence.</remarks>
    public class CosmosDbValueContainer<T, TModel> : CosmosDbContainerBase<T, TModel, CosmosDbValueContainer<T, TModel>> where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
    {
        private readonly Lazy<CosmosDbValueModelContainer<TModel>> _modelContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValueContainer{T, TModel}"/> class.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        /// <param name="dbArgs">The optional <see cref="CosmosDbArgs"/>.</param>
        public CosmosDbValueContainer(ICosmosDb cosmosDb, string containerId, CosmosDbArgs? dbArgs = null) : base(cosmosDb, containerId, dbArgs)
            => _modelContainer = new(() => new CosmosDbValueModelContainer<TModel>(CosmosDb, Container.Id, DbArgs));

        /// <summary>
        /// Gets the underlying <see cref="CosmosDbValueModelContainer{TModel}"/>.
        /// </summary>
        public CosmosDbValueModelContainer<TModel> ModelContainer => _modelContainer.Value;

        /// <summary>
        /// Sets the function to determine the <see cref="PartitionKey"/>; used for <see cref="CosmosDbValueModelContainer{TModel}.GetPartitionKey(CosmosDbValue{TModel}, CosmosDbArgs)"/> (only <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="partitionKey">The function to determine the <see cref="PartitionKey"/>.</param>
        /// <returns>The <see cref="CosmosDbValueContainer{T, TModel}"/> instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is used where there is a value and the corresponding <see cref="PartitionKey"/> needs to be dynamically determined.</remarks>
        public CosmosDbValueContainer<T, TModel> UsePartitionKey(Func<CosmosDbValue<TModel>, PartitionKey> partitionKey)
        {
            ModelContainer.UsePartitionKey(partitionKey);
            return this;
        }

        /// <summary>
        /// Gets the <b>value</b> from the response updating any special properties as required.
        /// </summary>
        /// <param name="resp">The response value.</param>
        /// <returns>The entity value.</returns>
        internal T? GetResponseValue(Response<CosmosDbValue<TModel>> resp)
        {
            if (resp?.Resource == null)
                return default;

            return GetValue(resp.Resource);
        }

        /// <summary>
        /// Gets the <b>value</b> formatting/updating any special properties as required.
        /// </summary>
        /// <param>The model value.</param>
        /// <returns>The entity value.</returns>
        [return: NotNullIfNotNull(nameof(model))]
        public T? GetValue(CosmosDbValue<TModel>? model)
        {
            if (model is null)
                return default;

            ((ICosmosDbValue)model).PrepareAfter(DbArgs);
            var val = CosmosDb.Mapper.Map<TModel, T>(model.Value, OperationTypes.Get)!;
            if (val is IETag et)
            {
                if (et.ETag is not null)
                    et.ETag = ETagGenerator.ParseETag(et.ETag);
                else
                    et.ETag = ETagGenerator.ParseETag(model.ETag);
            }
            
            return DbArgs.CleanUpResult ? Cleaner.Clean(val) : val;
        }

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> Query(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) => Query(new CosmosDbArgs(DbArgs), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> Query(PartitionKey? partitionKey = null, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => Query(new CosmosDbArgs(DbArgs, partitionKey), query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueQuery{T, TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueQuery{T, TModel}"/>.</returns>
        public CosmosDbValueQuery<T, TModel> Query(CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => new(this, dbArgs, query);

        /// <inheritdoc/>
        public async override Task<Result<T?>> GetWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default)
        {
            var result = await ModelContainer.GetWithResultAsync(dbArgs, key, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(GetValue);
        }

        /// <inheritdoc/>
        public async override Task<Result<T>> CreateWithResultAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default)
        {
            ChangeLog.PrepareCreated(value.ThrowIfNull(nameof(value)));
            TModel model = CosmosDb.Mapper.Map<T, TModel>(value, OperationTypes.Create)!;
            var cvm = new CosmosDbValue<TModel>(model!);

            var result = await ModelContainer.CreateWithResultAsync(dbArgs, cvm, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(model => GetValue(model)!);
        }

        /// <inheritdoc/>
        public async override Task<Result<T>> UpdateWithResultAsync(CosmosDbArgs dbArgs, T value, CancellationToken cancellationToken = default)
        {
            ChangeLog.PrepareUpdated(value);
            var model = CosmosDb.Mapper.Map<T, TModel>(value.ThrowIfNull(nameof(value)), OperationTypes.Update)!;
            var cvm = new CosmosDbValue<TModel>(model!);

            var result = await ModelContainer.UpdateWithResultInternalAsync(dbArgs, cvm, cvm => CosmosDb.Mapper.Map(value, cvm.Value, OperationTypes.Update), cancellationToken).ConfigureAwait(false);
            return result.ThenAs(model => GetValue(model)!);
        }

        /// <inheritdoc/>
        public override Task<Result> DeleteWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => ModelContainer.DeleteWithResultAsync(dbArgs, key, cancellationToken);
    }
}