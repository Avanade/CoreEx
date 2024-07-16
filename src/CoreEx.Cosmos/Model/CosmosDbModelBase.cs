// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Provides the base <b>CosmosDb</b> <b>model</b> capabilities.
    /// </summary>
    public abstract class CosmosDbModelBase : IIdentifier<string>, IETag, IPartitionKey
    {
        /// <summary>
        /// Gets or sets the <see cref="IIdentifier{TId}.Id"/>.
        /// </summary>
        [JsonProperty("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IETag"/>.
        /// </summary>
        [JsonProperty("_etag")]
        [JsonPropertyName("_etag")]
        public string? ETag { get; set; }

        /// <summary>
        /// Gets or sets the time-to-live (https://docs.microsoft.com/en-us/azure/cosmos-db/time-to-live).
        /// </summary>
        [JsonProperty("ttl", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonPropertyName("ttl")]
        public int? TimeToLive { get; set; }

        /// <summary>
        /// Gets or sets an optional <b>CosmosDb</b> <b>partition key</b> that can be used when required.
        /// </summary>
        /// <remarks>This property exists to support scenarios where the partition key is not represented in the underlying <see cref="ICosmosDbValue.Value"/> itself.</remarks>
        [JsonProperty("_partitionKey")]
        [JsonPropertyName("_partitionKey")]
        public string? PartitionKey { get; set; }
    }
}