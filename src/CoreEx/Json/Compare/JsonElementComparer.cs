﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CoreEx.Json.Compare
{
    /// <summary>
    /// Provides a <see cref="JsonElement"/> comparer where property order is not significant.
    /// </summary>
    /// <remarks>Influenced by <see href="https://stackoverflow.com/questions/60580743/what-is-equivalent-in-jtoken-deepequals-in-system-text-json"/>.</remarks>
    public sealed class JsonElementComparer : IEqualityComparer<JsonElement>, IEqualityComparer<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonElementComparer"/> class.
        /// </summary>
        /// <param name="options">The <see cref="JsonElementComparerOptions"/>; defaults to <see cref="JsonElementComparerOptions.Default"/>.</param>
        public JsonElementComparer(JsonElementComparerOptions? options = null) => Options = options ?? JsonElementComparerOptions.Default;

        /// <summary>
        /// Gets the <see cref="JsonElementComparerOptions"/>.
        /// </summary>
        public JsonElementComparerOptions Options { get; }

        /// <summary>
        /// Compare two object values for equality; each value is JSON-serialized (uses <see cref="JsonSerializer.Default"/>) and then compared.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <param name="pathsToIgnore">Optional list of paths to exclude from the comparison. Qualified paths, that include indexing, are also supported.</param>
        /// <returns>The <see cref="JsonElementComparerResult"/>.</returns>
        public JsonElementComparerResult CompareValue<TLeft, TRight>(TLeft left, TRight right, params string[] pathsToIgnore)
            =>  Compare(SerializeValue(Options.JsonSerializer, left, nameof(left)), SerializeValue(Options.JsonSerializer, right, nameof(right)), pathsToIgnore);

        /// <summary>
        /// Serialize the value to JSON.
        /// </summary>
        private static string SerializeValue<T>(IJsonSerializer jsonSerializer, T value, string name)
        {
            if (value is JsonElement je)
                return je.ToString();

            try
            {
                return jsonSerializer.Serialize(value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to serialize value '{value?.GetType().FullName ?? "null"}' to JSON.", name, ex);
            }
        }

        /// <summary>
        /// Compare two JSON strings for equality.
        /// </summary>
        /// <param name="left">The left JSON <see cref="string"/>.</param>
        /// <param name="right">The right JSON <see cref="string"/>.</param>
        /// <param name="pathsToIgnore">Optional list of paths to exclude from the comparison. Qualified paths, that include indexing, are also supported.</param>
        /// <returns>The <see cref="JsonElementComparerResult"/>.</returns>
        public JsonElementComparerResult Compare(string left, string right, params string[] pathsToIgnore)
        {
            var ljr = new Utf8JsonReader(new BinaryData(left));
            if (!JsonElement.TryParseValue(ref ljr, out JsonElement? lje))
                throw new ArgumentException("JSON is not considered valid.", nameof(left));

            var rjr = new Utf8JsonReader(new BinaryData(right));
            if (!JsonElement.TryParseValue(ref rjr, out JsonElement? rje))
                throw new ArgumentException("JSON is not considered valid.", nameof(right));

            return Compare(lje.Value, rje.Value, pathsToIgnore);
        }

        /// <summary>
        /// Compare two <see cref="JsonNode"/> values for equality.
        /// </summary>
        /// <param name="left">The left <see cref="JsonNode"/>.</param>
        /// <param name="right">The right <see cref="JsonNode"/>.</param>
        /// <param name="pathsToIgnore">Optional list of paths to exclude from the comparison. Qualified paths, that include indexing, are also supported.</param>
        /// <returns>The <see cref="JsonElementComparerResult"/>.</returns>
        /// <remarks><i>Note</i>: this immediately converts each <see cref="JsonNode"/> into a corresponding <see cref="JsonElement"/> to perform the comparison, as such there may be a small performance cost.</remarks>
        public JsonElementComparerResult Compare(JsonNode left, JsonNode right, params string[] pathsToIgnore)
            => Compare(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(left), System.Text.Json.JsonSerializer.Deserialize<JsonElement>(right), pathsToIgnore);

        /// <summary>
        /// Compare two <see cref="JsonElement"/> values for equality.
        /// </summary>
        /// <param name="left">The left <see cref="JsonElement"/>.</param>
        /// <param name="right">The right <see cref="JsonElement"/>.</param>
        /// <param name="pathsToIgnore">Optional list of paths to exclude from the comparison. Qualified paths, that include indexing, are also supported.</param>
        /// <returns>The <see cref="JsonElementComparerResult"/>.</returns>
        public JsonElementComparerResult Compare(JsonElement left, JsonElement right, params string[] pathsToIgnore)
        {
            var result = new JsonElementComparerResult(left, right, Options.MaxDifferences, Options.AlwaysReplaceAllArrayItems);
            Compare(left, right, new CompareState(result, Options.PathComparer, pathsToIgnore));
            return result;
        }

        /// <summary>
        /// Perform the <see cref="JsonElement"/> comparison.
        /// </summary>
        private void Compare(JsonElement left, JsonElement right, CompareState state)
        {
            if (left.ValueKind != right.ValueKind)
            {
                state.AddDifference(left, right, JsonElementDifferenceType.Kind);
                return;
            }

            switch (left.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    // These are the same by kind, so carry on!
                    break;

                case JsonValueKind.String:
                    switch (Options.ValueComparison)
                    {
                        case JsonElementComparison.Exact:
                            if (left.GetRawText() != right.GetRawText())
                                state.AddDifference(left, right, JsonElementDifferenceType.Value);

                            break;

                        default:
                            if (left.GetRawText() == right.GetRawText())
                                break;

                            if (left.TryGetDateTimeOffset(out var ldto) && right.TryGetDateTimeOffset(out var rdto))
                            {
                                if (ldto != rdto)
                                    state.AddDifference(left, right, JsonElementDifferenceType.Value);
                            }
                            else if (left.TryGetDateTime(out var ldt) && right.TryGetDateTime(out var rdt))
                            {
                                if (ldt != rdt)
                                    state.AddDifference(left, right, JsonElementDifferenceType.Value);
                            }
                            else if (left.TryGetGuid(out var lg) && right.TryGetGuid(out var rg))
                            {
                                if (lg != rg)
                                    state.AddDifference(left, right, JsonElementDifferenceType.Value);
                            }
                            else if (left.GetString() != right.GetString())
                                state.AddDifference(left, right, JsonElementDifferenceType.Value);

                            break;
                    }

                    break;

                case JsonValueKind.Number:
                    switch (Options.ValueComparison)
                    {
                        case JsonElementComparison.Exact:
                            if (left.GetRawText() != right.GetRawText())
                                state.AddDifference(left, right, JsonElementDifferenceType.Value);

                            break;

                        default:
                            if (left.GetRawText() == right.GetRawText())
                                break;

                            if (left.TryGetDecimal(out var ldec) && right.TryGetDecimal(out var rdec))
                            {
                                if (ldec != rdec)
                                    state.AddDifference(left, right, JsonElementDifferenceType.Value);
                            }
                            else if (left.TryGetDouble(out var ldbl) && right.TryGetDouble(out var rdbl))
                            {
                                if (ldbl != rdbl)
                                    state.AddDifference(left, right, JsonElementDifferenceType.Value);
                            }
                            else
                                state.AddDifference(left, right, JsonElementDifferenceType.Value);

                            break;
                    }

                    break;

                case JsonValueKind.Object:
                    var lprops = left.EnumerateObject().ToList();
                    var rprops = right.EnumerateObject().ToList();

                    foreach (var l in lprops)
                    {
                        state.Compare(l.Name, () =>
                        {
                            if (right.TryGetProperty(l.Name, out var r))
                                Compare(l.Value, r, state);
                            else
                                state.AddDifference(left, right, JsonElementDifferenceType.RightNone);
                        });

                        if (state.MaxDifferencesFound)
                            break;
                    }

                    foreach (var r in rprops)
                    {
                        state.Compare(r.Name, () =>
                        {
                            if (!left.TryGetProperty(r.Name, out _))
                                state.AddDifference(left, right, JsonElementDifferenceType.LeftNone);
                        });

                        if (state.MaxDifferencesFound)
                            break;
                    }

                    break;

                case JsonValueKind.Array:
                    if (left.GetArrayLength() != right.GetArrayLength())
                    {
                        state.AddDifference(left, right, JsonElementDifferenceType.ArrayLength);
                        break;
                    }

                    var i = 0;
                    var rja = right.EnumerateArray();
                    foreach (var lje in left.EnumerateArray())
                    {
                        rja.MoveNext();
                        state.Compare(i++, () => Compare(lje, rja.Current, state));
                        if (state.MaxDifferencesFound)
                            break;
                    }

                    break;

                case JsonValueKind.Undefined:
                    // Ignore Undefined, assume irrelevant (i.e. not included in comparison).
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected JsonValueKind {left.ValueKind}.");
            }
        }

        /// <inheritdoc/>
        public bool Equals(string? x, string? y)
        {
            if (x == null && y == null)
                return true;
            else if (x == null || y == null)
                return false;

            var ljr = new Utf8JsonReader(new BinaryData(x));
            if (!JsonElement.TryParseValue(ref ljr, out JsonElement? lje))
                throw new ArgumentException("JSON is not considered valid.", nameof(x));

            var rjr = new Utf8JsonReader(new BinaryData(y));
            if (!JsonElement.TryParseValue(ref rjr, out JsonElement? rje))
                throw new ArgumentException("JSON is not considered valid.", nameof(y));

            var state = new CompareState(new JsonElementComparerResult(lje.Value, rje.Value, 1), Options.PathComparer);
            Compare(lje.Value, rje.Value, state);
            return !state.MaxDifferencesFound;
        }

        /// <inheritdoc/>
        public bool Equals(JsonElement x, JsonElement y)
        {
            var state = new CompareState(new JsonElementComparerResult(x, y, 1), Options.PathComparer);
            Compare(x, y, state);
            return !state.MaxDifferencesFound;
        }

        /// <inheritdoc/>
        public int GetHashCode(string json)
        {
            if (json == null)
                return 0;

            var jr = new Utf8JsonReader(new BinaryData(json));
            if (!JsonElement.TryParseValue(ref jr, out JsonElement? je))
                throw new ArgumentException("JSON is not considered valid.", nameof(json));

            return GetHashCode(je.Value);
        }

        /// <inheritdoc/>
        public int GetHashCode(JsonElement json)
        {
            var hash = new HashCode();
            ComputeHashCode(json, ref hash);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Computes the hash code.
        /// </summary>
        private void ComputeHashCode(JsonElement json, ref HashCode hash)
        {
            hash.Add(json.ValueKind);

            switch (json.ValueKind)
            {
                case JsonValueKind.Null:
                    break;

                case JsonValueKind.True:
                    hash.Add(true.GetHashCode());
                    break;

                case JsonValueKind.False:
                    hash.Add(false.GetHashCode());
                    break;

                case JsonValueKind.Number:
                    hash.Add(json.GetDecimal().GetHashCode());
                    break;

                case JsonValueKind.String:
                    hash.Add(json.GetString());
                    break;

                case JsonValueKind.Array:
                    foreach (var item in json.EnumerateArray())
                    {
                        ComputeHashCode(item, ref hash);
                    }

                    break;

                case JsonValueKind.Object:
                    foreach (var property in json.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        hash.Add(property.Name);
                        ComputeHashCode(property.Value, ref hash);
                    }

                    break;

                case JsonValueKind.Undefined:
                    break;

                default:
                    throw new JsonException(string.Format("Unknown JsonValueKind {0}", json.ValueKind));
            }
        }

        /// <summary>
        /// Provides internal state needed to support the comparison.
        /// </summary>
        private sealed class CompareState
        {
            private readonly Stack<string> _unqualifiedPaths = new(new[] { "$" });
            private readonly Stack<string> _paths = new(new[] { "$" });

            /// <summary>
            /// Initializes a new instance of the <see cref="CompareState"/> class.
            /// </summary>
            /// <param name="result">The <see cref="JsonElementComparerResult"/>.</param>
            /// <param name="pathComparer">The <see cref="IEqualityComparer{String}"/> to use for comparing JSON paths.</param>
            /// <param name="pathsToIgnore">The paths to ignore from the comparison.</param>
            public CompareState(JsonElementComparerResult result, IEqualityComparer<string>? pathComparer, params string[] pathsToIgnore)
            {
                Result = result;
                PathComparer = pathComparer ?? StringComparer.InvariantCultureIgnoreCase;
                var maxDepth = 0;
                PathsToIgnore = new(Text.Json.JsonFilterer.CreateDictionary(pathsToIgnore, JsonPropertyFilter.Exclude, StringComparison.Ordinal, ref maxDepth).Keys);
            }

            /// <summary>
            /// Gets the <see cref="JsonElementComparerResult"/>.
            /// </summary>
            public JsonElementComparerResult Result { get; }

            /// <summary>
            /// Gets or sets the <see cref="IEqualityComparer{String}"/> to use for comparing JSON paths.
            /// </summary>
            /// <remarks>Defaults to <see cref="StringComparer.InvariantCultureIgnoreCase"/>.</remarks>
            public IEqualityComparer<string> PathComparer { get; }

            /// <summary>
            /// Indicates whether the maximum number of differences specified to detect has been found.
            /// </summary>
            public bool MaxDifferencesFound => Result.IsMaxDifferencesFound;

            /// <summary>
            /// Get paths to exclude.
            /// </summary>
            public HashSet<string> PathsToIgnore { get; }

            /// <summary>
            /// Gets the unqualified path (excludes indexing).
            /// </summary>
            public string UnqualifiedPath => _unqualifiedPaths.Peek();

            /// <summary>
            /// Gets the path.
            /// </summary>
            public string Path => _paths.Peek();

            /// <summary>
            /// Encapsulates a path comparison action.
            /// </summary>
            /// <param name="name">The path name.</param>
            /// <param name="action">The action to execute.</param>
            public void Compare(string name, Action action)
            {
                var unqualifiedPath = $"{UnqualifiedPath}.{name}";
                if (PathsToIgnore.Contains(unqualifiedPath, PathComparer))
                    return;

                var path = $"{Path}.{name}";
                if (PathsToIgnore.Contains(path, PathComparer))
                    return;

                _unqualifiedPaths.Push(unqualifiedPath);
                _paths.Push(path);

                action.Invoke();

                _unqualifiedPaths.Pop();
                _paths.Pop();
            }

            /// <summary>
            /// Encapsulates an array item comparison.
            /// </summary>
            /// <param name="index">The array index.</param>
            /// <param name="action">The action to execute.</param>
            public void Compare(int index, Action action)
            {
                _paths.Push($"{Path}[{index}]");

                action.Invoke();

                _paths.Pop();
            }

            /// <summary>
            /// Adds a difference to the result.
            /// </summary>
            /// <param name="left">The left <see cref="JsonElement"/>.</param>
            /// <param name="right">The right <see cref="JsonElement"/>.</param>
            /// <param name="type">The <see cref="JsonElementDifferenceType"/>.</param>
            public void AddDifference(JsonElement left, JsonElement right, JsonElementDifferenceType type) 
                => Result.AddDifference(new JsonElementDifference(Path, left, right, type));
        }
    }
}