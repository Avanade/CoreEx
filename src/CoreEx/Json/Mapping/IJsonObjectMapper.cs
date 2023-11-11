// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;
using System.Text.Json.Nodes;

namespace CoreEx.Json.Mapping
{
    /// <summary>
    /// Defines a <see cref="JsonObject"/> mapper.
    /// </summary>
    public interface IJsonObjectMapper
    {
        /// <summary>
        /// Gets the <see cref="Text.Json.JsonSerializer"/>.
        /// </summary>
        Text.Json.JsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the source <see cref="Type"/> being mapped from/to a <see cref="JsonObject"/>.
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Maps from an <paramref name="json"/> creating a corresponding instance of the <see cref="SourceType"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonObject"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of the <see cref="SourceType"/>.</returns>
        object? MapFromJson(JsonObject json, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="json"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="json">The <see cref="JsonObject"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToJson(object? value, JsonObject json, OperationTypes operationType = OperationTypes.Unspecified);
    }
}