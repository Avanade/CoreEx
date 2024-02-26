// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace UnitTestEx.Json
{
    /// <summary>
    /// Provides a wrapper for <see cref="CoreEx.Json.IJsonSerializer"/> to <see cref="IJsonSerializer"/>.
    /// </summary>
    /// <remarks>Only the compatible capabilities have been implemented.</remarks>
    /// <param name="coreJsonSerializer">The <see cref="CoreEx.Json.IJsonSerializer"/>.</param>
    public class ToUnitTestExJsonSerializerMapper(CoreEx.Json.IJsonSerializer coreJsonSerializer) : IJsonSerializer, CoreEx.Json.IJsonSerializer
    {
        private readonly CoreEx.Json.IJsonSerializer _coreJsonSerializer = coreJsonSerializer.ThrowIfNull(nameof(coreJsonSerializer));

        /// <inheritdoc/>
        public object Options => _coreJsonSerializer.Options;

        /// <inheritdoc/>
        public object? Deserialize(string json) => _coreJsonSerializer.Deserialize(json);

        /// <inheritdoc/>
        public object? Deserialize(string json, Type type) => _coreJsonSerializer.Deserialize(json, type);

        /// <inheritdoc/>
        public T? Deserialize<T>(string json) => _coreJsonSerializer.Deserialize<T>(json);

        /// <inheritdoc/>
        public string Serialize<T>(T value, JsonWriteFormat? format = null) => _coreJsonSerializer.Serialize(value, format == JsonWriteFormat.Indented ? CoreEx.Json.JsonWriteFormat.Indented : CoreEx.Json.JsonWriteFormat.None);

        #region CoreEx.Json.IJsonSerializer

        /// <inheritdoc/>
        object? CoreEx.Json.IJsonSerializer.Deserialize(BinaryData json) => _coreJsonSerializer.Deserialize(json);

        /// <inheritdoc/>
        object? CoreEx.Json.IJsonSerializer.Deserialize(BinaryData json, Type type) => _coreJsonSerializer.Deserialize(json, type);

        /// <inheritdoc/>
        T? CoreEx.Json.IJsonSerializer.Deserialize<T>(BinaryData json) where T : default => _coreJsonSerializer.Deserialize<T>(json);

        /// <inheritdoc/>
        string CoreEx.Json.IJsonSerializer.Serialize<T>(T value, CoreEx.Json.JsonWriteFormat? format)
            => _coreJsonSerializer.Serialize(value, format);

        /// <inheritdoc/>
        BinaryData CoreEx.Json.IJsonSerializer.SerializeToBinaryData<T>(T value, CoreEx.Json.JsonWriteFormat? format) 
            => _coreJsonSerializer.SerializeToBinaryData(value, format);

        /// <inheritdoc/>
        bool CoreEx.Json.IJsonSerializer.TryApplyFilter<T>(T value, IEnumerable<string>? names, out string json, CoreEx.Json.JsonPropertyFilter filter, StringComparison comparison, Action<CoreEx.Json.IJsonPreFilterInspector>? preFilterInspector)
            => _coreJsonSerializer.TryApplyFilter(value, names, out json, filter, comparison, preFilterInspector);

        /// <inheritdoc/>
        bool CoreEx.Json.IJsonSerializer.TryApplyFilter<T>(T value, IEnumerable<string>? names, out object json, CoreEx.Json.JsonPropertyFilter filter, StringComparison comparison, Action<CoreEx.Json.IJsonPreFilterInspector>? preFilterInspector)
            => _coreJsonSerializer.TryApplyFilter(value, names, out json, filter, comparison, preFilterInspector);

        /// <inheritdoc/>
        bool CoreEx.Json.IJsonSerializer.TryGetJsonName(MemberInfo memberInfo, [NotNullWhen(true)] out string? jsonName)
            => _coreJsonSerializer.TryGetJsonName(memberInfo, out jsonName);

        #endregion
    }
}