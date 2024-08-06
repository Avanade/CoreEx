// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Cosmos.Model;
using CoreEx.Entities;
using Newtonsoft.Json;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Represents a special-purpose <b>CosmosDb</b> object that houses an underlying model-<see cref="Value"/>, including <see cref="Type"/> name, and flexible <see cref="IEntityKey"/>, for persistence.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Value"/> <see cref="Type"/>.</typeparam>
    /// <remarks>The <see cref="CosmosDbModelBase.Id"/>, <see cref="Type"/> and <see cref="CosmosDbModelBase.ETag"/> are updated internally, where possible, when interacting directly with <b>CosmosDB</b>.</remarks>
    public sealed class CosmosDbValue<TModel> : CosmosDbModelBase, ICosmosDbValue where TModel : class, IEntityKey, new()
    {
        private TModel _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValue{TModel}"/> class.
        /// </summary>
        public CosmosDbValue()
        {
            Type = typeof(TModel).Name;
            _value = new();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValue{TModel}"/> class with a <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public CosmosDbValue(TModel value)
        {
            Type = typeof(TModel).Name;
            _value = value.ThrowIfNull(nameof(value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValue{TModel}"/> class with a <paramref name="type"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> name override.</param>
        /// <param name="value">The value.</param>
        public CosmosDbValue(string? type, TModel value)
        {
            Type = type ?? typeof(TModel).Name;
            _value = value.ThrowIfNull(nameof(value));
        }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> name.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [JsonProperty("value")]
        public TModel Value { get => _value; set => _value = value.ThrowIfNull(nameof(Value)); }

        /// <summary>
        /// Gets the value.
        /// </summary>
        object ICosmosDbValue.Value => _value;

        /// <inheritdoc/>
        void ICosmosDbValue.PrepareBefore(CosmosDbArgs dbArgs, string? typeName)
        {
            if (Value != default)
            {
                Id = dbArgs.FormatIdentifier(Value.EntityKey);

                if (Value is IETag etag)
                    ETag = ETagGenerator.FormatETag(etag.ETag);

                if (Value is IPartitionKey pk)
                    PartitionKey = pk.PartitionKey;
            }

            if (!string.IsNullOrEmpty(typeName))
                Type = typeName;
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