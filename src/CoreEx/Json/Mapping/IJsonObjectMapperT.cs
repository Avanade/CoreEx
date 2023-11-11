// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using System;
using System.Text.Json.Nodes;

namespace CoreEx.Json.Mapping
{
    /// <summary>
    /// Defines a typed <see cref="JsonObject"/> mapper.
    /// </summary>
    public interface IJsonObjectMapper<TSource> : IJsonObjectMapper
    {
        /// <inheritdoc/>
        Type IJsonObjectMapper.SourceType => typeof(TSource);

        /// <inheritdoc/>
        object? IJsonObjectMapper.MapFromJson(JsonObject json, OperationTypes operationType) => MapFromJson(json, operationType)!;

        /// <inheritdoc/>
        void IJsonObjectMapper.MapToJson(object? value, JsonObject json, OperationTypes operationType) => MapToJson((TSource?)value, json, operationType);

        /// <summary>
        /// Maps from a <paramref name="json"/> creating a corresponding instance of <typeparamref name="TSource"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonObject"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The corresponding instance of <typeparamref name="TSource"/>.</returns>
        new TSource? MapFromJson(JsonObject json, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="json"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="json">The <see cref="JsonObject"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        void MapToJson(TSource? value, JsonObject json, OperationTypes operationType = OperationTypes.Unspecified);
    }
}