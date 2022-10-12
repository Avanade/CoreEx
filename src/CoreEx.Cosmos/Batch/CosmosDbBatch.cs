// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Json.Data;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos.Batch
{
    /// <summary>
    /// Provides <b>CosmosDb/DocumentDb</b>-related <i>batch</i> extension methods. The implementation is <b>bulk-ready</b> as per <see href="https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/"/>; to properly
    /// enable use <c>new CosmosClientOptions() { AllowBulkExecution = true }</c> as described.
    /// </summary>
    public static class CosmosDbBatch
    {
        /// <summary>
        /// Inidicates whether the items in the batch are executed sequentially.
        /// </summary>
        /// <remarks><c>true</c> results in sequenital (order-based and slower) execution; otherwise, <c>false</c> results in parallel (no order guarantees and faster) execution. Also, see 
        /// <see href="https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/"/> to further improve throughput.</remarks>
        public static bool SequentialExecution { get; set; }

        /// <summary>
        /// Imports (creates) a batch of <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task ImportBatchAsync<TModel>(this ICosmosDb cosmosDb, string containerId, IEnumerable<TModel> items, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier<string>
        {
            var container = (cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb))).Database.GetContainer(containerId ?? throw new ArgumentNullException(nameof(containerId)));

            if (items == null)
                return;

            List<Task> tasks = new();
            foreach (var item in items)
            {
                if (SequentialExecution)
                    await container.CreateItemAsync(modelUpdater?.Invoke(item) ?? item, null, requestOptions, cancellationToken).ConfigureAwait(false);
                else
                    tasks.Add(container.CreateItemAsync(modelUpdater?.Invoke(item) ?? item, null, requestOptions, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Imports (creates) a batch of <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer{T, TModel}"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task ImportBatchAsync<T, TModel>(this CosmosDbContainer<T, TModel> container, IEnumerable<TModel> items, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IIdentifier<string>, new()
            => ImportBatchAsync(container?.CosmosDb!, container?.Container.Id!, items, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of named items from the <paramref name="jsonDataReader"/> into the specified <paramref name="containerId"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the embedded resource. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportBatchAsync<TModel>(this ICosmosDb cosmosDb, string containerId, JsonDataReader jsonDataReader, string? name = null, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier<string>, new()
        {
            var container = (cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb))).Database.GetContainer(containerId ?? throw new ArgumentNullException(nameof(containerId)));
            if (!(jsonDataReader ?? throw new ArgumentNullException(nameof(jsonDataReader))).TryDeserialize<TModel>(name, out var items))
                return false;

            await ImportBatchAsync(cosmosDb, containerId, items, modelUpdater, requestOptions, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Imports (creates) a batch of named items from the <paramref name="jsonDataReader"/> into the specified <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer{T, TModel}"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the embedded resource. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportBatchAsync<T, TModel>(this CosmosDbContainer<T, TModel> container, JsonDataReader jsonDataReader, string? name = null, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IIdentifier<string>, new()
            => ImportBatchAsync(container?.CosmosDb!, container?.Container.Id!, jsonDataReader, name, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task ImportValueBatchAsync<TModel>(this ICosmosDb cosmosDb, string containerId, IEnumerable<TModel> items, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier, new()
        {
            var container = (cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb))).Database.GetContainer(containerId ?? throw new ArgumentNullException(nameof(containerId)));

            if (items == null)
                return;

            List<Task> tasks = new();
            foreach (var item in items)
            {
                var cdv = new CosmosDbValue<TModel>(item);
                ((ICosmosDbValue)cdv).PrepareBefore(cosmosDb);

                if (SequentialExecution)
                    await container.CreateItemAsync(modelUpdater?.Invoke(cdv) ?? cdv, null, requestOptions, cancellationToken).ConfigureAwait(false);
                else
                    tasks.Add(container.CreateItemAsync(modelUpdater?.Invoke(cdv) ?? cdv, null, requestOptions, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbValueContainer{T, TModel}"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task ImportValueBatchAsync<T, TModel>(this CosmosDbValueContainer<T, TModel> container, IEnumerable<TModel> items, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IIdentifier, new()
            => ImportValueBatchAsync(container?.CosmosDb!, container?.Container.Id!, items, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of named <see cref="CosmosDbValue{TModel}"/> items from the <paramref name="jsonDataReader"/> into the specified <paramref name="containerId"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the embedded resource. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportValueBatchAsync<TModel>(this ICosmosDb cosmosDb, string containerId, JsonDataReader jsonDataReader, string? name = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier, new()
        {
            var container = (cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb))).Database.GetContainer(containerId ?? throw new ArgumentNullException(nameof(containerId)));
            if (!(jsonDataReader ?? throw new ArgumentNullException(nameof(jsonDataReader))).TryDeserialize<TModel>(name, out var items))
                return false;

            await ImportValueBatchAsync(cosmosDb, containerId, items, modelUpdater, requestOptions, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Imports (creates) a batch of named <see cref="CosmosDbValue{TModel}"/> items from the <paramref name="jsonDataReader"/> into the specified <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbValueContainer{T, TModel}"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the embedded resource. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportValueBatchAsync<T, TModel>(this CosmosDbValueContainer<T, TModel> container, JsonDataReader jsonDataReader, string? name = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IIdentifier, new()
            => ImportValueBatchAsync(container?.CosmosDb!, container?.Container.Id!, jsonDataReader, name, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of named <see cref="CosmosDbValue{TModel}"/> items from the <paramref name="jsonDataReader"/> into the specified <paramref name="containerId"/>.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="types">The <see cref="Type"/> list to find and import.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportValueBatchAsync(this ICosmosDb cosmosDb, string containerId, JsonDataReader jsonDataReader, IEnumerable<Type> types, Func<object, object>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            var container = (cosmosDb ?? throw new ArgumentNullException(nameof(cosmosDb))).Database.GetContainer(containerId ?? throw new ArgumentNullException(nameof(containerId)));
            var tasks = new List<Task>();

            if (jsonDataReader == null)
                throw new ArgumentNullException(nameof(jsonDataReader));

            foreach (var type in types)
            {
                if (jsonDataReader.TryDeserialize(type, type.Name, out var items))
                {
                    var t = typeof(CosmosDbValue<>).MakeGenericType(type);

                    foreach (var item in items.Where(x => x is not null))
                    {
                        var cdv = Activator.CreateInstance(t, item);
                        ((ICosmosDbValue)cdv).PrepareBefore(cosmosDb);

                        if (SequentialExecution)
                            await container.CreateItemAsync(modelUpdater?.Invoke(cdv) ?? cdv, null, requestOptions, cancellationToken).ConfigureAwait(false);
                        else
                            tasks.Add(container.CreateItemAsync(modelUpdater?.Invoke(cdv) ?? cdv, null, requestOptions, cancellationToken));
                    }
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return tasks.Count > 0;
        }
    }
}