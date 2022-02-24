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
        /// <param name="comparer">The names <see cref="IEqualityComparer{T}"/>; defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? names, out string json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerSettings? settings = null, IEqualityComparer<string>? comparer = null)
        {
            var r = TryApply(value, names, out JToken node, filter, settings, comparer);
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
        /// <param name="comparer">The names <see cref="IEqualityComparer{T}"/>; defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
        /// <returns><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</returns>
        public static bool TryApply<T>(T value, IEnumerable<string>? names, out JToken json, JsonPropertyFilter filter = JsonPropertyFilter.Include, JsonSerializerSettings? settings = null, IEqualityComparer<string>? comparer = null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            json = JToken.FromObject(value, Nsj.JsonSerializer.Create(settings));
            return Apply(json, names, filter, comparer);
        }

        /// <summary>
        /// Applies the inclusion and exclusion of properties (using JSON names) to an existing <see cref="JToken"/>.
        /// </summary>
        /// <param name="json">The <see cref="JToken"/> value.</param>
        /// <param name="names">The list of JSON property names to <paramref name="filter"/>.</param>
        /// <param name="filter">The <see cref="JsonPropertyFilter"/>; defaults to <see cref="JsonPropertyFilter.Include"/>.</param>
        /// <param name="comparer">The names <see cref="IEqualityComparer{T}"/>; defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
        /// <remarks><c>true</c> indicates that at least one JSON node was filtered (removed); otherwise, <c>false</c> for no changes.</remarks>
        public static bool Apply(JToken json, IEnumerable<string>? names, JsonPropertyFilter filter = JsonPropertyFilter.Include, IEqualityComparer<string>? comparer = null)
        {
            var maxDepth = 0;
            var hs = Text.Json.JsonFilterer.CreateHashSet(names, filter, ref maxDepth);
            comparer ??= StringComparer.OrdinalIgnoreCase;

            var filtered = false;
            if (maxDepth > 0)
                JsonFilter(json, null, hs, filter, 0, maxDepth, ref filtered, comparer);

            return filtered;
        }

        /// <summary>
        /// Filter the JSON nodes based on the includes/excludes.
        /// </summary>
        private static void JsonFilter(JToken json, string? path, HashSet<string> names, JsonPropertyFilter filter, int depth, int maxDepth, ref bool filtered, IEqualityComparer<string> comparer)
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
                    if ((filter == JsonPropertyFilter.Include && !names.Contains(fp, comparer)) || (filter == JsonPropertyFilter.Exclude && names.Contains(fp, comparer)))
                    {
                        jo.Remove(ji.Name);
                        filtered = true;
                        continue;
                    }

                    // Where there is a child value then continue navigation.
                    if (ji.Value != null)
                        JsonFilter(ji.Value, fp, names, filter, depth + 1, maxDepth, ref filtered, comparer);
                }
            }
            else if (json is JArray ja)
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