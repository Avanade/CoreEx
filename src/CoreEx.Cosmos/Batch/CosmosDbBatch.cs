﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Json.Data;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos.Batch
{
    /// <summary>
    /// Provides <b>CosmosDb</b>-related <i>batch</i> extension methods. The implementation is <b>bulk-ready</b> as per <see href="https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/"/>; to properly
    /// enable use <c>new CosmosClientOptions() { AllowBulkExecution = true }</c> as described.
    /// </summary>
    public static class CosmosDbBatch
    {
        /// <summary>
        /// Indicates whether the items in the batch are executed sequentially.
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
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task ImportBatchAsync<TModel>(this ICosmosDb cosmosDb, string containerId, IEnumerable<TModel> items, Func<TModel, TModel>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where TModel : class, IEntityKey
        {
            var container = cosmosDb.ThrowIfNull(nameof(cosmosDb)).Database.GetContainer(containerId.ThrowIfNull(nameof(containerId)));

            if (items == null)
                return;

            dbArgs ??= cosmosDb.DbArgs;
            List<Task> tasks = [];
            foreach (var item in items)
            {
                if (SequentialExecution)
                    await container.CreateItemAsync(modelUpdater?.Invoke(item) ?? item, dbArgs.Value.PartitionKey, dbArgs.Value.ItemRequestOptions, cancellationToken).ConfigureAwait(false);
                else
                    tasks.Add(container.CreateItemAsync(modelUpdater?.Invoke(item) ?? item, dbArgs.Value.PartitionKey, dbArgs.Value.ItemRequestOptions, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Imports (creates) a batch of <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task ImportBatchAsync<T, TModel>(this CosmosDbContainer container, IEnumerable<TModel> items, Func<TModel, TModel>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => ImportBatchAsync(container.ThrowIfNull(nameof(container)).CosmosDb!, container.CosmosContainer.Id!, items, modelUpdater, dbArgs ?? container.DbArgs, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer{T, TModel}"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task ImportBatchAsync<T, TModel>(this CosmosDbContainer<T, TModel> container, IEnumerable<TModel> items, Func<TModel, TModel>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => ImportBatchAsync<T, TModel>(container.ThrowIfNull(nameof(container)).Container, items, modelUpdater, dbArgs, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of named items from the <paramref name="jsonDataReader"/> into the specified <paramref name="containerId"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the <paramref name="jsonDataReader"/>. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportBatchAsync<TModel>(this ICosmosDb cosmosDb, string containerId, JsonDataReader jsonDataReader, string? name = null, Func<TModel, TModel>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            var container = cosmosDb.ThrowIfNull(nameof(cosmosDb)).Database.GetContainer(containerId.ThrowIfNull(nameof(containerId)));
            if (!jsonDataReader.ThrowIfNull(nameof(jsonDataReader)).TryDeserialize<TModel>(name, out var items))
                return false;

            await ImportBatchAsync(cosmosDb, containerId, items, modelUpdater, dbArgs, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Imports (creates) a batch of named items from the <paramref name="jsonDataReader"/> into the specified <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the <paramref name="jsonDataReader"/>. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportBatchAsync<T, TModel>(this CosmosDbContainer container, JsonDataReader jsonDataReader, string? name = null, Func<TModel, TModel>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => ImportBatchAsync(container.ThrowIfNull(nameof(container)).CosmosDb!, container.CosmosContainer.Id!, jsonDataReader, name, modelUpdater, dbArgs ?? container.DbArgs, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of named items from the <paramref name="jsonDataReader"/> into the specified <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer{T, TModel}"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the <paramref name="jsonDataReader"/>. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportBatchAsync<T, TModel>(this CosmosDbContainer<T, TModel> container, JsonDataReader jsonDataReader, string? name = null, Func<TModel, TModel>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => ImportBatchAsync<T, TModel>(container.ThrowIfNull(nameof(container)).Container, jsonDataReader, name, modelUpdater, dbArgs, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task ImportValueBatchAsync<TModel>(this ICosmosDb cosmosDb, string containerId, IEnumerable<TModel> items, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            var container = cosmosDb.ThrowIfNull(nameof(cosmosDb)).Database.GetContainer(containerId.ThrowIfNull(nameof(containerId)));

            if (items == null)
                return;

            dbArgs ??= cosmosDb.DbArgs;
            List<Task> tasks = [];
            foreach (var item in items)
            {
                var cdv = new CosmosDbValue<TModel>(item);
                ((ICosmosDbValue)cdv).PrepareBefore(dbArgs.Value, null);

                if (SequentialExecution)
                    await container.CreateItemAsync(modelUpdater?.Invoke(cdv) ?? cdv, dbArgs.Value.PartitionKey, dbArgs.Value.ItemRequestOptions, cancellationToken).ConfigureAwait(false);
                else
                    tasks.Add(container.CreateItemAsync(modelUpdater?.Invoke(cdv) ?? cdv, dbArgs.Value.PartitionKey, dbArgs.Value.ItemRequestOptions, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task ImportValueBatchAsync<T, TModel>(this CosmosDbContainer container, IEnumerable<TModel> items, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            CosmosDbValue<TModel> func(CosmosDbValue<TModel> cvm)
            {
                cvm.Type = container.Model.GetModelName<TModel>();
                return modelUpdater?.Invoke(cvm) ?? cvm;
            }

            return ImportValueBatchAsync(container.ThrowIfNull(nameof(container)).CosmosDb!, container.CosmosContainer.Id!, items, func, dbArgs ?? container.DbArgs, cancellationToken);
        }

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer{T, TModel}"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task ImportValueBatchAsync<T, TModel>(this CosmosDbValueContainer<T, TModel> container, IEnumerable<TModel> items, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => ImportValueBatchAsync<T, TModel>(container.ThrowIfNull(nameof(container)).Container, items, modelUpdater, dbArgs, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of named <see cref="CosmosDbValue{TModel}"/> items from the <paramref name="jsonDataReader"/> into the specified <paramref name="containerId"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the <paramref name="jsonDataReader"/>. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportValueBatchAsync<TModel>(this ICosmosDb cosmosDb, string containerId, JsonDataReader jsonDataReader, string? name = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where TModel : class, IEntityKey, new()
        {
            var container = cosmosDb.ThrowIfNull(nameof(cosmosDb)).Database.GetContainer(containerId.ThrowIfNull(nameof(containerId)));
            if (!jsonDataReader.ThrowIfNull(nameof(jsonDataReader)).TryDeserialize<TModel>(name, out var items))
                return false;

            await ImportValueBatchAsync(cosmosDb, containerId, items, modelUpdater, dbArgs, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Imports (creates) a batch of named <see cref="CosmosDbValue{TModel}"/> items from the <paramref name="jsonDataReader"/> into the specified <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the <paramref name="jsonDataReader"/>. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportValueBatchAsync<T, TModel>(this CosmosDbContainer container, JsonDataReader jsonDataReader, string? name = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
        {
            CosmosDbValue<TModel> func(CosmosDbValue<TModel> cvm)
            {
                cvm.Type = container.Model.GetModelName<TModel>();
                return modelUpdater?.Invoke(cvm) ?? cvm;
            }

            return ImportValueBatchAsync(container.ThrowIfNull(nameof(container)).CosmosDb!, container.CosmosContainer.Id!, jsonDataReader, name ?? container.Model.GetModelName<TModel>(), (Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>)func, dbArgs ?? container.DbArgs, cancellationToken);
        }

        /// <summary>
        /// Imports (creates) a batch of named <see cref="CosmosDbValue{TModel}"/> items from the <paramref name="jsonDataReader"/> into the specified <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed within the <paramref name="jsonDataReader"/>. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportValueBatchAsync<T, TModel>(this CosmosDbValueContainer<T, TModel> container, JsonDataReader jsonDataReader, string? name = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new()
            => ImportValueBatchAsync<T, TModel>(container.ThrowIfNull(nameof(container)).Container, jsonDataReader, name, modelUpdater, dbArgs, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of named <see cref="CosmosDbValue{TModel}"/> items from the <paramref name="jsonDataReader"/> into the specified <paramref name="containerId"/>.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="types">The <see cref="Type"/> list to find and import.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportValueBatchAsync(this ICosmosDb cosmosDb, string containerId, JsonDataReader jsonDataReader, IEnumerable<Type> types, Func<object, object>? modelUpdater = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default)
        {
            var container = cosmosDb.ThrowIfNull(nameof(cosmosDb)).Database.GetContainer(containerId.ThrowIfNull(nameof(containerId)));
            jsonDataReader.ThrowIfNull(nameof(jsonDataReader));

            dbArgs ??= cosmosDb.DbArgs;
            var tasks = new List<Task>();

            foreach (var type in types)
            {
                if (jsonDataReader.TryDeserialize(type, type.Name, out var items))
                {
                    var t = typeof(CosmosDbValue<>).MakeGenericType(type);

                    foreach (var item in items.Where(x => x is not null))
                    {
                        var cdv = Activator.CreateInstance(t, item)!;
                        ((ICosmosDbValue)cdv).PrepareBefore(dbArgs.Value, null);

                        if (SequentialExecution)
                            await container.CreateItemAsync(modelUpdater?.Invoke(cdv) ?? cdv, dbArgs.Value.PartitionKey, dbArgs.Value.ItemRequestOptions, cancellationToken).ConfigureAwait(false);
                        else
                            tasks.Add(container.CreateItemAsync(modelUpdater?.Invoke(cdv) ?? cdv, dbArgs.Value.PartitionKey, dbArgs.Value.ItemRequestOptions, cancellationToken));
                    }
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return tasks.Count > 0;
        }

        /// <summary>
        /// Imports (creates) a batch of JSON items from the <paramref name="jsonDataReader"/> as-is into the specified <paramref name="container"/>.
        /// </summary>
        /// <param name="container">The <see cref="CosmosDbContainer"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items are housed within the <paramref name="jsonDataReader"/>. Defaults to the underlying <see cref="Container.Id"/>.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportJsonBatchAsync(this CosmosDbContainer container, JsonDataReader jsonDataReader, string? name = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default)
            => ImportJsonBatchAsync(container.CosmosDb, container.CosmosContainer.Id, jsonDataReader, name, dbArgs, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of JSON items from the <paramref name="jsonDataReader"/> as-is into the specified <paramref name="containerId"/>.
        /// </summary>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="containerId">The <see cref="Container.Id"/>.</param>
        /// <param name="jsonDataReader">The <see cref="JsonDataReader"/>.</param>
        /// <param name="name">The element name where the array of items are housed within the <paramref name="jsonDataReader"/>. Defaults to the <paramref name="containerId"/>.</param>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportJsonBatchAsync(this ICosmosDb cosmosDb, string containerId, JsonDataReader jsonDataReader, string? name = null, CosmosDbArgs? dbArgs = null, CancellationToken cancellationToken = default)
        {
            var container = cosmosDb.ThrowIfNull(nameof(cosmosDb)).Database.GetContainer(containerId.ThrowIfNull(nameof(containerId)));
            jsonDataReader.ThrowIfNull(nameof(jsonDataReader));

            dbArgs ??= cosmosDb.DbArgs;
            var pk = dbArgs.Value.PartitionKey ?? PartitionKey.None;

            var tasks = new List<Task>();

            var result = await jsonDataReader.EnumerateJsonAsync(name ?? containerId, async json =>
            { 
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json.ToString()));

                if (SequentialExecution)
                {
                    var resp = await container.CreateItemStreamAsync(ms, pk, dbArgs.Value.ItemRequestOptions, cancellationToken).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();
                }
                else
                    tasks.Add(container.CreateItemAsync(ms, pk, dbArgs.Value.ItemRequestOptions, cancellationToken));
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return result;
        }
    }
}