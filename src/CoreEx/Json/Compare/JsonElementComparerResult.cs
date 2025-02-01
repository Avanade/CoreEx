// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CoreEx.Json.Compare
{
    /// <summary>
    /// Represents the result of a <see cref="JsonElementComparer"/>.
    /// </summary>
    public sealed class JsonElementComparerResult
    {
        private List<JsonElementDifference>? _differences;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonElementComparerResult"/> class.
        /// </summary>
        /// <param name="left">The left <see cref="JsonElement"/>.</param>
        /// <param name="right">The right <see cref="JsonElement"/>.</param>
        /// <param name="maxDifferences">The maximum number of differences to detect.</param>
        /// <param name="replaceAllArrayItemsOnMerge">Indicates whether to always replace all array items where at least one item has changed when performing a corresponding <see cref="ToMergePatch(string[])"/>.</param>
        internal JsonElementComparerResult(JsonElement left, JsonElement right, int maxDifferences, bool replaceAllArrayItemsOnMerge = true)
        {
            Left = left;
            Right = right;
            MaxDifferences = maxDifferences;
            ReplaceAllArrayItemsOnMerge = replaceAllArrayItemsOnMerge;
        }

        /// <summary>
        /// Gets the left <see cref="JsonElement"/>.
        /// </summary>
        public JsonElement Left { get; }

        /// <summary>
        /// Gets the right <see cref="JsonElement"/>.
        /// </summary>
        public JsonElement Right { get; }

        /// <summary>
        /// Gets the maximum number of differences to detect.
        /// </summary>
        public int MaxDifferences { get; }

        /// <summary>
        /// Indicates whether the two JSON elements are considered equal.
        /// </summary>
        public bool AreEqual => DifferenceCount == 0;

        /// <summary>
        /// Indicates whether there are any differences between the two JSON elements based on the specified criteria.
        /// </summary>
        public bool HasDifferences => DifferenceCount != 0;

        /// <summary>
        /// Gets the current number of differences detected.
        /// </summary>
        public int DifferenceCount => _differences?.Count ?? 0;

        /// <summary>
        /// Indicates whether the maximum number of differences specified to detect has been found.
        /// </summary>
        public bool IsMaxDifferencesFound => DifferenceCount >= MaxDifferences;

        /// <summary>
        /// Indicates whether to always replace all array items where at least one item has changed when performing a corresponding <see cref="ToMergePatch(string[])"/>.
        /// </summary>
        /// <remarks>The formal specification <see href="https://tools.ietf.org/html/rfc7396"/> explictly states that an <see cref="System.Text.Json.JsonValueKind.Array"/> is to be a replacement operation.
        /// <para>Where set to <c>false</c> and there is an array length difference this will always result in a replace (i.e. all); no means to reliably determine what has been added, deleted, modified, resequenced, etc.</para></remarks>
        public bool ReplaceAllArrayItemsOnMerge { get; }

        /// <summary>
        /// Gets the <see cref="JsonElementDifference"/> array.
        /// </summary>
        /// <remarks>The differences found up to the <see cref="MaxDifferences"/> specified.</remarks>
        public JsonElementDifference[] GetDifferences() => _differences is null ? [] : [.. _differences];

        /// <summary>
        /// Adds a <see cref="JsonElementDifference"/>.
        /// </summary>
        /// <param name="difference">The <see cref="JsonElementDifference"/>.</param>
        internal void AddDifference(JsonElementDifference difference) => (_differences ??= []).Add(difference);

        /// <inheritdoc/>
        public override string ToString()
        {
            if (AreEqual)
                return "No differences detected.";

            var sb = new StringBuilder();
            foreach (var d in _differences!)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.Append(d.ToString());
            }

            if (IsMaxDifferencesFound)
            {
                sb.AppendLine();
                sb.Append($"Maximum difference count of '{MaxDifferences}' found; comparison stopped.");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates a JSON Merge Patch (<c>application/merge-patch+json</c>) <see cref="JsonNode"/> from the <see cref="Right"/> <see cref="JsonElement"/> (used within the comparison) based on the differences (<see cref="GetDifferences"/>) 
        /// found and the optional <paramref name="pathsToInclude"/>.
        /// </summary>
        /// <param name="pathsToInclude">Optional list of paths to additionally include. Qualified paths, that include indexing, are also supported.</param>
        /// <returns>A JSON Merge Patch (<c>application/merge-patch+json</c>) <see cref="JsonNode"/> originally sourced as the <see cref="Right"/> <see cref="JsonElement"/>.</returns>
        /// <remarks>The <paramref name="pathsToInclude"/> enables additional paths to always be included regardless of whether any differences were found for those paths; i.e those paths will always be included. Additionally,
        /// to further include or exclude consider the <see cref="JsonFilterer"/> which is conveniently enabled as <see cref="JsonNode"/> extensions methods <see cref="JsonExtensions.ApplyInclude(JsonNode, string[])"/> and
        /// <see cref="JsonExtensions.ApplyExclude(JsonNode, string[])"/>.</remarks>
        public JsonNode ToMergePatch(params string[] pathsToInclude)
        {
            var maxDepth = 0;
            var state = new MergePatchState(
                _differences is null ? null : JsonFilterer.CreateDictionary(_differences.Select(x => x.Path), JsonPropertyFilter.Include, StringComparison.Ordinal, ref maxDepth, true),
                pathsToInclude.Length == 0 ? null : JsonFilterer.CreateDictionary(pathsToInclude, JsonPropertyFilter.Include, StringComparison.Ordinal, ref maxDepth, true));

            switch (Right.ValueKind)
            {
                case JsonValueKind.Object:
                    var jo = JsonObject.Create(Right)!;
                    MergePatch(jo, state);
                    return jo;

                case JsonValueKind.Array:
                    var ja = JsonArray.Create(Right)!;
                    MergePatch(ja, state);
                    return ja;

                case JsonValueKind.Undefined:
                    throw new InvalidOperationException("Cannot create a JSON Merge Patch from an undefined JSON element.");

                default:
                    return JsonValue.Create(Right)!;
            }
        }

        /// <summary>
        /// Merge patch based on node type.
        /// </summary>
        private PathMatch MergePatch(JsonNode? jn, MergePatchState state)
        {
            if (jn == null)
                return PathMatch.None;

            var match = state.GetMatch(jn);
            if (match != PathMatch.Partial)
                return match;

            return jn switch
            {
                JsonValue => PathMatch.Partial,
                JsonObject jo => MergePatch(jo, state),
                JsonArray ja => MergePatch(ja, state),
                _ => throw new NotSupportedException($"Unsupported JSON node type '{jn.GetType().Name}'.")
            };
        }

        /// <summary>
        /// Merge patch an object.
        /// </summary>
        private PathMatch MergePatch(JsonObject jo, MergePatchState state)
        {
            PathMatch match;
            var overall = PathMatch.None;

            foreach (var jn in jo.ToArray())
            {
                if (jn.Value is null)
                {
                    var path = $"{jo.GetPath()}.{jn.Key}";
                    match = state.GetMatch(path);
                }
                else
                    match = MergePatch(jn.Value, state);

                overall = MergePatchState.ConsolidateMatch(overall, match);
                if (match != PathMatch.Full)
                    jo.Remove(jn.Key);
            }

            return overall;
        }

        /// <summary>
        /// Merge patch an array.
        /// </summary>
        private PathMatch MergePatch(JsonArray ja, MergePatchState state)
        {
            PathMatch match;
            var overall = PathMatch.None;

            if (ReplaceAllArrayItemsOnMerge && state.GetMatch(ja) != PathMatch.None)
                return PathMatch.Full;

            for (var i = ja.Count - 1; i >= 0; i--)
            {
                var jn = ja[i];
                match = jn is null ? state.GetMatch($"{ja.GetPath()}[{i}]") : MergePatch(ja[i], state);
                overall = MergePatchState.ConsolidateMatch(overall, match);
                if (match != PathMatch.Full)
                    ja.RemoveAt(i);
            }

            return overall;
        }

        /// <summary>
        /// Provides internal state needed to support the merge patch.
        /// </summary>
        /// <param name="differencePaths">The differences paths for inclusion.</param>
        /// <param name="includePaths">The additional paths for inclusion.</param>
        private sealed class MergePatchState(Dictionary<string, bool>? differencePaths, Dictionary<string, bool>? includePaths)
        {
            /// <summary>
            /// Gets the differences paths for inclusion.
            /// </summary>
            public Dictionary<string, bool>? DifferencePaths { get; } = differencePaths;

            /// <summary>
            /// Gets the additional paths for inclusion.
            /// </summary>
            public Dictionary<string, bool>? IncludePaths { get; } = includePaths;

            /// <summary>
            /// Gets the difference and include match for the specified <see cref="JsonNode"/>.
            /// </summary>
            public PathMatch GetMatch(JsonNode jsonNode) => GetMatch(jsonNode.GetPath());

            /// <summary>
            /// Gets the difference and include match for the specified path.
            /// </summary>
            public PathMatch GetMatch(string path)
            {
                var match = GetMatchAsIs(path);
                return match != PathMatch.Full && JsonFilterer.TryRemovePathIndexes(path, out var unindexed) ? ConsolidateMatch(GetMatchAsIs(unindexed), match) : match;
            }

            /// <summary>
            /// Gets the difference and include match for the specified path as-is.
            /// </summary>
            private PathMatch GetMatchAsIs(string path)
            {
                var dm = GetDifferenceMatch(path);
                if (dm == PathMatch.Full)
                    return PathMatch.Full;

                var im = GetIncludeMatch(path);
                if (im == PathMatch.Full)
                    return PathMatch.Full;
                else if (dm == PathMatch.Partial || im == PathMatch.Partial)
                    return PathMatch.Partial;
                else
                    return PathMatch.None;
            }

            /// <summary>
            /// Gets the difference match for the specified path.
            /// </summary>
            public PathMatch GetDifferenceMatch(string path) => (DifferencePaths is not null && DifferencePaths.TryGetValue(path, out var match)) ? (match ? PathMatch.Full : PathMatch.Partial) : PathMatch.None;

            /// <summary>
            /// Gets the include match for the specified path.
            /// </summary>
            public PathMatch GetIncludeMatch(string path) => (IncludePaths is not null && IncludePaths.TryGetValue(path, out var match)) ? (match ? PathMatch.Full : PathMatch.Partial) : PathMatch.None;

            /// <summary>
            /// Consolidate the existing and matched and return the mostest (sic) match.
            /// </summary>
            public static PathMatch ConsolidateMatch(PathMatch existing, PathMatch matched)
                => existing == PathMatch.Full || matched == PathMatch.Full ? PathMatch.Full : (existing == PathMatch.Partial || matched == PathMatch.Partial ? PathMatch.Partial : PathMatch.None);
        }

        /// <summary>
        /// Represents the result of path matching.
        /// </summary>
        private enum PathMatch
        {
            /// <summary>
            /// Indicates no match.
            /// </summary>
            None,

            /// <summary>
            /// Indicates a partial match.
            /// </summary>
            Partial,

            /// <summary>
            /// Indicates a full match.
            /// </summary>
            Full
        }
    }
}