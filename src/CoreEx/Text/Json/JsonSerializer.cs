// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Stj = System.Text.Json;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides the <see cref="Stj.JsonSerializer"/> encapsulated implementation.
    /// </summary>
    public class JsonSerializer : IJsonSerializer
    {
        /// <summary>
        /// Gets or sets the default <see cref="Stj.JsonSerializerOptions"/>.
        /// </summary>
        /// <remarks>The following <see cref="Stj.JsonSerializerOptions"/>, including use of <see cref="Stj.JsonSerializerDefaults.Web"/>, will default:
        /// <list type="bullet">
        ///  <item><description><see cref="Stj.JsonSerializerOptions.DefaultIgnoreCondition"/> = <see cref="JsonIgnoreCondition.WhenWritingDefault"/>.</description></item>
        ///  <item><description><see cref="Stj.JsonSerializerOptions.WriteIndented"/> = <c>false</c>.</description></item>
        ///  <item><description><see cref="Stj.JsonSerializerOptions.DictionaryKeyPolicy"/> = <see cref="SubstituteNamingPolicy.Substitute"/>.</description></item>
        ///  <item><description><see cref="Stj.JsonSerializerOptions.PropertyNamingPolicy"/> = <see cref="SubstituteNamingPolicy.Substitute"/>.</description></item>
        ///  <item><description><see cref="Stj.JsonSerializerOptions.Converters"/> = <see cref="JsonStringEnumConverter"/>, <see cref="ExceptionConverterFactory"/>, <see cref="ReferenceDataConverterFactory"/>,
        ///  <see cref="CollectionResultConverterFactory"/>, <see cref="ResultConverterFactory"/> and <see cref="CompositeKeyConverterFactory"/>.</description></item>
        /// </list>
        /// </remarks>
        public static Stj.JsonSerializerOptions DefaultOptions { get; set; } = new Stj.JsonSerializerOptions(Stj.JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            WriteIndented = false,
            DictionaryKeyPolicy = SubstituteNamingPolicy.Substitute,
            PropertyNamingPolicy = SubstituteNamingPolicy.Substitute,
            Converters = { new JsonStringEnumConverter(), new ExceptionConverterFactory(), new ReferenceDataConverterFactory(), new CollectionResultConverterFactory(), new ResultConverterFactory(), new CompositeKeyConverterFactory() }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializer"/> class.
        /// </summary>
        /// <param name="options">The <see cref="Stj.JsonSerializerOptions"/>. Defaults to <see cref="DefaultOptions"/>.</param>
        public JsonSerializer(Stj.JsonSerializerOptions? options = null)
        {
            Options = options ?? DefaultOptions;
            IndentedOptions = new Stj.JsonSerializerOptions(Options) { WriteIndented = true };
        }

        /// <summary>
        /// Gets the underlying serializer configuration settings/options.
        /// </summary>
        object IJsonSerializer.Options => Options;

        /// <summary>
        /// Gets the <see cref="Stj.JsonSerializerOptions"/>.
        /// </summary>
        public Stj.JsonSerializerOptions Options { get; }

        /// <summary>
        /// Gets or sets the <see cref="Stj.JsonSerializerOptions"/> with <see cref="Stj.JsonSerializerOptions.WriteIndented"/> = <c>true</c>.
        /// </summary>
        public Stj.JsonSerializerOptions? IndentedOptions { get; }

        /// <inheritdoc/>
        public string Serialize<T>(T value, JsonWriteFormat? format = null) => SerializeToBinaryData(value, format).ToString();

        /// <inheritdoc/>
        public BinaryData SerializeToBinaryData<T>(T value, JsonWriteFormat? format = null) 
            => new(Stj.JsonSerializer.SerializeToUtf8Bytes(value, format == null || format.Value == JsonWriteFormat.None ? Options : IndentedOptions));

        /// <inheritdoc/>
        public object? Deserialize(string json) => Stj.JsonSerializer.Deserialize<dynamic>(json, Options);

        /// <inheritdoc/>
        public object? Deserialize(string json, Type type) => Stj.JsonSerializer.Deserialize(json, type, Options);

        /// <inheritdoc/>
        public T? Deserialize<T>(string json) => Stj.JsonSerializer.Deserialize<T>(json, Options)!;

        /// <inheritdoc/>
        public object? Deserialize(BinaryData json) => Stj.JsonSerializer.Deserialize<dynamic>(json, Options);

        /// <inheritdoc/>
        public object? Deserialize(BinaryData json, Type type) => Stj.JsonSerializer.Deserialize(json, type, Options);

        /// <inheritdoc/>
        public T? Deserialize<T>(BinaryData json) => Stj.JsonSerializer.Deserialize<T>(json, Options)!;

        /// <inheritdoc/>
        public bool TryApplyFilter<T>(T value, IEnumerable<string>? names, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<IJsonPreFilterInspector>? preFilterInspector = null)
            => JsonFilterer.TryApply(value, names, out json, filter, Options, comparison, preFilterInspector);

        /// <inheritdoc/>
        public bool TryApplyFilter<T>(T value, IEnumerable<string>? names, out object json, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<IJsonPreFilterInspector>? preFilterInspector = null)
        {
            var r = JsonFilterer.TryApply(value, names, out JsonNode node, filter, Options, comparison, preFilterInspector);
            json = node;
            return r;
        }

        /// <inheritdoc/>
        bool IJsonSerializer.TryGetJsonName(MemberInfo memberInfo, [NotNullWhen(true)] out string? jsonName)
        {
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));

            var ji = memberInfo.GetCustomAttribute<JsonIgnoreAttribute>();
            if (ji != null)
            {
                jsonName = null;
                return false;
            }

            var jpn = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(true);
            jsonName = jpn?.Name ?? Options.PropertyNamingPolicy?.ConvertName(memberInfo.Name) ?? memberInfo.Name;
            return true;
        }
    }
}