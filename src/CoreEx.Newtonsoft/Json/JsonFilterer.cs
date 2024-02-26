// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Nsj = Newtonsoft.Json;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Provides a means to apply a filter to include or exclude JSON properties (in effect removing the unwanted properties).
    /// </summary>
    public static class JsonFilterer
    {
        /// <summary>
        /// Trys to apply the JSON <paramref name="filter"/> (using JSON <paramref name="paths"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding JSON <paramref name="value"/> <see cref="string"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="settings">The optional <see cref="JsonSerializerSettings"/>.</param>
        /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <param name="preFilterInspector">The <see cref="IJsonPreFilterInspector"/> action.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? paths, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerSettings? settings = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<IJsonPreFilterInspector>? preFilterInspector = null)
        {
            var r = TryApply(value, paths, out JToken node, filter, settings, comparison, preFilterInspector);
            json = node.ToString(Formatting.None);
            return r;
        }

        /// <summary>
        /// Trys to apply the JSON <paramref name="filter"/> (using JSON <paramref name="paths"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding <paramref name="value"/> <see cref="JToken"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="settings">The optional <see cref="JsonSerializerSettings"/>.</param>
        /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <param name="preFilterInspector">The <see cref="IJsonPreFilterInspector"/> action.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? paths, out JToken json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerSettings? settings = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase, Action<IJsonPreFilterInspector>? preFilterInspector = null)
        {
            value.ThrowIfNull(nameof(value));
            json = JToken.FromObject(value, Nsj.JsonSerializer.Create(settings));
            preFilterInspector?.Invoke(new JsonPreFilterInspector(json));
            return Apply(json, paths, filter, comparison);
        }

        /// <summary>
        /// Applies the inclusion and exclusion of the JSON paths to a specified <see cref="JToken"/>.
        /// </summary>
        /// <param name="json">The <see cref="JToken"/> value.</param>
        /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <remarks><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</remarks>
        public static bool Apply(JToken json, IEnumerable<string>? paths, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            var maxDepth = 0;
            var dict = Text.Json.JsonFilterer.CreateDictionary(paths, filter, comparison, ref maxDepth, true);

            var filtered = false;
            if (maxDepth > 0)
                JsonFilter(json, dict, filter, 0, maxDepth, ref filtered, comparison);

            return filtered;
        }

        /// <summary>
        /// Filter the JSON nodes based on the includes/excludes.
        /// </summary>
        private static bool JsonFilter(JToken json, Dictionary<string, bool> paths, JsonPropertyFilter filter, int depth, int maxDepth, ref bool filtered, StringComparison comparison)
        {
            // Do not check beyond maximum depth as there is no further filtering required.
            if (depth > maxDepth)
                return false;

            // Iterate through the properties within the object and filter accordingly.
            if (json is JObject jo)
            {
                foreach (var jn in jo.Properties().ToArray())
                {
                    string path = Text.Json.JsonFilterer.PrependRootPath(jn.Path);
                    bool found = paths.TryGetValue(path, out var isSpecifiedPath);
                    if (!found && Text.Json.JsonFilterer.TryRemovePathIndexes(path, out var pathWithoutIndexes))
                        found = paths.TryGetValue(pathWithoutIndexes, out isSpecifiedPath);

                    if ((filter == JsonPropertyFilter.Include && !found) || (filter == JsonPropertyFilter.Exclude && found))
                    {
                        jo.Remove(jn.Name);
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
            else if (json is JArray ja)
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
            else if (json is JValue)
            {
                var path = Text.Json.JsonFilterer.PrependRootPath(json.Path);
                if (!paths.TryGetValue(path, out var isSpecifiedPath) && Text.Json.JsonFilterer.TryRemovePathIndexes(path, out var pathWithoutIndexes))
                    paths.TryGetValue(pathWithoutIndexes, out isSpecifiedPath);

                return filter == JsonPropertyFilter.Include ? !isSpecifiedPath : isSpecifiedPath;
            }

            return false;
        }
    }
}