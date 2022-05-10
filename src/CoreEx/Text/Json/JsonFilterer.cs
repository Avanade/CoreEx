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
        /// <param name="comparison">The names <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <param name="preFilterInspector">The <see cref="IJsonPreFilterInspector"/> action.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? names, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerOptions? options = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<IJsonPreFilterInspector>? preFilterInspector = null)
        {
            var r = TryApply(value, names, out JsonNode node, filter, options, comparison, preFilterInspector);
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
        /// <param name="comparison">The names <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <param name="preFilterInspector">The <see cref="IJsonPreFilterInspector"/> action.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? names, out JsonNode json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerOptions? options = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<IJsonPreFilterInspector>? preFilterInspector = null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            json =  System.Text.Json.JsonSerializer.SerializeToNode(value, options)!;
            preFilterInspector?.Invoke(new JsonPreFilterInspector(json));

            return Apply(json, names, filter, comparison);
        }

        /// <summary>
        /// Applies the inclusion and exclusion of properties (using JSON names) to an existing <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonNode"/> value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="comparison">The names <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <remarks><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</remarks>
        public static bool Apply(JsonNode json, IEnumerable<string>? names, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            var maxDepth = 0;
            var dict = CreateDictionary(names, filter, comparison, ref maxDepth);

            var filtered = false;
            if (maxDepth > 0)
                JsonFilter(json, null, dict, filter, 0, maxDepth, ref filtered, comparison);

            return filtered;
        }

        /// <summary>
        /// Create a <see cref="Dictionary{TKey, TValue}"/> from the <paramref name="list"/> and expands list where <paramref name="filter"/> is <see cref="JsonPropertyFilter.Include"/>.
        /// </summary>
        /// <param name="list">The list of JSON property names.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>.</param>
        /// <param name="comparison">The names <see cref="StringComparison"/>.</param>
        /// <param name="maxDepth">The maximum name hierarchy depth.</param>
        /// <returns>The <see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static Dictionary<string, bool> CreateDictionary(IEnumerable<string>? list, JsonPropertyFilter filter, StringComparison comparison, ref int maxDepth)
        {
            var dict = new Dictionary<string, bool>(StringComparer.FromComparison(comparison));
            list ??= Array.Empty<string>();
            list.ForEach(item => dict.TryAdd(item, true));

            if (filter == JsonPropertyFilter.Include)
            {
                var sb = new StringBuilder();
                foreach (var kvp in dict.ToArray())
                {
                    sb.Clear();
                    var parts = kvp.Key.Split('.');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                            sb.Append('.');

                        sb.Append(parts[i]);
                        dict.TryAdd(sb.ToString(), false);

                        maxDepth = Math.Max(maxDepth, i + 1);
                    }
                }

                foreach (var kvp in dict.ToArray())
                {
                    if (dict.Keys.Any(x => !x.Equals(kvp.Key, comparison) && x.StartsWith(kvp.Key, comparison)))
                        dict[kvp.Key] = false;
                }
            }
            else
                maxDepth = Math.Max(maxDepth, dict.Count == 0 ? 0 : dict.Max(x => x.Key.Count(c => c == '.') + 1));

            return dict;
        }

        /// <summary>
        /// Filter the JSON nodes based on the includes/excludes.
        /// </summary>
        private static void JsonFilter(JsonNode json, string? path, Dictionary<string, bool> names, JsonPropertyFilter filter, int depth, int maxDepth, ref bool filtered, StringComparison comparison)
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
                    bool found = names.TryGetValue(fp, out var endOfTheLine);

                    if ((filter == JsonPropertyFilter.Include && !found) || (filter == JsonPropertyFilter.Exclude && found))
                    {
                        jo.Remove(ji.Key);
                        filtered = true;
                        continue;
                    }

                    if (filter == JsonPropertyFilter.Include && found && endOfTheLine)
                        continue;

                    // Where there is a child value then continue navigation.
                    if (ji.Value != null)
                        JsonFilter(ji.Value, fp, names, filter, depth + 1, maxDepth, ref filtered, comparison);
                }
            }
            else if (json is JsonArray ja)
            {
                // Iterate and filter each item in the array.
                foreach (var ji in ja)
                {
                    if (ji != null)
                        JsonFilter(ji, path, names, filter, depth + 1, maxDepth, ref filtered, comparison);
                }
            }
        }
    }
}