// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="ICollectionResult"/> values.
    /// </summary>
    public class CollectionResultConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeof(ICollectionResult).IsAssignableFrom(typeToConvert);

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => (JsonConverter)Activator.CreateInstance(typeof(JsonValueConverterReferenceData<>).MakeGenericType(typeToConvert))!;

        /// <summary>
        /// Performs the "actual" JSON value conversion for <see cref="ICollectionResult"/> values.
        /// </summary>
        private class JsonValueConverterReferenceData<T> : JsonConverter<T> where T : ICollectionResult, new()
        {
            /// <inheritdoc/>
            public override bool HandleNull => false;

            /// <inheritdoc/>
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException();

                var cr = new T();
                cr.Items = (ICollection?)System.Text.Json.JsonSerializer.Deserialize(ref reader, cr.CollectionType, options) ?? throw new InvalidOperationException();
                return cr;
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                if (value.Items != null)
                {
                    foreach (var item in value.Items)
                    {
                        System.Text.Json.JsonSerializer.Serialize(writer, item, value.ItemType, options);
                    }
                }

                writer.WriteEndArray();
            }
        }
    }
}