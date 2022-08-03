// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="ReferenceDataMultiCollection"/> which must be manually serialized as <c>System.Text.Json</c> does not serialize correctly natively.
    /// </summary>
    public class ReferenceDataMultiCollectionConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(ReferenceDataMultiCollection);

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new JsonValueConverter();

        /// <summary>
        /// Performs the "actual" JSON value conversion for a <see cref="ReferenceDataMultiCollection"/>.
        /// </summary>
        private class JsonValueConverter : JsonConverter<ReferenceDataMultiCollection>
        {
            /// <inheritdoc/>
            public override bool HandleNull => false;

            /// <inheritdoc/>
            public override ReferenceDataMultiCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException($"Deserialization of Type {nameof(ReferenceDataMultiCollection)} is not supported.");

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, ReferenceDataMultiCollection coll, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (var item in coll)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("name");
                    writer.WriteStringValue(item.Name);
                    writer.WritePropertyName("items");
                    writer.WriteStartArray();

                    Type? rdiType = null;
                    foreach (var rdi in item.Items)
                    {
                        System.Text.Json.JsonSerializer.Serialize(writer, rdi, rdiType ??= rdi.GetType(), options);
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
        }
    }
}