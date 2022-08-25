// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Json.Data;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Reflection;
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
        /// <param name="container">The <see cref="Container"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task ImportBatchAsync<TModel>(this ICosmosDb cosmosDb, Container container, IEnumerable<TModel> items, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier<string>
        {
            if (cosmosDb == null)
                throw new ArgumentNullException(nameof(cosmosDb));
            
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (cosmosDb.Database != container.Database)
                throw new ArgumentException("There is a Cosmos Database mismatch to the specified Container", nameof(cosmosDb));

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
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task ImportBatchAsync<TModel>(this ICosmosDbContainer container, IEnumerable<TModel> items, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier<string>
            => ImportBatchAsync(container?.CosmosDb!, container?.Container!, items, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of items specified within a <b>YAML</b> resource (see <see cref="JsonDataReader.ParseYaml(System.IO.Stream, JsonDataReaderArgs?)"/>) into the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="container">The <see cref="Container"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="dataReaderArgs">The corresponding <see cref="JsonDataReaderArgs"/>.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportYamlBatchAsync<TResource, TModel>(this ICosmosDb cosmosDb, Container container, string resourceName, string? name = null, JsonDataReaderArgs? dataReaderArgs = null, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier<string>, new()
        {
            if (cosmosDb == null)
                throw new ArgumentNullException(nameof(cosmosDb));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (cosmosDb.Database != container.Database)
                throw new ArgumentException("There is a Cosmos Database mismatch to the specified Container", nameof(cosmosDb));

            using var s = Abstractions.Resource.GetStream<TResource>(resourceName ?? throw new ArgumentNullException(nameof(resourceName)));
            var jdr = JsonDataReader.ParseYaml(s, dataReaderArgs);
            if (!jdr.TryDeserialize<TModel>(name, out var items))
                return false;

            await ImportBatchAsync(cosmosDb, container, items, modelUpdater, requestOptions, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Imports (creates) a batch of items specified within a <b>YAML</b> resource (see <see cref="JsonDataReader.ParseYaml(System.IO.Stream, JsonDataReaderArgs?)"/>) into the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="dataReaderArgs">The corresponding <see cref="JsonDataReaderArgs"/>.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportYamlBatchAsync<TResource, TModel>(this ICosmosDbContainer container, string resourceName, string? name = null, JsonDataReaderArgs? dataReaderArgs = null, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier<string>, new()
            => ImportYamlBatchAsync<TResource, TModel>(container?.CosmosDb!, container?.Container!, resourceName, name, dataReaderArgs, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of items specified within a <b>JSON</b> resource (see <see cref="JsonDataReader.ParseJson(System.IO.Stream, JsonDataReaderArgs?)"/>) into the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="container">The <see cref="Container"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="dataReaderArgs">The corresponding <see cref="JsonDataReaderArgs"/>.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportJsonBatchAsync<TResource, TModel>(this ICosmosDb cosmosDb, Container container, string resourceName, string? name = null, JsonDataReaderArgs? dataReaderArgs = null, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier<string>, new()
        {
            if (cosmosDb == null)
                throw new ArgumentNullException(nameof(cosmosDb));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (cosmosDb.Database != container.Database)
                throw new ArgumentException("There is a Cosmos Database mismatch to the specified Container", nameof(cosmosDb));

            using var s = Abstractions.Resource.GetStream<TResource>(resourceName ?? throw new ArgumentNullException(nameof(resourceName)));
            var jdr = JsonDataReader.ParseJson(s, dataReaderArgs);
            if (!jdr.TryDeserialize<TModel>(name, out var items))
                return false;

            await ImportBatchAsync(cosmosDb, container, items, modelUpdater, requestOptions, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Imports (creates) a batch of items specified within a <b>JSON</b> resource (see <see cref="JsonDataReader.ParseJson(System.IO.Stream, JsonDataReaderArgs?)"/>) into the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="dataReaderArgs">The corresponding <see cref="JsonDataReaderArgs"/>.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportJsonBatchAsync<TResource, TModel>(this ICosmosDbContainer container, string resourceName, string? name = null, JsonDataReaderArgs? dataReaderArgs = null, Func<TModel, TModel>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier<string>, new()
            => ImportJsonBatchAsync<TResource, TModel>(container?.CosmosDb!, container?.Container!, resourceName, name, dataReaderArgs, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="container">The <see cref="Container"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task ImportValueBatchAsync<TModel>(this ICosmosDb cosmosDb, Container container, IEnumerable<TModel> items, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier, new()
        {
            if (cosmosDb == null)
                throw new ArgumentNullException(nameof(cosmosDb));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (cosmosDb.Database != container.Database)
                throw new ArgumentException("There is a Cosmos Database mismatch to the specified Container", nameof(cosmosDb));

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
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
        /// <param name="items">The batch of items to create.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task ImportValueBatchAsync<TModel>(this ICosmosDbContainer container, IEnumerable<TModel> items, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier, new()
            => ImportValueBatchAsync(container?.CosmosDb!, container?.Container!, items, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> items specified within a <b>YAML</b> resource (see <see cref="JsonDataReader.ParseYaml(System.IO.Stream, JsonDataReaderArgs?)"/>) into the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="dataReaderArgs">The corresponding <see cref="JsonDataReaderArgs"/>.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportYamlValueBatchAsync<TResource, TModel>(this ICosmosDb cosmosDb, Container container, string resourceName, string? name = null, JsonDataReaderArgs? dataReaderArgs = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier, new()
        {
            if (cosmosDb == null)
                throw new ArgumentNullException(nameof(cosmosDb));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (cosmosDb.Database != container.Database)
                throw new ArgumentException("There is a Cosmos Database mismatch to the specified Container", nameof(cosmosDb));

            using var s = Abstractions.Resource.GetStream<TResource>(resourceName ?? throw new ArgumentNullException(nameof(resourceName)));
            var jdr = JsonDataReader.ParseYaml(s, dataReaderArgs);
            if (!jdr.TryDeserialize<TModel>(name, out var items))
                return false;

            await ImportValueBatchAsync(cosmosDb, container, items, modelUpdater, requestOptions, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> items specified within a <b>YAML</b> resource (see <see cref="JsonDataReader.ParseYaml(System.IO.Stream, JsonDataReaderArgs?)"/>) into the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="dataReaderArgs">The corresponding <see cref="JsonDataReaderArgs"/>.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportYamlValueBatchAsync<TResource, TModel>(this ICosmosDbContainer container, string resourceName, string? name = null, JsonDataReaderArgs? dataReaderArgs = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier, new()
            => ImportYamlValueBatchAsync<TResource, TModel>(container?.CosmosDb!, container?.Container!, resourceName, name, dataReaderArgs, modelUpdater, requestOptions, cancellationToken);

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> items specified within a <b>JSON</b> resource (see <see cref="JsonDataReader.ParseJson(System.IO.Stream, JsonDataReaderArgs?)"/>) into the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="cosmosDb">The <see cref="ICosmosDb"/>.</param>
        /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="dataReaderArgs">The corresponding <see cref="JsonDataReaderArgs"/>.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static async Task<bool> ImportJsonValueBatchAsync<TResource, TModel>(this ICosmosDb cosmosDb, Container container, string resourceName, string? name = null, JsonDataReaderArgs? dataReaderArgs = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier, new()
        {
            if (cosmosDb == null)
                throw new ArgumentNullException(nameof(cosmosDb));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (cosmosDb.Database != container.Database)
                throw new ArgumentException("There is a Cosmos Database mismatch to the specified Container", nameof(cosmosDb));

            using var s = Abstractions.Resource.GetStream<TResource>(resourceName ?? throw new ArgumentNullException(nameof(resourceName)));
            var jdr = JsonDataReader.ParseJson(s, dataReaderArgs);
            if (!jdr.TryDeserialize<TModel>(name, out var items))
                return false;

            await ImportValueBatchAsync(cosmosDb, container, items, modelUpdater, requestOptions, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Imports (creates) a batch of <see cref="CosmosDbValue{TModel}"/> items specified within a <b>JSON</b> resource (see <see cref="JsonDataReader.ParseJson(System.IO.Stream, JsonDataReaderArgs?)"/>) into the <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="Assembly"/> to find manifest resources (see <see cref="Assembly.GetManifestResourceStream(string)"/>).</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="container">The <see cref="ICosmosDbContainer"/>.</param>
        /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualifed resource name).</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="dataReaderArgs">The corresponding <see cref="JsonDataReaderArgs"/>.</param>
        /// <param name="modelUpdater">The function that enables the deserialized model value to be further updated.</param>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized and imported; otherwise, <c>false</c> for none found.</returns>
        /// <remarks>Each item is added individually and is not transactional.</remarks>
        public static Task<bool> ImportJsonValueBatchAsync<TResource, TModel>(this ICosmosDbContainer container, string resourceName, string? name = null, JsonDataReaderArgs? dataReaderArgs = null, Func<CosmosDbValue<TModel>, CosmosDbValue<TModel>>? modelUpdater = null, ItemRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) where TModel : class, IIdentifier, new()
            => ImportJsonValueBatchAsync<TResource, TModel>(container?.CosmosDb!, container?.Container!, resourceName, name, dataReaderArgs, modelUpdater, requestOptions, cancellationToken);
    }
}