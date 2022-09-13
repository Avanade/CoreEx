// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides <see cref="JsonTokenType.Number"/> to <see cref="JsonTokenType.String"/> conversion where the number is a valid <see cref="long"/>.
    /// </summary>
    public class NumberToStringConverter : JsonConverter<string>
    {
        /// <inheritdoc/>
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
        { 
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetDecimal(out var _) ? System.Text.Encoding.Default.GetString(reader.ValueSpan) : throw new JsonException(),
            _ => throw new JsonException()
        };

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}