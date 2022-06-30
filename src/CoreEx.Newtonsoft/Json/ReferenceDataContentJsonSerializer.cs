// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using CoreEx.RefData;
using Newtonsoft.Json;
using Nsj = Newtonsoft.Json;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Provides the JSON Serialize and Deserialize implementation to allow <see cref="IReferenceData"/> types to serialize contents.
    /// </summary>
    /// <remarks>Generally, <see cref="IReferenceData"/> types will serialize the <see cref="IReferenceData.Code"/> as the value; this allows for full <see cref="IReferenceData"/> contents to be serialized.</remarks>
    public class ReferenceDataContentJsonSerializer : JsonSerializer, IReferenceDataContentJsonSerializer
    {
        /// <summary>
        /// Gets or sets the default <see cref="JsonSerializerSettings"/> without <see cref="ReferenceDataJsonConverter"/> to allow <see cref="IReferenceData"/> types to serialize contents.
        /// </summary>
        /// <remarks>The following <see cref="JsonSerializerSettings"/> will default:
        /// <list type="bullet">
        ///  <item><description><see cref="JsonSerializerSettings.DefaultValueHandling"/> = <see cref="DefaultValueHandling.Ignore"/>.</description></item>
        ///  <item><description><see cref="JsonSerializerSettings.NullValueHandling"/> = <see cref="NullValueHandling.Ignore"/>.</description></item>
        ///  <item><description><see cref="JsonSerializerSettings.Formatting"/> = <see cref="Formatting.None"/>.</description></item>
        ///  <item><description><see cref="JsonSerializerSettings.ContractResolver"/> = <see cref="ContractResolver.Default"/>.</description></item>
        ///  <item><description><see cref="JsonSerializerSettings.Converters"/> = <see cref="Nsj.Converters.StringEnumConverter"/> and <see cref="CollectionResultJsonConverter"/>.</description></item>
        /// </list>
        /// </remarks>
        public static new JsonSerializerSettings DefaultSettings { get; set; } = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            ContractResolver = ContractResolver.Default,
            Converters = { new Nsj.Converters.StringEnumConverter(), new CollectionResultJsonConverter() }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializer"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="JsonSerializerSettings"/>. Defaults to <see cref="DefaultSettings"/>.</param>
        public ReferenceDataContentJsonSerializer(JsonSerializerSettings? settings = null) : base(settings ?? DefaultSettings) { }
    }
}