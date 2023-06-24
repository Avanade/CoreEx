// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="IResult"/> values.
    /// </summary>
    public class ResultConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeof(IResult).IsAssignableFrom(typeToConvert);

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new ResultConverter();

        /// <summary>
        /// Performs the "actual" JSON value conversion for a <see cref="Result"/>.
        /// </summary>
        private class ResultConverter : JsonConverter<IResult>
        {
            /// <inheritdoc/>
            public override bool HandleNull => false;

            /// <inheritdoc/>
            public override IResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException($"Deserialization of Type {nameof(IResult)} is not supported.");

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, IResult value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                if (value.IsSuccess)
                {
                    writer.WritePropertyName("isSuccess");
                    writer.WriteBooleanValue(true);

                    if (value.Value is not null)
                    {
                        writer.WritePropertyName("value");
                        System.Text.Json.JsonSerializer.Serialize(writer, value.Value, value.Value.GetType(), options);
                    }
                }
                else
                {
                    writer.WritePropertyName("isFailure");
                    writer.WriteBooleanValue(true);
                    writer.WritePropertyName("error");
                    writer.WriteStringValue(value.Error.Message);
                }

                writer.WriteEndObject();
            }
        }
    }
}