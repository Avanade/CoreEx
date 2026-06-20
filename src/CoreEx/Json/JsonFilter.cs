namespace CoreEx.Json;

/// <summary>
/// Provides a means to apply a filter to include or exclude JSON properties (in effect removing the unwanted properties).
/// </summary>
/// <remarks>The JSON path matching is exact (other than specified <see cref="StringComparison"/>) in that the path matches with no indexing or fully indexed; i.e. no mixing is supported. For example, a JSON path of 
/// '<c>$.projects[0].technologies[1]</c>' will only match based on a filter of either '<c>$.projects[0].technologies[1]</c>' (fully indexed) or '<c>$.projects.technologies</c>' (no indexing); not on 
/// '<c>$.projects.technologies[1]</c>' (mixed). Property names that contain special characters such as dots may be specified using bracket notation, e.g. <c>$.entries['stackExchange.Redis']</c> or
/// <c>$.entries["stackExchange.Redis"]</c>. Note that the '<c>$.</c>' JSON path prefix for the filter is optional.</remarks>
public static partial class JsonFilter
{
    private static readonly Regex _regex = IndexesRegex();

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
    /// Removes all numeric (integer) array indexes from the specified <paramref name="input"/> JSON path; bracket-notation string property names (e.g. <c>['name']</c>) are preserved.
    /// </summary>
    /// <param name="input">The input JSON path.</param>
    /// <param name="path">The resulting JSON path.</param>
    /// <returns><see langword="true"/> indicates indexes were removed; otherwise, <see langword="false"/>.</returns>
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
    /// Tries to apply the JSON <paramref name="filter"/> (using JSON <paramref name="paths"/>) to a JSON <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
    /// </summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
    /// <param name="json">The corresponding JSON <paramref name="value"/> <see cref="string"/> with the filtering applied.</param>
    /// <param name="filter">The <see cref="JsonFilterOption"/>; defaults to <see cref="JsonFilterOption.Include"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
    /// <returns><see langword="true"/> indicates that at least one JSON node was filtered (removed); otherwise, <see langword="false"/> for no changes.</returns>
    public static bool TryJsonFilter([StringSyntax(StringSyntaxAttribute.Json)] string value, IEnumerable<string>? paths, out string json, JsonFilterOption filter = JsonFilterOption.Include, JsonSerializerOptions? jsonSerializerOptions = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        var j = JsonNode.Parse(value.ThrowIfNull())!;
        var r = Filter(j, paths, filter, comparison);
        json = j?.ToJsonString(jsonSerializerOptions ?? JsonDefaults.SerializerOptions) ?? "null";
        return r;
    }

    /// <summary>
    /// Tries to apply the JSON <paramref name="filter"/> (using JSON <paramref name="paths"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
    /// <param name="json">The corresponding JSON <paramref name="value"/> <see cref="string"/> with the filtering applied.</param>
    /// <param name="filter">The <see cref="JsonFilterOption"/>; defaults to <see cref="JsonFilterOption.Include"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
    /// <returns><see langword="true"/> indicates that at least one JSON node was filtered (removed); otherwise, <see langword="false"/> for no changes.</returns>
    public static bool TryFilter<T>(T value, IEnumerable<string>? paths, out string json, JsonFilterOption filter = JsonFilterOption.Include, JsonSerializerOptions? jsonSerializerOptions = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        var r = TryFilter(value, paths, out JsonNode node, filter, jsonSerializerOptions, comparison);
        json = node?.ToJsonString(jsonSerializerOptions ?? JsonDefaults.SerializerOptions) ?? "null";
        return r;
    }

    /// <summary>
    /// Tries to apply the JSON <paramref name="filter"/> (using JSON <paramref name="paths"/>) to a <paramref name="value"/> resulting in the corresponding <paramref name="json"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
    /// <param name="json">The corresponding <paramref name="value"/> <see cref="JsonNode"/> with the filtering applied.</param>
    /// <param name="filter">The <see cref="JsonFilterOption"/>; defaults to <see cref="JsonFilterOption.Include"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
    /// <returns><see langword="true"/> indicates that at least one JSON node was filtered (removed); otherwise, <see langword="false"/> for no changes.</returns>
    public static bool TryFilter<T>(T value, IEnumerable<string>? paths, out JsonNode json, JsonFilterOption filter = JsonFilterOption.Include, JsonSerializerOptions? jsonSerializerOptions = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        json = JsonSerializer.SerializeToNode(value, jsonSerializerOptions ?? JsonDefaults.SerializerOptions)!;
        return Filter(json, paths, filter, comparison);
    }

