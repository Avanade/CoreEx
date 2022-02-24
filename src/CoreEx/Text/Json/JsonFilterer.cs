// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides a means to apply a filter to include or exclude JSON properties (in effect removing the unwanted properties).
    /// </summary>
    public static class JsonFilterer
    {
        /// <summary>
        /// Trys to apply the JSON property <paramref name="filter"/> (using JSON <paramref name="names"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding JSON <paramref name="value"/> <see cref="string"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="options">The optional <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="comparer">The names <see cref="IEqualityComparer{T}"/>; defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? names, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerOptions? options = null, IEqualityComparer<string>? comparer = null)
        {
            var r = TryApply(value, names, out JsonNode node, filter, options, comparer);
            json = node.ToJsonString();
            return r;
        }

        /// <summary>
        /// Trys to apply the JSON property <paramref name="filter"/> (using JSON <paramref name="names"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding <paramref name="value"/> <see cref="JsonNode"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="options">The optional <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="comparer">The names <see cref="IEqualityComparer{T}"/>; defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? names, out JsonNode json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerOptions? options = null, IEqualityComparer<string>? comparer = null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            json =  System.Text.Json.JsonSerializer.SerializeToNode(value, options)!;
            return Apply(json, names, filter, comparer);
        }

        /// <summary>
        /// Applies the inclusion and exclusion of properties (using JSON names) to an existing <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonNode"/> value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="comparer">The names <see cref="IEqualityComparer{T}"/>; defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
        /// <remarks><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</remarks>
        public static bool Apply(JsonNode json, IEnumerable<string>? names, JsonPropertyFilter filter = JsonPropertyFilter.Include, IEqualityComparer<string>? comparer = null)
        {
            var maxDepth = 0;
            var hs = CreateHashSet(names, filter, ref maxDepth);
            comparer ??= StringComparer.OrdinalIgnoreCase;

            var filtered = false;
            if (maxDepth > 0)
                JsonFilter(json, null, hs, filter, 0, maxDepth, ref filtered, comparer);

            return filtered;
        }

        /// <summary>
        /// Create a <see cref="HashSet{T}"/> from the <paramref name="list"/> and expands list where <paramref name="filter"/> is <see cref="JsonPropertyFilter.Include"/>.
        /// </summary>
        /// <param name="list">The list of JSON property names.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>.</param>
        /// <param name="maxDepth">The maximum name hierarchy depth.</param>
        /// <returns>The <see cref="HashSet{T}"/>.</returns>
        public static HashSet<string> CreateHashSet(IEnumerable<string>? list, JsonPropertyFilter filter, ref int maxDepth)
        {
            var hs = new HashSet<string>(list == null ? Array.Empty<string>() : list.Where(x => !string.IsNullOrEmpty(x)));
            var sb = new StringBuilder();

            if (filter == JsonPropertyFilter.Include)
            {
                foreach (var item in hs.ToArray())
                {
                    sb.Clear();
                    var parts = item.Split('.');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                            sb.Append('.');

                        sb.Append(parts[i]);
                        hs.Add(sb.ToString());

                        maxDepth = Math.Max(maxDepth, i + 1);
                    }
                }
            }
            else
                maxDepth = Math.Max(maxDepth, hs.Count == 0 ? 0 : hs.Max(x => x.Count(c => c == '.') + 1));

            return hs;
        }

        /// <summary>
        /// Filter the JSON nodes based on the includes/excludes.
        /// </summary>
        private static void JsonFilter(JsonNode json, string? path, HashSet<string> names, JsonPropertyFilter filter, int depth, int maxDepth, ref bool filtered, IEqualityComparer<string> comparer)
        {
            // Do not check beyond maximum depth as there is no further filtering required.
            if (depth > maxDepth)
                return;

            // Iterate through the properties within the object and filter accordingly.
            if (json is JsonObject jo)
            {
                foreach (var ji in jo.ToArray())
                {
                    string fp = path == null ? ji.Key : string.Concat(path, '.', ji.Key);
                    if ((filter == JsonPropertyFilter.Include && !names.Contains(fp, comparer)) || (filter == JsonPropertyFilter.Exclude && names.Contains(fp, comparer)))
                    {
                        jo.Remove(ji.Key);
                        filtered = true;
                        continue;
                    }

                    // Where there is a child value then continue navigation.
                    if (ji.Value != null)
                        JsonFilter(ji.Value, fp, names, filter, depth + 1, maxDepth, ref filtered, comparer);
                }
            }
            else if (json is JsonArray ja)
            {
                // Iterate and filter each item in the array.
                foreach (var ji in ja)
                {
                    if (ji != null)
                        JsonFilter(ji, path, names, filter, depth + 1, maxDepth, ref filtered, comparer);
                }
            }
        }
    }
}