// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="IReferenceData"/> values.
    /// </summary>
    public class ReferenceDataConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeof(IReferenceData).IsAssignableFrom(typeToConvert);

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => (JsonConverter)Activator.CreateInstance(typeof(JsonValueConverterReferenceData<>).MakeGenericType(typeToConvert));

        /// <summary>
        /// Performs the "actual" JSON value conversion for <see cref="IReferenceData"/> values.
        /// </summary>
        private class JsonValueConverterReferenceData<T> : JsonConverter<T> where T : IReferenceData, new()
        {
            /// <inheritdoc/>
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return default;

                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException(null, new InvalidCastException($"The {nameof(IReferenceData)} property TokenType must be a {nameof(JsonTokenType.String)} not a {reader.TokenType}. To support an {nameof(IReferenceData)} Object consider using the {nameof(ReferenceDataContentJsonSerializer)} {nameof(CoreEx.Json.IJsonSerializer)} instead."));

                var code = reader.GetString();
                if (code == null)
                    return default;

                if (ExecutionContext.HasCurrent)
                {
                    var coll = ReferenceDataOrchestrator.Current.GetByType(typeToConvert);
                    if (coll != null && coll.TryGetByCode(code, out var rd))
                        return (T)rd!;
                }

                var rdx = new T() { Code = code };
                ((IReferenceData)rdx).SetInvalid();
                return rdx;
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                if (value == null || value.Code == null)
                    return;

                writer.WriteStringValue(value.Code);
            }
        }
    }
}