    /// <summary>
    /// Applies the JSON <paramref name="filter"/> (using JSON <paramref name="paths"/>) to a specified <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="json">The <see cref="JsonNode"/> value.</param>
    /// <param name="paths">The list of JSON paths to <paramref name="filter"/>.</param>
    /// <param name="filter">The <see cref="JsonFilterOption"/>; defaults to <see cref="JsonFilterOption.Include"/>.</param>
    /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
    /// <remarks><see langword="true"/> indicates that at least one JSON node was filtered (removed); otherwise, <see langword="false"/> for no changes.</remarks>
    public static bool Filter(JsonNode json, IEnumerable<string>? paths, JsonFilterOption filter = JsonFilterOption.Include, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (json is null)
            return false;

        var maxDepth = 0;
        var dict = CreateDictionary(paths, filter, comparison, ref maxDepth, true);
        var args = new JsonFilterArgs { MaxDepth = maxDepth, Paths = dict };

        if (filter == JsonFilterOption.Include)
            FilterInclude(json, args);
        else if (maxDepth > 0)
            FilterExclude(json, args, 1);

        return args.IsFiltered;
    }

    /// <summary>
    /// Gets the first <see cref="JsonNode"/> that matches the JSON <paramref name="path"/> from within the specified <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="json">The <see cref="JsonNode"/> value.</param>
    /// <param name="path">The JSON path to match.</param>
    /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
    /// <returns>The first matched <see cref="JsonNode"/> where found; otherwise, <see langword="null"/>.</returns>
    public static JsonNode? GetMatched(JsonNode json, string path, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        var maxDepth = 0;
        var dict = CreateDictionary([path.ThrowIfNullOrEmpty()], JsonFilterOption.Include, comparison, ref maxDepth, true);
        var args = new JsonFilterArgs { MaxDepth = maxDepth, Paths = dict };

        FilterInclude(json, args);
        return args.MatchedNode;
    }

    /// <summary>
    /// Create a <see cref="Dictionary{TKey, TValue}"/> from the <paramref name="paths"/> and expands list with intermediary paths where <paramref name="filter"/> is <see cref="JsonFilterOption.Include"/>.
    /// </summary>
    /// <param name="paths">The list of JSON paths.</param>
    /// <param name="filter">The <see cref="JsonFilterOption"/>.</param>
    /// <param name="comparison">The paths <see cref="StringComparison"/>.</param>
    /// <param name="maxDepth">The maximum hierarchy depth for all specified <paramref name="paths"/>.</param>
    /// <returns>The <see cref="Dictionary{TKey, TValue}"/>.</returns>
    /// <remarks>Where the <see cref="bool"/> is <see langword="true"/> this indicates the specified path; versus, <see langword="false"/> that indicates an intermediary path.</remarks>
    public static Dictionary<string, bool> CreateDictionary(IEnumerable<string>? paths, JsonFilterOption filter, StringComparison comparison, ref int maxDepth)
        => CreateDictionary(paths, filter, comparison, ref maxDepth, false);

