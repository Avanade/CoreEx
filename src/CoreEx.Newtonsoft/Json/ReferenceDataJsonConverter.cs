// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using Newtonsoft.Json;
using System;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="IReferenceData"/> values.
    /// </summary>
    public class ReferenceDataJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => typeof(IReferenceData).IsAssignableFrom(objectType);

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return default;

            if (reader.TokenType != JsonToken.String)
            {
                var jtr = (JsonTextReader)reader;
                throw new JsonSerializationException("Reference data value must be a string.", jtr.Path, jtr.LineNumber, jtr.LinePosition, null);
            }

            if (reader.Value is not string code)
                return default;

            if (ExecutionContext.HasCurrent)
            {
                var coll = ReferenceDataOrchestrator.Current.GetByType(objectType);
                if (coll != null && coll.TryGetByCode(code, out var rd))
                    return rd;
            }

            var rdx = (IReferenceData)Activator.CreateInstance(objectType)!;
            rdx.Code = code;
            rdx.SetInvalid();
            return rdx;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is not IReferenceData rd || rd.Code == null)
                return;

            writer.WriteValue(rd.Code);
        }
    }
}