// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stj = System.Text.Json;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Converter for <see cref="Exception"/>. see <see href="https://github.com/dotnet/runtime/issues/43026"/>.
    /// It can serialize <see cref="Exception"/> to <see cref="JsonElement"/> and vice versa, but deserialization is very basic - only handles <see cref="Exception.Message"/>
    /// </summary>
    public class ExceptionConverter<TExceptionType> : JsonConverter<TExceptionType>
     where TExceptionType : Exception
    {

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Exception).IsAssignableFrom(typeToConvert);
        }

        /// <inheritdoc/>
        public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            TExceptionType exception = default!;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return exception;
                }

                // Get the key.
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                string? propertyName = reader.GetString();

                if (propertyName != nameof(Exception.Message))
                {
                    // Skip all properties other than the message.
                    reader.Skip();
                    continue;
                }

                reader.Read();
                string? message = reader.GetString();
                exception = (TExceptionType)Activator.CreateInstance(typeof(TExceptionType), message);

                // read the rest of the exception
                while(reader.Read())
                {
                    reader.Skip();
                }
                return exception;
            }

            throw new JsonException();
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
        {
            var serializableProperties = value.GetType()
                .GetProperties()
                .Select(uu => new { uu.Name, Value = uu.GetValue(value) })
                .Where(uu => uu.Name != nameof(Exception.TargetSite));

            if (options?.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            {
                serializableProperties = serializableProperties.Where(uu => uu.Value != null);
            }

            var propList = serializableProperties.ToList();

            if (propList.Count == 0)
            {
                // Nothing to write
                return;
            }

            writer.WriteStartObject();

            foreach (var prop in propList)
            {
                writer.WritePropertyName(prop.Name);
                Stj.JsonSerializer.Serialize(writer, prop.Value, options);
            }

            writer.WriteEndObject();
        }
    }

    /// <summary> 
    /// Exception converter factory
    /// </summary>
    public class ExceptionConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Exception).IsAssignableFrom(typeToConvert);
        }

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(
               typeof(ExceptionConverter<>).MakeGenericType(typeToConvert));
        }
    }
}