    /// <summary>
    /// Create a <see cref="Dictionary{TKey, TValue}"/> from the <paramref name="paths"/> and expands list with intermediary paths where <paramref name="filter"/> is <see cref="JsonFilterOption.Include"/>.
    /// </summary>
    /// <param name="paths">The list of JSON paths.</param>
    /// <param name="filter">The <see cref="JsonFilterOption"/>.</param>
    /// <param name="comparison">The paths <see cref="StringComparison"/>.</param>
    /// <param name="maxDepth">The maximum hierarchy depth for all specified  <paramref name="paths"/>.</param>
    /// <param name="prependRootPath">Indicates whether to prepend the <see cref="JsonRootPath"/> to each path.</param>
    /// <returns>The <see cref="Dictionary{TKey, TValue}"/>.</returns>
    /// <remarks>Where the <see cref="bool"/> is <see langword="true"/> this indicates the specified path; versus, <see langword="false"/> that indicates an intermediary path.</remarks>
    private static Dictionary<string, bool> CreateDictionary(IEnumerable<string>? paths, JsonFilterOption filter, StringComparison comparison, ref int maxDepth, bool prependRootPath)
    {
        var dict = new Dictionary<string, bool>(StringComparer.FromComparison(comparison));
        paths ??= [];

        // Add each 'specified' path.
        foreach (var path in paths)
        {
            var normalized = NormalizeDoubleQuoteBrackets(prependRootPath ? PrependRootPath(path) : path);
            dict.TryAdd(normalized, true);
        }

        // Add each 'intermediary' path where applicable.
        if (filter == JsonFilterOption.Include)
        {
            foreach (var kvp in dict.ToArray())
            {
                var depth = 0;
                foreach (var segment in GetCumulativeSegments(kvp.Key))
                {
                    dict.TryAdd(segment, false);
                    maxDepth = Math.Max(maxDepth, ++depth);
                }

                if (TryRemovePathIndexes(kvp.Key, out var indexless))
                {
                    foreach (var segment in GetCumulativeSegments(indexless))
                        dict.TryAdd(segment, false);
                }
            }

            foreach (var kvp in dict.ToArray())
            {
                if (dict.Keys.Any(x => !x.Equals(kvp.Key, comparison) && x.StartsWith(kvp.Key, comparison)))
                    dict[kvp.Key] = false;
            }
        }
        else
            maxDepth = Math.Max(maxDepth, dict.Count == 0 ? 0 : dict.Max(x => GetCumulativeSegments(x.Key).Count()));

        return dict;
    }

    /// <summary>
    /// Recursively filters the JSON <paramref name="json"/> based on the specified <paramref name="args"/> and results in true where should be excluded (removed).
    /// This is used for the <see cref="JsonFilterOption.Include"/> option.
    /// </summary>
    private static bool FilterInclude(JsonNode json, JsonFilterArgs args)
    {
        var path = json.GetPath();
        if (args.Paths.TryGetValue(path, out var isSpecifiedPath))
        {
            if (isSpecifiedPath)
            {
                args.MatchedNode = json;
                return false;
            }
        }
        else
        {
            if (TryRemovePathIndexes(path, out var pathWithoutIndexes))
            {
                if (args.Paths.TryGetValue(pathWithoutIndexes, out isSpecifiedPath) && isSpecifiedPath)
                {
                    args.MatchedNode = json;
                    return false;
                }
            }
            else
                return true;
        }

        if (json is JsonObject jo)
        {
            foreach (var jn in jo.ToArray())
            {
                if (FilterInclude(jn.Value ?? throw new InvalidOperationException(), args))
                {
                    jo.Remove(jn.Key);
                    args.IsFiltered = true;
                }
                else
                    isSpecifiedPath = true;
            }
        }
        else if (json is JsonArray ja)
        {
            for (var i = ja.Count - 1; i >= 0; i--)
            {
                var jn = ja[i]!;
                if (FilterInclude(jn, args))
                {
                    ja.RemoveAt(i);
                    args.IsFiltered = true;
                }
                else
                    isSpecifiedPath = true;
            }
        }

        return !isSpecifiedPath;
    }

