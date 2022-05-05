// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nsj = Newtonsoft.Json;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Provides the <see cref="Nsj.JsonSerializer"/> encapsulated implementation.
    /// </summary>
    public class JsonSerializer : IJsonSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializer"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="JsonSerializerSettings"/>; where <c>null</c> these will default.</param>
        /// <remarks>Where the <paramref name="settings"/> are <c>null</c> the following <see cref="JsonSerializerSettings"/> will default:
        /// <list type="bullet">
        ///  <item><description><see cref="JsonSerializerSettings.DefaultValueHandling"/> = <see cref="DefaultValueHandling.Ignore"/>.</description></item>
        ///  <item><description><see cref="JsonSerializerSettings.NullValueHandling"/> = <see cref="NullValueHandling.Ignore"/></description></item>
        ///  <item><description><see cref="JsonSerializerSettings.Formatting"/> = <see cref="Formatting.None"/></description></item>
        ///  <item><description><see cref="JsonSerializerSettings.ContractResolver"/> = <see cref="ContractResolver.Default"/></description></item>
        ///  <item><description><see cref="JsonSerializerSettings.Converters"/> = <see cref="Nsj.Converters.StringEnumConverter"/></description></item>
        /// </list>
        /// </remarks>
        public JsonSerializer(JsonSerializerSettings? settings = null)
        {
            Settings = settings ?? new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                ContractResolver = ContractResolver.Default,
                Converters = { new Nsj.Converters.StringEnumConverter() }
            };
        }

        /// <summary>
        /// Gets the underlying serializer configuration settings/options.
        /// </summary>
        object IJsonSerializer.Options => Settings;

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/>.
        /// </summary>
        public JsonSerializerSettings Settings { get; }

        /// <inheritdoc/>
        public string Serialize<T>(T value, JsonWriteFormat? format = null) => SerializeToBinaryData(value, format).ToString();

        /// <inheritdoc/>
        public BinaryData SerializeToBinaryData<T>(T value, JsonWriteFormat? format = null)
        {
            var ms = new MemoryStream();
            using var sw = new StreamWriter(ms);
            using var jtw = new Nsj.JsonTextWriter(sw);
            Nsj.JsonSerializer.Create(format == null ? Settings : CopySettings(format!.Value)).Serialize(jtw, value);
            jtw.Flush();
            ms.Position = 0;
            return BinaryData.FromStream(ms);
        }

        /// <inheritdoc/>
        public object? Deserialize(string json) => Deserialize(BinaryData.FromString(json));

        /// <inheritdoc/>
        public object? Deserialize(string json, Type type) => Deserialize(BinaryData.FromString(json), type);

        /// <inheritdoc/>
        public T? Deserialize<T>(string json) =>Deserialize<T>(BinaryData.FromString(json))!;

        /// <inheritdoc/>
        public object? Deserialize(BinaryData json) => Deserialize<dynamic>(json);

        /// <inheritdoc/>
        public T? Deserialize<T>(BinaryData json) => (T?)Deserialize(json, typeof(T));

        /// <inheritdoc/>
        public object? Deserialize(BinaryData json, Type type)
        {
            using var s = json.ToStream();
            using var sr = new StreamReader(s);
            using var jtr = new Nsj.JsonTextReader(sr);
            return Nsj.JsonSerializer.Create(Settings).Deserialize(jtr, type);
        }

        /// <inheritdoc/>
        public bool TryApplyFilter<T>(T value, IEnumerable<string>? names, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
            => JsonFilterer.TryApply(value, names, out json, filter, Settings, comparison);

        /// <inheritdoc/>
        public bool TryApplyFilter<T>(T value, IEnumerable<string>? names, out object json, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            var r = JsonFilterer.TryApply(value, names, out JToken node, filter, Settings, comparison);
            json = node;
            return r;
        }

        /// <inheritdoc/>
        bool IJsonSerializer.TryGetJsonName(MemberInfo memberInfo, out string? jsonName)
        {
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));

            var ji = memberInfo.GetCustomAttribute<JsonIgnoreAttribute>(true);
            if (ji != null)
            {
                jsonName = null;
                return false;
            }

            var jpn = memberInfo.GetCustomAttribute<JsonPropertyAttribute>(true);
            if (jpn?.PropertyName != null)
            {
                jsonName = jpn.PropertyName;
                return true;
            }

            if (Settings.ContractResolver is ContractResolver cr)
            {
                var jo = memberInfo.DeclaringType.GetCustomAttribute<JsonObjectAttribute>(true);
                var jp = cr.GetProperty(memberInfo, jo == null ? MemberSerialization.OptOut : jo.MemberSerialization);
                if (jp != null)
                {
                    jsonName = jp.Ignored ? null : jp.PropertyName;
                    return !jp.Ignored;
                }
            }

            if (Settings.ContractResolver is CamelCasePropertyNamesContractResolver ccr && ccr.NamingStrategy != null)
                jsonName = ccr.NamingStrategy.GetPropertyName(memberInfo.Name, false);
            else
                jsonName = memberInfo.Name;

            return true;
        }

        /// <summary>
        /// Copies the settings.
        /// </summary>
        private JsonSerializerSettings CopySettings(JsonWriteFormat format)
        {
            var s = new JsonSerializerSettings
            {
                ReferenceLoopHandling = Settings.ReferenceLoopHandling,
                MissingMemberHandling = Settings.MissingMemberHandling,
                ObjectCreationHandling = Settings.ObjectCreationHandling,
                NullValueHandling = Settings.NullValueHandling,
                DefaultValueHandling = Settings.DefaultValueHandling,
                PreserveReferencesHandling = Settings.PreserveReferencesHandling,
                TypeNameHandling = Settings.TypeNameHandling,
                MetadataPropertyHandling = Settings.MetadataPropertyHandling,
                TypeNameAssemblyFormatHandling = Settings.TypeNameAssemblyFormatHandling,
                ConstructorHandling = Settings.ConstructorHandling,
                ContractResolver = Settings.ContractResolver,
                EqualityComparer = Settings.EqualityComparer,
                ReferenceResolverProvider = Settings.ReferenceResolverProvider,
                TraceWriter = Settings.TraceWriter,
                SerializationBinder = Settings.SerializationBinder,
                Error = Settings.Error,
                Context = Settings.Context,
                DateFormatString = Settings.DateFormatString,
                MaxDepth = Settings.MaxDepth,
                Formatting = format == JsonWriteFormat.None ? Formatting.None : Formatting.Indented,
                DateFormatHandling = Settings.DateFormatHandling,
                DateTimeZoneHandling = Settings.DateTimeZoneHandling,
                DateParseHandling = Settings.DateParseHandling,
                FloatFormatHandling = Settings.FloatFormatHandling,
                FloatParseHandling = Settings.FloatParseHandling,
                StringEscapeHandling = Settings.StringEscapeHandling,
                Culture = Settings.Culture,
                CheckAdditionalContent = Settings.CheckAdditionalContent,
            };

            if (Settings.Converters != null)
                s.Converters = new List<JsonConverter>(Settings.Converters);

            return s;
        }
    }
}