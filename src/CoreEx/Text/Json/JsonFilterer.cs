// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides a means to apply a filter to include or exclude JSON properties (in effect removing the unwanted properties).
    /// </summary>
    public static partial class JsonFilterer
    {
#if NET8_0_OR_GREATER
        private static readonly Regex _regex = IndexesRegex();
#else
        private static readonly Regex _regex = new(@"\[(.*?)\]", RegexOptions.Compiled);
#endif

        /// <summary>
        /// Gets the standard JSON root path.
        /// </summary>
        public const string JsonRootPath = "$";

        /// <summary>
        /// Prepends the JSON <paramref name="path"/> with the <see cref="JsonRootPath"/> where not already present.
        /// </summary>
        /// <param name="path">The JSON path.</param>
        /// <returns>The resulting JSON path.</returns>
        public static string PrependRootPath(string path) => string.IsNullOrEmpty(path) ? JsonRootPath : (!path.StartsWith(JsonRootPath) ? (path.StartsWith('[') ? $"{JsonRootPath}{path}" : $"{JsonRootPath}.{path}") : path);

        /// <summary>
        /// Removes all indexes from the specified <paramref name="input"/> JSON path.
        /// </summary>
        /// <param name="input">The input JSON path.</param>
        /// <param name="path">The resulting JSON path.</param>
        /// <returns><c>true</c> indicates indexes were removed; otherwise, <c>false</c>.</returns>
        public static bool TryRemovePathIndexes(string input, out string path)
        {
            if (string.IsNullOrEmpty(input))
            {
                path = input;
                return false;
            }

            path = _regex.Replace(input, string.Empty);
            return path.Length != input.Length;
        }

        /// <summary>
        /// Trys to apply the JSON <paramref name="filter"/> (using JSON <paramref name="paths"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding JSON <paramref name="value"/> <see cref="string"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="options">The optional <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <param name="preFilterInspector">The <see cref="IJsonPreFilterInspector"/> action.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? paths, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerOptions? options = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<IJsonPreFilterInspector>? preFilterInspector = null)
        {
            var r = TryApply(value, paths, out JsonNode node, filter, options, comparison, preFilterInspector);
            json = node.ToJsonString(options);
            return r;
        }

        /// <summary>
        /// Trys to apply the JSON property <paramref name="filter"/> (using JSON <paramref name="paths"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding <paramref name="value"/> <see cref="JsonNode"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="options">The optional <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <param name="preFilterInspector">The <see cref="IJsonPreFilterInspector"/> action.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? paths, out JsonNode json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerOptions? options = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<IJsonPreFilterInspector>? preFilterInspector = null)
        {
            json =  System.Text.Json.JsonSerializer.SerializeToNode(value.ThrowIfNull(), options)!;
            preFilterInspector?.Invoke(new JsonPreFilterInspector(json));

            return Apply(json, paths, filter, comparison);
        }

        /// <summary>
        /// Applies the inclusion and exclusion of JSON paths to a specified <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonNode"/> value.</param>
        /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <remarks><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</remarks>
        public static bool Apply(JsonNode json, IEnumerable<string>? paths, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            var maxDepth = 0;
            var dict = CreateDictionary(paths, filter, comparison, ref maxDepth, true);

            var filtered = false;
            if (maxDepth > 0)
                JsonFilter(json, dict, filter, 0, maxDepth, ref filtered, comparison);

            return filtered;
        }

        /// <summary>
        /// Create a <see cref="Dictionary{TKey, TValue}"/> from the <paramref name="paths"/> and expands list with intermediary paths where <paramref name="filter"/> is <see cref="JsonPropertyFilter.Include"/>.
        /// </summary>
        /// <param name="paths">The list of JSON paths.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>.</param>
        /// <param name="comparison">The paths <see cref="StringComparison"/>.</param>
        /// <param name="maxDepth">The maximum hierarchy depth for all specified .</param>
        /// <returns>The <see cref="Dictionary{TKey, TValue}"/>.</returns>
        /// <remarks>Where the <see cref="bool"/> is <c>true</c> this indicates the specified path; versus, <c>false</c> that indicates an intermediary path.</remarks>
        public static Dictionary<string, bool> CreateDictionary(IEnumerable<string>? paths, JsonPropertyFilter filter, StringComparison comparison, ref int maxDepth)
            => CreateDictionary(paths, filter, comparison, ref maxDepth, false);

        /// <summary>
        /// Create a <see cref="Dictionary{TKey, TValue}"/> from the <paramref name="paths"/> and expands list with intermediary paths where <paramref name="filter"/> is <see cref="JsonPropertyFilter.Include"/>.
        /// </summary>
        /// <param name="paths">The list of JSON paths.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>.</param>
        /// <param name="comparison">The paths <see cref="StringComparison"/>.</param>
        /// <param name="maxDepth">The maximum hierarchy depth for all specified .</param>
        /// <param name="prependRootPath">Indicates whether to prepend the <see cref="JsonRootPath"/> to each path.</param>
        /// <returns>The <see cref="Dictionary{TKey, TValue}"/>.</returns>
        /// <remarks>Where the <see cref="bool"/> is <c>true</c> this indicates the specified path; versus, <c>false</c> that indicates an intermediary path.</remarks>
        public static Dictionary<string, bool> CreateDictionary(IEnumerable<string>? paths, JsonPropertyFilter filter, StringComparison comparison, ref int maxDepth, bool prependRootPath)
        {
            var dict = new Dictionary<string, bool>(StringComparer.FromComparison(comparison));
            paths ??= Array.Empty<string>();

            // Add each 'specified' path.
            paths.ForEach(path => dict.TryAdd(prependRootPath ? PrependRootPath(path) : path, true));

            // Add each 'intermediary' path where applicable.
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

                    if (TryRemovePathIndexes(kvp.Key, out var indexless))
                    {
                        sb.Clear();
                        parts = indexless.Split('.');
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (i > 0)
                                sb.Append('.');

                            sb.Append(parts[i]);
                            dict.TryAdd(sb.ToString(), false);
                        }
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
        private static bool JsonFilter(JsonNode json, Dictionary<string, bool> paths, JsonPropertyFilter filter, int depth, int maxDepth, ref bool filtered, StringComparison comparison)
        {
            // Do not check beyond maximum depth as there is no further filtering required.
            if (depth > maxDepth)
                return false;

            // Iterate through the properties within the object and filter accordingly.
            if (json is JsonObject jo)
            {
                foreach (var jn in jo.ToArray())
                {
                    var path = jn.Value is null ? $"{jo.GetPath()}.{jn.Key}" : jn.Value.GetPath();
                    bool found = paths.TryGetValue(path, out var isSpecifiedPath);
                    if (!found && TryRemovePathIndexes(path, out var pathWithoutIndexes))
                        found = paths.TryGetValue(pathWithoutIndexes, out isSpecifiedPath);

                    if ((filter == JsonPropertyFilter.Include && !found) || (filter == JsonPropertyFilter.Exclude && found))
                    {
                        jo.Remove(jn.Key);
                        filtered = true;
                        continue;
                    }

                    if (filter == JsonPropertyFilter.Include && found && isSpecifiedPath)
                        continue;

                    // Where there is a child value then continue navigation.
                    if (jn.Value != null)
                        JsonFilter(jn.Value, paths, filter, depth + 1, maxDepth, ref filtered, comparison);
                }
            }
            else if (json is JsonArray ja)
            {
                // Iterate and filter each item in the array.
                for (var i = ja.Count - 1; i >= 0; i--)
                {
                    var jn = ja[i];
                    if (jn != null)
                    {
                        if (JsonFilter(jn, paths, filter, depth, maxDepth, ref filtered, comparison))
                        {
                            ja.RemoveAt(i);
                            filtered = true;
                        }
                    }
                }
            }
            else if (json is JsonValue)
            {
                var path = json.GetPath();
                if (!paths.TryGetValue(path, out var isSpecifiedPath) && TryRemovePathIndexes(path, out var pathWithoutIndexes))
                    paths.TryGetValue(pathWithoutIndexes, out isSpecifiedPath);

                return filter == JsonPropertyFilter.Include ? !isSpecifiedPath : isSpecifiedPath;
            }

            return false;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Provides the generated <see cref="Regex"/> for <see cref="TryRemovePathIndexes"/>.
        /// </summary>
        [GeneratedRegex(@"\[(.*?)\]", RegexOptions.Compiled)]
        private static partial Regex IndexesRegex();
#endif
    }
}