    /// <summary>
    /// Recursively filters the JSON <paramref name="json"/> based on the specified <paramref name="args"/> and results in true where should be excluded (removed).
    /// This is used for the <see cref="JsonFilterOption.Exclude"/> option.
    /// </summary>
    private static bool FilterExclude(JsonNode json, JsonFilterArgs args, int depth)
    {
        if (depth > args.MaxDepth)
            return false;

        var path = json.GetPath();
        if (args.Paths.TryGetValue(path, out var isSpecifiedPath))
        {
            if (isSpecifiedPath)
                return true;
        }
        else
        {
            if (TryRemovePathIndexes(path, out var pathWithoutIndexes))
            {
                if (args.Paths.TryGetValue(pathWithoutIndexes, out isSpecifiedPath) && isSpecifiedPath)
                    return true;
            }
        }

        if (json is JsonObject jo)
        {
            depth++;
            foreach (var jn in jo.ToArray())
            {
                if (FilterExclude(jn.Value ?? throw new InvalidOperationException(), args, depth))
                {
                    jo.Remove(jn.Key);
                    args.IsFiltered = true;
                }
            }
        }
        else if (json is JsonArray ja)
        {
            for (var i = ja.Count - 1; i >= 0; i--)
            {
                var jn = ja[i]!;
                if (FilterExclude(jn, args, depth))
                {
                    ja.RemoveAt(i);
                    args.IsFiltered = true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Provides the generated <see cref="Regex"/> for <see cref="TryRemovePathIndexes"/>.
    /// </summary>
    [GeneratedRegex(@"\[\d+\]", RegexOptions.Compiled)]
    private static partial Regex IndexesRegex();

    /// <summary>
    /// Yields the cumulative path prefix after each token in <paramref name="path"/>, correctly handling bracket-notation
    /// string properties (e.g. <c>['name']</c>, <c>["name"]</c>) as well as numeric array indexes (e.g. <c>[0]</c>) and
    /// standard dot-notation properties.
    /// </summary>
    /// <remarks>
    /// For example, <c>$.entries['stackExchange.Redis'].enabled</c> yields:
    /// <c>$</c>, <c>$.entries</c>, <c>$.entries['stackExchange.Redis']</c>, <c>$.entries['stackExchange.Redis'].enabled</c>.
    /// </remarks>
    private static IEnumerable<string> GetCumulativeSegments(string path)
    {
        if (string.IsNullOrEmpty(path))
            yield break;

        var sb = new StringBuilder();
        var i = 0;

        while (i < path.Length)
        {
            var c = path[i];

            if (c == '[')
            {
                // Bracket token: ['name'], ["name"], or [N] — consume up to and including the closing ].
                var start = i++;
                if (i < path.Length && (path[i] == '\'' || path[i] == '"'))
                {
                    var quote = path[i++];
                    while (i < path.Length && path[i] != quote)
                        i++;
                    if (i < path.Length) i++; // skip closing quote
                }
                else
                {
                    while (i < path.Length && path[i] != ']')
                        i++;
                }
                if (i < path.Length) i++; // skip ']'
                sb.Append(path, start, i - start);
                yield return sb.ToString();
            }
            else if (c == '.' && sb.Length > 0)
            {
                // Dot-notation segment: consume '.' plus all chars up to the next '.' or '['.
                var start = i++;
                while (i < path.Length && path[i] != '.' && path[i] != '[')
                    i++;
                sb.Append(path, start, i - start);
                yield return sb.ToString();
            }
            else
            {
                // Dollar root (or any leading non-dot/non-bracket chars).
                while (i < path.Length && path[i] != '.' && path[i] != '[')
                    sb.Append(path[i++]);
                yield return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Normalizes double-quote bracket-notation property segments to single-quote form so that user-supplied filter paths
    /// match the single-quote output of <see cref="JsonNode.GetPath"/>. For example, <c>$.a["b.c"]</c> becomes <c>$.a['b.c']</c>.
    /// </summary>
    private static string NormalizeDoubleQuoteBrackets(string path) =>
        path.Contains("[\"", StringComparison.Ordinal)
            ? DoubleQuoteBracketsRegex().Replace(path, static m => $"['{m.Groups[1].Value}']")
            : path;

    /// <summary>
    /// Provides the generated <see cref="Regex"/> for <see cref="NormalizeDoubleQuoteBrackets"/>.
    /// </summary>
    [GeneratedRegex(@"\[""([^""]*)""\]", RegexOptions.Compiled)]
    private static partial Regex DoubleQuoteBracketsRegex();

    /// <summary>
    /// Represents the internal arguments for the JSON filter state.
    /// </summary>
    private sealed class JsonFilterArgs
    {
        /// <summary>
        /// Gets the selected JSON paths to include/exclude.
        /// </summary>
        public required Dictionary<string, bool> Paths { get; init; }

        /// <summary>
        /// Gets the maximum depth of the JSON hierarchy of the <see cref="Paths"/> specified.
        /// </summary>
        public int MaxDepth { get; init; } = 0;

        /// <summary>
        /// Indicates whether a filter took place; i.e. there was at least one JSON node removed.
        /// </summary>
        public bool IsFiltered { get; set; }

        /// <summary>
        /// Gets or sets the last fully matched JSON node for am <see cref="JsonFilterOption.Include"/>.
        /// </summary>
        public JsonNode? MatchedNode { get; set; }
    }
}