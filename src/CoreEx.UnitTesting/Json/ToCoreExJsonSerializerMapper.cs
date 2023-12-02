// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace UnitTestEx.Json
{
    /// <summary>
    /// Provides a wrapper for <see cref="IJsonSerializer"/> to <see cref="CoreEx.Json.IJsonSerializer"/>.
    /// </summary>
    /// <remarks>Only the compatible capabilities have been implemented.</remarks>
    public class ToCoreExJsonSerializerMapper : CoreEx.Json.IJsonSerializer
    {
        private readonly IJsonSerializer _testJsonSerializer;

        /// <summary>
        /// Initializes a new <see cref="ToCoreExJsonSerializerMapper"/> instance.
        /// </summary>
        /// <param name="testJsonSerializer">The <see cref="UnitTestEx.Json.IJsonSerializer"/>.</param>
        public ToCoreExJsonSerializerMapper(IJsonSerializer testJsonSerializer) => _testJsonSerializer = testJsonSerializer ?? throw new ArgumentNullException(nameof(testJsonSerializer));

        /// <inheritdoc/>
        public object Options => _testJsonSerializer.Options;

        /// <inheritdoc/>
        public object? Deserialize(string json) => _testJsonSerializer.Deserialize(json);

        /// <inheritdoc/>
        public object? Deserialize(string json, Type type) => _testJsonSerializer.Deserialize(json, type);

        /// <inheritdoc/>
        public T? Deserialize<T>(string json) => _testJsonSerializer.Deserialize<T>(json);

        /// <inheritdoc/>
        public object? Deserialize(BinaryData json) => throw new NotImplementedException();

        /// <inheritdoc/>
        public object? Deserialize(BinaryData json, Type type) => throw new NotImplementedException();

        /// <inheritdoc/>
        public T? Deserialize<T>(BinaryData json) => throw new NotImplementedException();

        /// <inheritdoc/>
        public string Serialize<T>(T value, CoreEx.Json.JsonWriteFormat? format = null) => _testJsonSerializer.Serialize(value, format == CoreEx.Json.JsonWriteFormat.Indented ? JsonWriteFormat.Indented : JsonWriteFormat.None);

        /// <inheritdoc/>
        public BinaryData SerializeToBinaryData<T>(T value, CoreEx.Json.JsonWriteFormat? format = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public bool TryApplyFilter<T>(T value, IEnumerable<string>? names, out string json, CoreEx.Json.JsonPropertyFilter filter = CoreEx.Json.JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<CoreEx.Json.IJsonPreFilterInspector>? preFilterInspector = null)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public bool TryApplyFilter<T>(T value, IEnumerable<string>? names, out object json, CoreEx.Json.JsonPropertyFilter filter = CoreEx.Json.JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<CoreEx.Json.IJsonPreFilterInspector>? preFilterInspector = null)
            => throw new NotImplementedException();

        /// <inheritdoc/>
        public bool TryGetJsonName(MemberInfo memberInfo, [NotNullWhen(true)] out string? jsonName)
            => throw new NotImplementedException();
    }
}