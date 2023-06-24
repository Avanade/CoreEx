// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using Newtonsoft.Json;
using System;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Represents a special-purpose <b>CosmosDb</b> object that houses an underlying model-<see cref="Value"/>, including <see cref="Type"/> name, and flexible <see cref="IIdentifier"/>, for persistence.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Value"/> <see cref="Type"/>.</typeparam>
    /// <remarks>The <see cref="CosmosDbModelBase.Id"/>, <see cref="Type"/> and <see cref="CosmosDbModelBase.ETag"/> are updated internally when interacting directly with <b>CosmosDB</b>.</remarks>
    public sealed class CosmosDbValue<TModel> : CosmosDbModelBase, ICosmosDbValue where TModel : class, IIdentifier, new()
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
            _value = value ?? throw new ArgumentNullException(nameof(value));
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
        public TModel Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(Value)); }

        /// <summary>
        /// Gets the value.
        /// </summary>
        object ICosmosDbValue.Value => _value;

        /// <summary>
        /// Prepares the object before sending to Cosmos.
        /// </summary>
        void ICosmosDbValue.PrepareBefore(ICosmosDb db)
        {
            if (Value != default)
            {
                Id = db.FormatIdentifier(Value.Id);

                if (Value is IETag etag)
                    ETag = ETagGenerator.FormatETag(etag.ETag);
            }

            Type = typeof(TModel).Name;
        }

        /// <summary>
        /// Prepares the object after getting from Cosmos.
        /// </summary>
        void ICosmosDbValue.PrepareAfter(ICosmosDb db)
        {
            if (Value == default)
                return;

            Value.Id = db.ParseIdentifier(Value.IdType, Id);

            if (Value is IETag etag)
                etag.ETag = ETagGenerator.ParseETag(ETag);
        }
    }
}