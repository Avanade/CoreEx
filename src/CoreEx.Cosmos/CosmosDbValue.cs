// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Cosmos.Model;
using CoreEx.Entities;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Represents a special-purpose <b>CosmosDb</b> object that houses an underlying model-<see cref="Value"/>, including <see cref="System.Type"/> name, and flexible <see cref="IEntityKey"/>, for persistence.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Value"/> <see cref="System.Type"/>.</typeparam>
    /// <remarks>The <see cref="CosmosDbModelBase.Id"/>, <see cref="System.Type"/> and <see cref="CosmosDbModelBase.ETag"/> are updated internally, where possible, when interacting directly with <b>CosmosDB</b>.</remarks>
    public sealed class CosmosDbValue<TModel> : CosmosDbModelBase, ICosmosDbValue where TModel : class, IEntityKey, new()
    {
        private TModel _value;
        private string _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValue{TModel}"/> class.
        /// </summary>
        public CosmosDbValue()
        {
            _type = typeof(TModel).Name;
            _value = new();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValue{TModel}"/> class with a <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public CosmosDbValue(TModel value)
        {
            _type = typeof(TModel).Name;
            _value = value.ThrowIfNull(nameof(value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValue{TModel}"/> class with a <paramref name="type"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> name override.</param>
        /// <param name="value">The value.</param>
        public CosmosDbValue(string? type, TModel value)
        {
            _type = type ?? typeof(TModel).Name;
            _value = value.ThrowIfNull(nameof(value));
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Type"/> name.
        /// </summary>
        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get => _type; set => _type = value.ThrowIfNullOrEmpty(nameof(Type)); }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public TModel Value { get => _value; set => _value = value.ThrowIfNull(nameof(Value)); }

        /// <summary>
        /// Gets the value.
        /// </summary>
        object ICosmosDbValue.Value => _value;

        /// <inheritdoc/>
        void ICosmosDbValue.PrepareBefore(CosmosDbArgs dbArgs, string? type)
        {
            if (Value != default)
            {
                Id = dbArgs.FormatIdentifier(Value.EntityKey);

                if (Value is IETag etag)
                    ETag = ETagGenerator.FormatETag(etag.ETag);

                if (Value is IPartitionKey pk)
                    PartitionKey = pk.PartitionKey;
            }

            if (string.IsNullOrEmpty(type))
                Type ??= typeof(TModel).Name;
            else
                Type = type;
        }

        /// <inheritdoc/>
        void ICosmosDbValue.PrepareAfter(CosmosDbArgs dbArgs)
        {
            if (Value == default)
                return;

            if (Value is IETag etag)
                etag.ETag = ETagGenerator.ParseETag(ETag);
        }
    }
}