// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Reflection;

namespace CoreEx.Json
{
    /// <summary>
    /// Provides the core (common) JSON Serialize and Deserialize capabilities.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Gets the underlying serializer configuration settings/options.
        /// </summary>
        object Options { get; }

        /// <summary>
        /// Serialize the <paramref name="value"/> to a JSON <see cref="string"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="format">Where specified overrides the serialization write formatting.</param>
        /// <returns>The JSON <see cref="string"/>.</returns>
        string Serialize<T>(T value, JsonWriteFormat? format = null);

        /// <summary>
        /// Serialize the <paramref name="value"/> to a JSON <see cref="BinaryData"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="format">Where specified overrides the serialization write formatting.</param>
        /// <returns>The JSON <see cref="BinaryData"/>.</returns>
        BinaryData SerializeToBinaryData<T>(T value, JsonWriteFormat? format = null);

        /// <summary>
        /// Deserialize the JSON <see cref="string"/> to an underlying JSON object.
        /// </summary>
        /// <param name="json">The JSON <see cref="string"/>.</param>
        /// <returns>The JSON object (as per the underlying implementation).</returns>
        object? Deserialize(string json);

        /// <summary>
        /// Deserialize the JSON <see cref="string"/> to the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="json">The JSON <see cref="string"/>.</param>
        /// <param name="type">The <see cref="Type"/> to convert to.</param>
        /// <returns>The corresponding typed value.</returns>
        object? Deserialize(string json, Type type);

        /// <summary>
        /// Deserialize the JSON <see cref="string"/> to the <see cref="Type"/> of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to convert to.</typeparam>
        /// <param name="json">The JSON <see cref="string"/>.</param>
        /// <returns>The corresponding typed value.</returns>
        T? Deserialize<T>(string json);

        /// <summary>
        /// Deserialize the JSON <see cref="BinaryData"/> to an underlying JSON object.
        /// </summary>
        /// <param name="json">The JSON <see cref="BinaryData"/>.</param>
        /// <returns>The JSON object (as per the underlying implementation).</returns>
        object? Deserialize(BinaryData json);

        /// <summary>
        /// Deserialize the JSON <see cref="BinaryData"/> to the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="json">The JSON <see cref="BinaryData"/>.</param>
        /// <param name="type">The <see cref="Type"/> to convert to.</param>
        /// <returns>The corresponding typed value.</returns>
        object? Deserialize(BinaryData json, Type type);

        /// <summary>
        /// Deserialize the JSON <see cref="BinaryData"/> to the <see cref="Type"/> of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to convert to.</typeparam>
        /// <param name="json">The JSON <see cref="BinaryData"/>.</param>
        /// <returns>The corresponding typed value.</returns>
        T? Deserialize<T>(BinaryData json);

        /// <summary>
        /// Trys to apply the JSON property <paramref name="filter"/> (using JSON <paramref name="names"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding JSON <paramref name="value"/> <see cref="string"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="comparison">The names <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        bool TryApplyFilter<T>(T value, IEnumerable<string>? names, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Trys to apply the JSON property <paramref name="filter"/> (using JSON <paramref name="names"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding <paramref name="value"/> JSON object (as per the underlying implementation) with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="comparison">The names <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        bool TryApplyFilter<T>(T value, IEnumerable<string>? names, out object json, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Trys and gets the corresponding JSON name for the <paramref name="memberInfo"/>.
        /// </summary>
        /// <param name="memberInfo">The <see cref="MemberInfo"/></param>
        /// <param name="jsonName">The JSON name where underlying JSON attribute is defined or not; <c>null</c> where not serializable.</param>
        /// <returns><c>true</c> indicates that the property is considered serializable; otherwise, <c>false</c>.</returns>
        bool TryGetJsonName(MemberInfo memberInfo, out string? jsonName);
    }
}