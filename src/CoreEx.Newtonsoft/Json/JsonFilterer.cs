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
        /// Trys to apply the JSON property <paramref name="filter"/> (using JSON <paramref name="names"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding JSON <paramref name="value"/> <see cref="string"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="settings">The optional <see cref="JsonSerializerSettings"/>.</param>
        /// <param name="comparison">The names <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? names, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerSettings? settings = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            var r = TryApply(value, names, out JToken node, filter, settings, comparison);
            json = node.ToString(Formatting.None);
            return r;
        }

        /// <summary>
        /// Trys to apply the JSON property <paramref name="filter"/> (using JSON <paramref name="names"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="json">The corresponding <paramref name="value"/> <see cref="JToken"/> with the filtering applied.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="settings">The optional <see cref="JsonSerializerSettings"/>.</param>
        /// <param name="comparison">The names <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? names, out JToken json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerSettings? settings = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            json = JToken.FromObject(value, Nsj.JsonSerializer.Create(settings));
            return Apply(json, names, filter, comparison);
        }

        /// <summary>
        /// Applies the inclusion and exclusion of properties (using JSON names) to an existing <see cref="JToken"/>.
        /// </summary>
        /// <param name="json">The <see cref="JToken"/> value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="comparison">The names <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
        /// <remarks><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</remarks>
        public static bool Apply(JToken json, IEnumerable<string>? names, JsonPropertyFilter filter = JsonPropertyFilter.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            var maxDepth = 0;
            var dict = Text.Json.JsonFilterer.CreateDictionary(names, filter, comparison, ref maxDepth);

            var filtered = false;
            if (maxDepth > 0)
                JsonFilter(json, null, dict, filter, 0, maxDepth, ref filtered, comparison);

            return filtered;
        }

        /// <summary>
        /// Filter the JSON nodes based on the includes/excludes.
        /// </summary>
        private static void JsonFilter(JToken json, string? path, Dictionary<string, bool> names, JsonPropertyFilter filter, int depth, int maxDepth, ref bool filtered, StringComparison comparison)
        {
            // Do not check beyond maximum depth as there is no further filtering required.
            if (depth > maxDepth)
                return;

            // Iterate through the properties within the object and filter accordingly.
            if (json is JObject jo)
            {
                foreach (var ji in jo.Properties().ToArray())
                {
                    string fp = path == null ? ji.Name : string.Concat(path, '.', ji.Name);
                    bool found = names.TryGetValue(fp, out var endOfTheLine);

                    if ((filter == JsonPropertyFilter.Include && !found) || (filter == JsonPropertyFilter.Exclude && found))
                    {
                        jo.Remove(ji.Name);
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
            else if (json is JArray ja)
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