// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.RefData;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace CoreEx.Json.Data
{
    /// <summary>
    /// Reads JSON or YAML data and converts into a corresponding typed collection.
    /// </summary>
    public sealed class JsonDataReader : IDisposable
    {
        private readonly JsonDocument _jsonDocument;
        private readonly JsonProperty _json;
        private readonly JsonDataReaderArgs _args;
        private readonly bool _disposeDocument;
        private readonly ExecutionContext _executionContext;
        private readonly TypeReflectorArgs _typeReflectorArgs;

        private class YamlNodeTypeResolver : INodeTypeResolver
        {
            private static readonly string[] boolValues = { "y", "Y", "yes", "Yes", "YES", "n", "N", "no", "No", "NO", "true", "True", "TRUE", "false", "False", "FALSE", "on", "On", "ON", "off", "Off", "OFF" };

            /// <inheritdoc/>
            bool INodeTypeResolver.Resolve(NodeEvent? nodeEvent, ref Type currentType)
            {
                if (nodeEvent is Scalar scalar && scalar.Style == YamlDotNet.Core.ScalarStyle.Plain)
                {
                    if (decimal.TryParse(scalar.Value, out _))
                    {
                        if (scalar.Value.Length > 1 && scalar.Value.StartsWith('0')) // Valid JSON does not support a number that starts with a zero.
                            currentType = typeof(string);
                        else
                            currentType = typeof(decimal);

                        return true;
                    }

                    if (boolValues.Contains(scalar.Value))
                    {
                        currentType = typeof(bool);
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Reads and parses the YAML <see cref="string"/>.
        /// </summary>
        /// <param name="yaml">The YAML <see cref="string"/>.</param>
        /// <param name="args">The optional <see cref="JsonDataReaderArgs"/>.</param>
        /// <returns>The <see cref="JsonDataReader"/>.</returns>
        public static JsonDataReader ParseYaml(string yaml, JsonDataReaderArgs? args = null)
        {
            using var sr = new StringReader(yaml);
            return ParseYaml(sr, args);
        }

        /// <summary>
        /// Reads and parses the YAML <see cref="Stream"/>.
        /// </summary>
        /// <param name="s">The YAML <see cref="Stream"/>.</param>
        /// <param name="args">The optional <see cref="JsonDataReaderArgs"/>.</param>
        /// <returns>The <see cref="JsonDataReader"/>.</returns>
        public static JsonDataReader ParseYaml(Stream s, JsonDataReaderArgs? args = null) => ParseYaml(new StreamReader(s), args);

        /// <summary>
        /// Reads and parses the YAML <see cref="TextReader"/>.
        /// </summary>
        /// <param name="tr">The YAML <see cref="TextReader"/>.</param>
        /// <param name="args">The optional <see cref="JsonDataReaderArgs"/>.</param>
        /// <returns>The <see cref="JsonDataReader"/>.</returns>
        public static JsonDataReader ParseYaml(TextReader tr, JsonDataReaderArgs? args = null)
        {
            var yaml = new DeserializerBuilder().WithNodeTypeResolver(new YamlNodeTypeResolver()).Build().Deserialize(tr);
            var json = new SerializerBuilder().JsonCompatible().Build().Serialize(yaml!);
            return new(JsonDocument.Parse(json) ?? throw new InvalidOperationException("JsonNode.Parse resulted in a null."), args, true);
        }

        /// <summary>
        /// Reads and parses the JSON <see cref="string"/>.
        /// </summary>
        /// <param name="json">The JSON <see cref="string"/>.</param>
        /// <param name="args">The optional <see cref="JsonDataReaderArgs"/>.</param>
        /// <returns>The <see cref="JsonDataReader"/>.</returns>
        public static JsonDataReader ParseJson(string json, JsonDataReaderArgs? args = null) => new(JsonDocument.Parse(json) ?? throw new InvalidOperationException("JsonNode.Parse resulted in a null."), args, true);

        /// <summary>
        /// Reads and parses the JSON <see cref="Stream"/>.
        /// </summary>
        /// <param name="s">The JSON <see cref="Stream"/>.</param>
        /// <param name="args">The optional <see cref="JsonDataReaderArgs"/>.</param>
        /// <returns>The <see cref="JsonDataReader"/>.</returns>
        public static JsonDataReader ParseJson(Stream s, JsonDataReaderArgs? args = null) => new(JsonDocument.Parse(s) ?? throw new InvalidOperationException("JsonNode.Parse resulted in a null."), args, true);

        /// <summary>
        /// Reads and parses the <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonDocument"/>.</param>
        /// <param name="args">The optional <see cref="JsonDataReaderArgs"/>.</param>
        /// <returns>The <see cref="JsonDataReader"/>.</returns>
        /// <remarks>A <see cref="JsonDocument"/> is only used to read and navigate the JSON, any serialization operation will use the specified <see cref="JsonDataReaderArgs.JsonSerializer"/>.</remarks>
        public static JsonDataReader ParseJson(JsonDocument json, JsonDataReaderArgs? args = null) => new(json, args, false);

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDataReader"/> class.
        /// </summary>
        private JsonDataReader(JsonDocument json, JsonDataReaderArgs? args, bool disposeDocument)
        {
            _jsonDocument = json ?? throw new ArgumentNullException(nameof(json));
            try
            {
                if (_jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException("JSON root element must be an Object.", nameof(json));

                _json = json.RootElement.EnumerateObject().FirstOrDefault();
                if (_json.Value.ValueKind != JsonValueKind.Array)
                    throw new ArgumentException("JSON root element must be an Object with an underlying array.", nameof(json));
            }
            catch
            {
                if (disposeDocument)
                    _jsonDocument.Dispose();

                throw;
            }

            _args = args ?? new JsonDataReaderArgs();
            _disposeDocument = disposeDocument;
            _executionContext = new ExecutionContext { UserName = (string)_args.Parameters[JsonDataReaderArgs.UserNameKey]!, Timestamp = (DateTime)_args.Parameters[JsonDataReaderArgs.DateTimeNowKey]! };
            _typeReflectorArgs = new(_args.JsonSerializer);
        }

        /// <summary>
        /// Deserializes the contents of the named element into a collection of the specified <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to deserialize to.</typeparam>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="items">The resulting collection of items.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized; otherwise, <c>false</c> for none found.</returns>
        public bool TryDeserialize<T>(string? name, [NotNullWhen(true)] out List<T>? items)
        {
            items = null;

            // Find the named object and deserialize corresponding items.
            foreach (var ji in _json.Value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object))
            {
                foreach (var jo in ji.EnumerateObject().Where(x => x.Name == (name ?? typeof(T).Name) && x.Value.ValueKind == JsonValueKind.Array))
                {
                    foreach (var jd in jo.Value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object))
                    {
                        var item = Deserialize<T>(jd);
                        if (item != null)
                        {
                            (items ??= new List<T>()).Add(item);
                            _args.IdentifierGenerator?.AssignIdentifierAsync(item);
                            ChangeLog.PrepareCreated(item, _executionContext);
                            PrepareReferenceData(typeof(T), item, jd, items.Count - 1);
                        }
                    }
                }
            }

            return items != null;
        }

        /// <summary>
        /// Deserializes the contents of the named element into a collection of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to deserialize to.</param>
        /// <param name="name">The element name where the array of items to deserialize are housed. Defaults to the <see cref="Type"/> name.</param>
        /// <param name="items">The resulting collection of items.</param>
        /// <returns><c>true</c> indicates that one or more items were deserialized; otherwise, <c>false</c> for none found.</returns>
        public bool TryDeserialize(Type type, string? name, [NotNullWhen(true)] out List<object>? items)
        {
            items = null;

            // Find the named object and deserialize corresponding items.
            foreach (var ji in _json.Value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object))
            {
                foreach (var jo in ji.EnumerateObject().Where(x => x.Name == (name ?? type.Name) && x.Value.ValueKind == JsonValueKind.Array))
                {
                    foreach (var jd in jo.Value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object))
                    {
                        var item = Deserialize(type, jd);
                        if (item != null)
                        {
                            (items ??= new List<object>()).Add(item);
                            _args.IdentifierGenerator?.AssignIdentifierAsync(item);
                            ChangeLog.PrepareCreated(item, _executionContext);
                            PrepareReferenceData(type, item, jd, items.Count - 1);
                        }
                    }
                }
            }

            return items != null;
        }

        /// <summary>
        /// Deserialize the JSON replacing any dynamic parameters.
        /// </summary>
        private T? Deserialize<T>(JsonElement json)
        {
            using var ms = new MemoryStream();
            using var jw = new Utf8JsonWriter(ms);

            // Copy and replace JSON.
            CopyAndReplace(json, jw);
            jw.Flush();

            // Deserialize the new JSON.
            return _args.JsonSerializer.Deserialize<T>(new BinaryData(ms.ToArray()));
        }

        /// <summary>
        /// Deserialize the JSON replacing any dynamic parameters.
        /// </summary>
        private object? Deserialize(Type type, JsonElement json)
        {
            using var ms = new MemoryStream();
            using var jw = new Utf8JsonWriter(ms);

            // Copy and replace JSON.
            CopyAndReplace(json, jw);
            jw.Flush();

            // Deserialize the new JSON.
            return _args.JsonSerializer.Deserialize(new BinaryData(ms.ToArray()), type);
        }

        /// <summary>
        /// Copy existing and replace any dynamic parameters.
        /// </summary>
        private void CopyAndReplace(JsonElement je, Utf8JsonWriter jw)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Array:
                    jw.WriteStartArray();
                    je.EnumerateArray().ForEach(j => CopyAndReplace(j, jw));
                    jw.WriteEndArray();
                    break;

                case JsonValueKind.Object:
                    jw.WriteStartObject();
                    je.EnumerateObject().ForEach(j =>
                    {
                        jw.WritePropertyName(j.Name);
                        CopyAndReplace(j.Value, jw);
                    });

                    jw.WriteEndObject();
                    break;

                case JsonValueKind.String:
                    ReplaceDynamicParameter(je, jw);
                    break;

                default:
                    je.WriteTo(jw);
                    break;
            }
        }

        /// <summary>
        /// Replace any '^' placholders.
        /// </summary>
        private void ReplaceDynamicParameter(JsonElement je, Utf8JsonWriter jw)
        {
            var str = je.GetString();
            if (!string.IsNullOrEmpty(str) && str.Length > 1 && str[0] == '^')
            {
                if (str.StartsWith("^(") && str.EndsWith(")"))
                {
                    var val = GetRuntimeValue(_args.Parameters, str[2..^1]);
                    if (val == null)
                    {
                        jw.WriteNullValue();
                        return;
                    }

                    switch (val)
                    {
                        case string sv: jw.WriteStringValue(sv); break;
                        case Guid gv: jw.WriteStringValue(gv); break;
                        case DateTime dv: jw.WriteStringValue(dv); break;
                        case DateTimeOffset ov: jw.WriteStringValue(ov); break;
                        case bool bv: jw.WriteBooleanValue(bv); break;
                        case short nsv: jw.WriteNumberValue(nsv); break;
                        case int niv: jw.WriteNumberValue(niv); break;
                        case long nlv: jw.WriteNumberValue(nlv); break;
                        case ushort nusv: jw.WriteNumberValue(nusv); break;
                        case uint nuiv: jw.WriteNumberValue(nuiv); break;
                        case ulong nulv: jw.WriteNumberValue(nulv); break;
                        case decimal ndv: jw.WriteNumberValue(ndv); break;
                        case double n2v: jw.WriteNumberValue(n2v); break;
                        case float nfv: jw.WriteNumberValue(nfv); break;
                        default: jw.WriteStringValue(val.ToString()); break;
                    }

                    return;
                }
                else if (_args.ReplaceShorthandGuids && int.TryParse(str[1..], out var i))
                {
                    jw.WriteStringValue(new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
                    return;
                }
            }

            je.WriteTo(jw);
        }

        /// <summary>
        /// Gets the runtime value for the specified key.
        /// </summary>
        public static object? GetRuntimeValue(IDictionary<string, object?> parameters, string key)
        {
            // Check against known values and runtime parameters.
            if ((parameters ?? throw new ArgumentNullException(nameof(parameters))).TryGetValue(key ?? throw new ArgumentNullException(nameof(key)), out object? dval))
                return dval;

            // Try instantiating as defined.
            var (val, msg) = GetSystemRuntimeValue(key);
            if (msg == null)
                return val;

            // Try again adding the System namespace.
            (val, msg) = GetSystemRuntimeValue("System." + key);
            if (msg == null)
                return val;

            throw new ArgumentException(msg, nameof(key));
        }

        /// <summary>
        /// Get the system runtime value.
        /// </summary>
        private static (object? value, string? message) GetSystemRuntimeValue(string param)
        {
            var ns = param.Split(",");
            if (ns.Length > 2)
                return (null, $"Runtime value parameter '{param}' is invalid; incorrect format.");

            var parts = ns[0].Split(".");
            if (parts.Length <= 1)
                return (null, $"Runtime value parameter '{param}' is invalid; incorrect format.");

            Type? type = null;
            int i = parts.Length;
            for (; i >= 0; i--)
            {
                if (ns.Length == 1)
                    type = Type.GetType(string.Join('.', parts[0..^(parts.Length - i)]));
                else
                    type = Type.GetType(string.Join('.', parts[0..^(parts.Length - i)]) + "," + ns[1]);

                if (type != null)
                    break;
            }

            if (type == null)
                return (null, $"Runtime value parameter '{param}' is invalid; no Type can be found.");

            return GetSystemPropertyValue(param, type, null, parts[i..]);
        }

        /// <summary>
        /// Recursively navigates the properties and values to discern the value.
        /// </summary>
        private static (object? value, string? message) GetSystemPropertyValue(string param, Type type, object? obj, string[] parts)
        {
            if (parts == null || parts.Length == 0)
                return (obj, null);

            var part = parts[0];
            if (part.EndsWith("()"))
            {
                var mi = type.GetMethod(part[0..^2], Array.Empty<Type>());
                if (mi == null || mi.GetParameters().Length != 0)
                    return (null, $"Runtime value parameter '{param}' is invalid; specified method '{part}' is invalid.");

                return GetSystemPropertyValue(param, mi.ReturnType, mi.Invoke(obj, null), parts[1..]);
            }
            else
            {
                var pi = type.GetProperty(part);
                if (pi == null || !pi.CanRead)
                    return (null, $"Runtime value parameter '{param}' is invalid; specified property '{part}' is invalid.");

                return GetSystemPropertyValue(param, pi.PropertyType, pi.GetValue(obj, null), parts[1..]);
            }
        }

        /// <summary>
        /// Prepare the <see cref="IReferenceData"/> value.
        /// </summary>
        private void PrepareReferenceData(Type type, object item, JsonElement json, int index)
        {
            if (item is not IReferenceData rd)
                return;

            if (rd.Code == null && rd.Text == null && json.EnumerateObject().Count() == 1)
            {
                var jp = json.EnumerateObject().Single();
                if (jp.Value.ValueKind == JsonValueKind.String)
                {
                    rd.Code = jp.Name;
                    rd.Text = jp.Value.GetString();
                }
            }

            if (_args.RefDataColumnDefaults.Count == 0)
                return;

            var tr = TypeReflector.GetReflector(_typeReflectorArgs, type);
            foreach (var rp in _args.RefDataColumnDefaults)
            {
                var pr = tr.GetProperty(rp.Key);
                if (pr.JsonName != null && !json.TryGetProperty(pr.JsonName, out _))
                {
                    pr.PropertyExpression.SetValue(item, rp.Value(index));
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposeDocument)
                _jsonDocument.Dispose();
        }
    }
}