// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="ReferenceDataMultiDictionary"/> values.
    /// </summary>
    /// <remarks>This is required to ensure each <see cref="IReferenceDataCollection"/> is serialized correctly according to its underlying type.</remarks>
    public class ReferenceDataMultiDictionaryConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(ReferenceDataMultiDictionary);

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new ReferenceDataMultiDictionaryConverter();

        /// <summary>
        /// Performs the "actual" JSON value conversion for <see cref="ReferenceDataMultiDictionary"/> values.
        /// </summary>
        private class ReferenceDataMultiDictionaryConverter : JsonConverter<ReferenceDataMultiDictionary>
        {
            /// <inheritdoc/>
            public override ReferenceDataMultiDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException($"Deserialization of Type {nameof(ReferenceDataMultiDictionary)} is not supported.");

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, ReferenceDataMultiDictionary value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach (var kvp in value)
                {
                    writer.WritePropertyName(options.DictionaryKeyPolicy?.ConvertName(kvp.Key) ?? kvp.Key);
                    System.Text.Json.JsonSerializer.Serialize(writer, kvp.Value, kvp.Value.GetType(), options);
                }

                writer.WriteEndObject();
            }
        }
    }
}