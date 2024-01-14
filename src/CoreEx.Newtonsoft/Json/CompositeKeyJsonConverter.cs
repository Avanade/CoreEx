// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="CompositeKey"/> values.
    /// </summary>
    public class CompositeKeyJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(CompositeKey);

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return CompositeKey.Empty;

            if (reader.TokenType != JsonToken.StartArray)
            {
                var jtr = (JsonTextReader)reader;
                throw new JsonSerializationException($"Expected {nameof(JsonToken.StartArray)} for a {nameof(CompositeKey)}; found {reader.TokenType}.", jtr.Path, jtr.LineNumber, jtr.LinePosition, null);
            }

            var depth = reader.Depth;
            var args = new List<object?>();

            reader.Read();
            while (reader.Depth > depth)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    args.Add(null);
                    reader.Read();
                    continue;
                }

                if (reader.TokenType != JsonToken.StartObject)
                {
                    var jtr = (JsonTextReader)reader;
                    throw new JsonSerializationException($"Expected {nameof(JsonToken.StartObject)} for a {nameof(CompositeKey)}; found {reader.TokenType}.", jtr.Path, jtr.LineNumber, jtr.LinePosition, null);
                }

                var objDepth = reader.Depth;
                reader.Read();
                while (reader.Depth > objDepth)
                {
                    if (reader.TokenType != JsonToken.PropertyName)
                    {
                        var jtr = (JsonTextReader)reader;
                        throw new JsonSerializationException($"Expected {nameof(JsonToken.PropertyName)} for a {nameof(CompositeKey)}; found {reader.TokenType}.", jtr.Path, jtr.LineNumber, jtr.LinePosition, null);
                    }

                    var name = reader.Value;

                    switch (name)
                    {
                        case "string": args.Add(reader.ReadAsString()); break;
                        case "char": args.Add(reader.ReadAsString()?.ToCharArray().FirstOrDefault()); break;
                        case "short": args.Add((short?)reader.ReadAsInt32()); break;
                        case "int": args.Add(reader.ReadAsInt32()); break;
                        case "long": args.Add((long?)reader.ReadAsDecimal()); break;
                        case "guid": args.Add(reader.ReadAsString() is string s && Guid.TryParse(s, out var g) ? g : null); break;
                        case "datetime": args.Add(reader.ReadAsDateTime()); break;
                        case "datetimeoffset": args.Add(reader.ReadAsDateTimeOffset()); break;
                        case "ushort": args.Add((ushort?)reader.ReadAsInt32()); break;
                        case "uint": args.Add((uint?)reader.ReadAsDecimal()); break;
                        case "ulong": args.Add((ulong?)reader.ReadAsDecimal()); break;
                        default:
                            var jtr = (JsonTextReader)reader;
                            throw new JsonSerializationException($"Unsupported {nameof(CompositeKey)} type '{name}'.", jtr.Path, jtr.LineNumber, jtr.LinePosition, null);
                    }

                    reader.Read();
                    if (reader.TokenType != JsonToken.EndObject)
                    {
                        var jtr = (JsonTextReader)reader;
                        throw new JsonSerializationException($"Expected {nameof(JsonToken.EndObject)} for a {nameof(CompositeKey)} argument; found {reader.TokenType}.", jtr.Path, jtr.LineNumber, jtr.LinePosition, null);
                    }
                }

                reader.Read();
            }

            return new CompositeKey([.. args]);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is not CompositeKey key || key.Args.Length == 0)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();

            foreach (var arg in key.Args)
            {
                if (arg is null)
                {
                    writer.WriteNull();
                    continue;
                }

                writer.WriteStartObject();

                _ = arg switch
                {
                    string str => JsonWrite(writer, "string", () => writer.WriteValue(str)),
                    char c => JsonWrite(writer, "char", () => writer.WriteValue(c.ToString())),
                    short s => JsonWrite(writer, "short", () => writer.WriteValue(s)),
                    int i => JsonWrite(writer, "int", () => writer.WriteValue(i)),
                    long l => JsonWrite(writer, "long", () => writer.WriteValue(l)),
                    Guid g => JsonWrite(writer, "guid", () => writer.WriteValue(g)),
                    DateTime d => JsonWrite(writer, "datetime", () => writer.WriteValue(d)),
                    DateTimeOffset o => JsonWrite(writer, "datetimeoffset", () => writer.WriteValue(o)),
                    ushort us => JsonWrite(writer, "ushort", () => writer.WriteValue(us)),
                    uint ui => JsonWrite(writer, "uint", () => writer.WriteValue(ui)),
                    ulong ul => JsonWrite(writer, "ulong", () => writer.WriteValue(ul)),
                    _ => throw new JsonException($"Unsupported {nameof(CompositeKey)} type '{arg.GetType().Name}'.")
                };

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        /// <summary>
        /// Provides a simple means to write a JSON property name and value.
        /// </summary>
        private static bool JsonWrite(JsonWriter writer, string name, Action action)
        {
            writer.WritePropertyName(name);
            action();
            return true;
        }
    }
}