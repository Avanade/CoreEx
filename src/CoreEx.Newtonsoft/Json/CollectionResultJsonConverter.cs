// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Newtonsoft.Json;
using System;
using System.Collections;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Performs JSON value conversion for <see cref="ICollectionResult"/> values.
    /// </summary>
    public class CollectionResultJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => typeof(ICollectionResult).IsAssignableFrom(objectType);

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return default;

            var cr = (ICollectionResult)Activator.CreateInstance(objectType);
            var coll = (ICollection?)serializer.Deserialize(reader, cr.CollectionType);

            if (coll != null)
                cr.Collection = coll;

            return cr;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, global::Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value == null || value is not ICollectionResult cr)
                return;

            writer.WriteStartArray();

            if (cr.Collection != null)
            {
                foreach (var item in cr.Collection)
                {
                    serializer.Serialize(writer, item);
                }
            }

            writer.WriteEndArray();
        }
    }
}