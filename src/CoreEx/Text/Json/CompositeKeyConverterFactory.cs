// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="CompositeKey"/> values.
    /// </summary>
    public class CompositeKeyConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(CompositeKey);

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new CompositeKeyConverter();

        /// <summary>
        /// Performs the "actual" JSON value conversion for a <see cref="CompositeKey"/>.
        /// </summary>
        private class CompositeKeyConverter : JsonConverter<CompositeKey>
        {
            /// <inheritdoc/>
            public override bool HandleNull => true;

            /// <inheritdoc/>
            public override CompositeKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return CompositeKey.Empty;

                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException($"Expected {nameof(JsonTokenType.StartArray)} for a {nameof(CompositeKey)}; found {reader.TokenType}.");

                var depth = reader.CurrentDepth;
                var args = new List<object?>();
                
                reader.Read();
                while (reader.CurrentDepth > depth)
                {
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        args.Add(null);
                        reader.Read();
                        continue;
                    }

                    if (reader.TokenType != JsonTokenType.StartObject)
                        throw new JsonException($"Expected {nameof(JsonTokenType.StartObject)} for a {nameof(CompositeKey)}; found {reader.TokenType}.");

                    var objDepth = reader.CurrentDepth;
                    reader.Read();
                    while (reader.CurrentDepth > objDepth)
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName)
                            throw new JsonException($"Expected {nameof(JsonTokenType.PropertyName)} for a {nameof(CompositeKey)}; found {reader.TokenType}.");

                        var name = reader.GetString();
                        reader.Read();

                        switch (name)
                        {
                            case "string": args.Add(reader.GetString()); break;
                            case "char": args.Add(reader.GetString()?.ToCharArray().FirstOrDefault()); break;
                            case "short": args.Add(reader.GetInt16()); break;
                            case "int": args.Add(reader.GetInt32()); break;
                            case "long": args.Add(reader.GetInt64()); break;
                            case "guid": args.Add(reader.GetGuid()); break;
                            case "datetime": args.Add(reader.GetDateTime()); break;
                            case "datetimeoffset": args.Add(reader.GetDateTimeOffset()); break;
                            case "ushort": args.Add(reader.GetUInt16()); break;
                            case "uint": args.Add(reader.GetUInt32()); break;
                            case "ulong": args.Add(reader.GetUInt64()); break;
                            default:
                                throw new JsonException($"Unsupported {nameof(CompositeKey)} type '{name}'.");
                        }

                        reader.Read();
                        if (reader.TokenType != JsonTokenType.EndObject)
                            throw new JsonException($"Expected {nameof(JsonTokenType.EndObject)} for a {nameof(CompositeKey)} argument; found {reader.TokenType}.");
                    }

                    reader.Read();
                }   

                return new CompositeKey([.. args]);
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, CompositeKey value, JsonSerializerOptions options)
            {
                if (value.Args.Length == 0)
                {
                    writer.WriteNullValue();
                    return;
                }

                writer.WriteStartArray();

                foreach (var arg in value.Args)
                {
                    if (arg is null)
                    {
                        writer.WriteNullValue();
                        continue;
                    }

                    writer.WriteStartObject();

                    _ = arg switch
                    {
                        string str => JsonWrite(writer, "string", () => writer.WriteStringValue(str)),
                        char c => JsonWrite(writer, "char", () => writer.WriteStringValue(c.ToString())),
                        short s => JsonWrite(writer, "short", () => writer.WriteNumberValue(s)),
                        int i => JsonWrite(writer, "int", () => writer.WriteNumberValue(i)),
                        long l => JsonWrite(writer, "long", () => writer.WriteNumberValue(l)),
                        Guid g => JsonWrite(writer, "guid", () => writer.WriteStringValue(g)),
                        DateTime d => JsonWrite(writer, "datetime", () => writer.WriteStringValue(d)),
                        DateTimeOffset o => JsonWrite(writer, "datetimeoffset", () => writer.WriteStringValue(o)),
                        ushort us => JsonWrite(writer, "ushort", () => writer.WriteNumberValue(us)),
                        uint ui => JsonWrite(writer, "uint", () => writer.WriteNumberValue(ui)),
                        ulong ul => JsonWrite(writer, "ulong", () => writer.WriteNumberValue(ul)),
                        _ => throw new JsonException($"Unsupported {nameof(CompositeKey)} type '{arg.GetType().Name}'.")
                    };

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            /// <summary>
            /// Provides a simple means to write a JSON property name and value.
            /// </summary>
            private static bool JsonWrite(Utf8JsonWriter writer, string name, Action action)
            {
                writer.WritePropertyName(name);
                action();
                return true;
            }
        }
    }
}