// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using CoreEx.RefData;
using System.Text.Json.Serialization;
using Stj = System.Text.Json;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides the JSON Serialize and Deserialize implementation to allow <see cref="IReferenceData"/> types to serialize contents.
    /// </summary>
    /// <remarks>Generally, <see cref="IReferenceData"/> types will serialize the <see cref="IReferenceData.Code"/> as the value; this allows for full <see cref="IReferenceData"/> contents to be serialized.</remarks>
    /// <param name="options">The <see cref="Stj.JsonSerializerOptions"/>. Defaults to <see cref="DefaultOptions"/>.</param>
    public class ReferenceDataContentJsonSerializer(Stj.JsonSerializerOptions? options = null) : JsonSerializer(options ?? DefaultOptions), IReferenceDataContentJsonSerializer
    {
        /// <summary>
        /// Gets or sets the default <see cref="Stj.JsonSerializerOptions"/> without <see cref="CollectionResultConverterFactory"/> to allow <see cref="IReferenceData"/> types to serialize contents.
        /// </summary>
        /// <remarks>The following <see cref="Stj.JsonSerializerOptions"/>, including use of <see cref="Stj.JsonSerializerDefaults.Web"/>, will default:
        /// <list type="bullet">
        ///  <item><description><see cref="Stj.JsonSerializerOptions.DefaultIgnoreCondition"/> = <see cref="JsonIgnoreCondition.WhenWritingDefault"/>.</description></item>
        ///  <item><description><see cref="Stj.JsonSerializerOptions.WriteIndented"/> = <c>false</c></description></item>
        ///  <item><description><see cref="Stj.JsonSerializerOptions.DictionaryKeyPolicy"/> = <see cref="SubstituteNamingPolicy.Substitute"/>.</description></item>
        ///  <item><description><see cref="Stj.JsonSerializerOptions.PropertyNamingPolicy"/> = <see cref="SubstituteNamingPolicy.Substitute"/>.</description></item>
        ///  <item><description><see cref="Stj.JsonSerializerOptions.Converters"/> = <see cref="JsonStringEnumConverter"/>, <see cref="ExceptionConverterFactory"/>, <see cref="CollectionResultConverterFactory"/> and <see cref="ResultConverterFactory"/>.</description></item>
        /// </list>
        /// </remarks>
        public static new Stj.JsonSerializerOptions DefaultOptions { get; set; } = new Stj.JsonSerializerOptions(Stj.JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            WriteIndented = false,
            DictionaryKeyPolicy = SubstituteNamingPolicy.Substitute,
            PropertyNamingPolicy = SubstituteNamingPolicy.Substitute,
            Converters = { new JsonStringEnumConverter(), new ExceptionConverterFactory(), new CollectionResultConverterFactory(), new ResultConverterFactory() }
        };
    }
}