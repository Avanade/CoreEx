namespace CoreEx.Json;

/// <summary>
/// Provides a means to apply a filter to include or exclude JSON properties (in effect removing the unwanted properties).
/// </summary>
/// <remarks>The JSON path matching is exact (other than specified <see cref="StringComparison"/>) in that the path matches with no indexing or fully indexed; i.e. no mixing is supported. For example, a JSON path of
/// '<c>$.projects[0].technologies[1]</c>' will only match based on a filter of either '<c>$.projects[0].technologies[1]</c>' (fully indexed) or '<c>$.projects.technologies</c>' (no indexing); not on
/// '<c>$.projects.technologies[1]</c>' (mixed). Property names that contain special characters such as dots may be specified using bracket notation, e.g. <c>$.entries['stackExchange.Redis']</c> or
/// <c>$.entries["stackExchange.Redis"]</c>. Note that the '<c>$.</c>' JSON path prefix for the filter is optional.
/// <para>A path prefixed with a recursive descent marker, '<c>..</c>' or '<c>$..</c>' (e.g. <c>$..Foo</c> or <c>$..Foo.Bar</c>), matches the remainder of the path at <b>any</b> depth within the JSON hierarchy,
/// for both <see cref="JsonFilterOption.Include"/> and <see cref="JsonFilterOption.Exclude"/>. For example, <c>$..Password</c> matches a property named <c>Password</c> irrespective of where it appears in the
/// document. Only this leading (global) form of recursive descent is supported; a scoped, mid-path form (e.g. <c>$.Root..Foo</c>, matching <c>Foo</c> at any depth but only underneath <c>Root</c>) is not currently
/// supported.</para>
/// <para><see cref="JsonFilterOption.Exclude"/> never needs to know whether a container's descendants will ultimately be retained, so it is implemented as a single-pass path-segment walk (no per-node path-string
/// allocation); <see cref="TryExcludeUtf8Json"/> takes this further by walking raw UTF-8 JSON bytes directly via <see cref="Utf8JsonReader"/>/<see cref="Utf8JsonWriter"/>, without ever materializing a
/// <see cref="JsonNode"/> document, for the common case of excluding a property from an already-serialized payload (e.g. <c>$..etag</c>). <see cref="JsonFilterOption.Include"/> cannot do the same, since a
/// container can only be judged empty (and therefore omittable) after all of its descendants have been visited; it remains <see cref="JsonNode"/>-based.</para></remarks>
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
        value.ThrowIfNull();

        if (filter == JsonFilterOption.Exclude)
        {
            var options = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;
            var r = TryExcludeUtf8Json(Encoding.UTF8.GetBytes(value), paths, out var filteredUtf8Json, new JsonWriterOptions { Indented = options.WriteIndented, Encoder = options.Encoder }, comparison);
            json = Encoding.UTF8.GetString(filteredUtf8Json);
            return r;
        }

        var j = JsonNode.Parse(value)!;
        var result = Filter(j, paths, filter, comparison);
        json = j?.ToJsonString(jsonSerializerOptions ?? JsonDefaults.SerializerOptions) ?? "null";
        return result;
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
        jsonSerializerOptions ??= JsonDefaults.SerializerOptions;

        if (filter == JsonFilterOption.Exclude)
        {
            var writerOptions = new JsonWriterOptions { Indented = jsonSerializerOptions.WriteIndented, Encoder = jsonSerializerOptions.Encoder };
            var r = TryExcludeUtf8Json(JsonSerializer.SerializeToUtf8Bytes(value, jsonSerializerOptions), paths, out var filteredUtf8Json, writerOptions, comparison);
            json = Encoding.UTF8.GetString(filteredUtf8Json);
            return r;
        }

        var result = TryFilter(value, paths, out JsonNode node, filter, jsonSerializerOptions, comparison);
        json = node?.ToJsonString(jsonSerializerOptions) ?? "null";
        return result;
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

        if (filter == JsonFilterOption.Exclude)
        {
            var matcher = ExcludeMatcher.Create(paths, comparison);
            if (!matcher.HasPatterns)
                return false;

            var stack = new List<PathSegment>();
            var indexDepth = 0;
            var isFiltered = false;
            FilterExcludeDom(json, matcher, stack, ref indexDepth, ref isFiltered);
            return isFiltered;
        }

        SplitRecursivePaths(paths, comparison, out var normalPaths, out var recursiveSuffixes);

        var maxDepth = 0;
        var dict = CreateDictionary(normalPaths, filter, comparison, ref maxDepth, true);
        var args = new JsonFilterArgs { MaxDepth = maxDepth, Paths = dict, RecursiveSuffixes = recursiveSuffixes, Comparison = comparison };

        FilterInclude(json, args);
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
        SplitRecursivePaths([path.ThrowIfNullOrEmpty()], comparison, out var normalPaths, out var recursiveSuffixes);

        var maxDepth = 0;
        var dict = CreateDictionary(normalPaths, JsonFilterOption.Include, comparison, ref maxDepth, true);
        var args = new JsonFilterArgs { MaxDepth = maxDepth, Paths = dict, RecursiveSuffixes = recursiveSuffixes, Comparison = comparison };

        FilterInclude(json, args);
        return args.MatchedNode;
    }

    /// <summary>
    /// Splits the specified <paramref name="paths"/> into <paramref name="normalPaths"/> (unchanged, exact-match paths) and <paramref name="recursiveSuffixes"/> (paths prefixed with the recursive
    /// descent marker, '<c>..</c>' or '<c>$..</c>', reduced to the trailing suffix to match at any depth).
    /// </summary>
    private static void SplitRecursivePaths(IEnumerable<string>? paths, StringComparison comparison, out List<string> normalPaths, out List<string> recursiveSuffixes)
    {
        normalPaths = [];
        recursiveSuffixes = [];

        foreach (var path in paths ?? [])
        {
            string? tail = null;
            if (path.StartsWith("$..", StringComparison.Ordinal))
                tail = path[3..];
            else if (path.StartsWith("..", StringComparison.Ordinal))
                tail = path[2..];

            if (tail is null)
            {
                normalPaths.Add(path);
                continue;
            }

            if (tail.Length == 0)
                throw new ArgumentException($"The recursive descent path '{path}' must specify a property to match at any depth.", nameof(paths));

            var suffix = NormalizeDoubleQuoteBrackets(tail.StartsWith('[') ? tail : $".{tail}");
            if (!recursiveSuffixes.Contains(suffix, StringComparer.FromComparison(comparison)))
                recursiveSuffixes.Add(suffix);
        }
    }

    /// <summary>
    /// Determines whether the specified <paramref name="path"/> matches any of the <paramref name="suffixes"/> (each of which is anchored at a proper path-segment boundary), indicating a match at any depth.
    /// </summary>
    private static bool MatchesAnyRecursiveSuffix(string path, List<string> suffixes, StringComparison comparison)
    {
        foreach (var suffix in suffixes)
        {
            if (path.EndsWith(suffix, comparison))
                return true;
        }

        return false;
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
                if (dict.Keys.Any(x => IsProperDescendantPath(kvp.Key, x, comparison)))
                    dict[kvp.Key] = false;
            }
        }
        else
            maxDepth = Math.Max(maxDepth, dict.Count == 0 ? 0 : dict.Max(x => GetCumulativeSegments(x.Key).Count()));

        return dict;
    }

    /// <summary>
    /// Determines whether <paramref name="candidate"/> is a genuine descendant of <paramref name="ancestor"/> - i.e. <paramref name="candidate"/> starts with <paramref name="ancestor"/> at a
    /// proper path-segment boundary (the next character being <c>.</c> or <c>[</c>) - rather than merely sharing a raw string prefix (e.g. <c>$.category</c> is not a descendant of
    /// <c>$.categoryText</c>, and vice versa, despite one being a textual prefix of the other).
    /// </summary>
    private static bool IsProperDescendantPath(string ancestor, string candidate, StringComparison comparison) =>
        candidate.Length > ancestor.Length && candidate.StartsWith(ancestor, comparison) && (candidate[ancestor.Length] == '.' || candidate[ancestor.Length] == '[');

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
            var hadIndexes = TryRemovePathIndexes(path, out var pathWithoutIndexes);
            if (hadIndexes && args.Paths.TryGetValue(pathWithoutIndexes, out isSpecifiedPath) && isSpecifiedPath)
            {
                args.MatchedNode = json;
                return false;
            }

            if (args.RecursiveSuffixes.Count > 0)
            {
                // Recursive Include pattern(s) present: this node's own path might match, or a descendant might; a container must therefore not be pruned without first exploring its children.
                if (MatchesAnyRecursiveSuffix(path, args.RecursiveSuffixes, args.Comparison) ||
                    (hadIndexes && MatchesAnyRecursiveSuffix(pathWithoutIndexes, args.RecursiveSuffixes, args.Comparison)))
                {
                    args.MatchedNode = json;
                    return false;
                }

                if (json is not JsonObject && json is not JsonArray)
                    return true;
            }
            else if (!hadIndexes)
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
    /// Tries to apply the JSON <paramref name="excludePaths"/> filter (see <see cref="JsonFilterOption.Exclude"/>) directly against UTF-8 encoded JSON <paramref name="utf8Json"/>, resulting in the
    /// filtered <paramref name="filteredUtf8Json"/>.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 encoded JSON.</param>
    /// <param name="excludePaths">The list of JSON paths to exclude.</param>
    /// <param name="filteredUtf8Json">The resulting UTF-8 encoded JSON with the filtering applied.</param>
    /// <param name="writerOptions">The optional <see cref="JsonWriterOptions"/> used to control the output formatting; defaults to compact (not indented), matching a plain <see cref="Utf8JsonWriter"/>.</param>
    /// <param name="comparison">The paths <see cref="StringComparison"/>; defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
    /// <returns><see langword="true"/> indicates that at least one JSON node was filtered (removed); otherwise, <see langword="false"/> for no changes.</returns>
    /// <remarks>Unlike <see cref="TryJsonFilter"/> and <see cref="Filter(JsonNode, IEnumerable{string}?, JsonFilterOption, StringComparison)"/>, this performs a single-pass <see cref="Utf8JsonReader"/>/
    /// <see cref="Utf8JsonWriter"/> copy and never materializes a <see cref="JsonNode"/> document object model, making it significantly faster and lower-allocation for the common case of excluding one
    /// or more properties (e.g. <c>$..etag</c>) from an already-serialized JSON payload. No naming policy or converters are (re-)applied to property names or values - those were already applied when
    /// <paramref name="utf8Json"/> was originally serialized. Numbers are copied byte-for-byte; string property names/values are decoded and re-written via <see cref="Utf8JsonWriter"/>, so their
    /// escaping (e.g. <c>\uXXXX</c> sequences) may be re-normalized by <paramref name="writerOptions"/>'s <see cref="JsonWriterOptions.Encoder"/> even though the decoded content is unchanged.</remarks>
    public static bool TryExcludeUtf8Json(ReadOnlySpan<byte> utf8Json, IEnumerable<string>? excludePaths, out byte[] filteredUtf8Json, JsonWriterOptions writerOptions = default, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        var matcher = ExcludeMatcher.Create(excludePaths, comparison);
        if (!matcher.HasPatterns)
        {
            filteredUtf8Json = utf8Json.ToArray();
            return false;
        }

        var bufferWriter = new ArrayBufferWriter<byte>(utf8Json.Length);
        var isFiltered = false;

        using (var writer = new Utf8JsonWriter(bufferWriter, writerOptions))
        {
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);
            reader.Read();

            var stack = new List<PathSegment>();
            var indexDepth = 0;
            FilterExcludeStream(ref reader, writer, matcher, stack, ref indexDepth, ref isFiltered);
        }

        filteredUtf8Json = bufferWriter.WrittenSpan.ToArray();
        return isFiltered;
    }

    /// <summary>
    /// Recursively filters the JSON <paramref name="json"/> by removing any node whose path-segment stack matches the <paramref name="matcher"/>.
    /// This is used for the <see cref="JsonFilterOption.Exclude"/> option against an in-memory <see cref="JsonNode"/>.
    /// </summary>
    private static void FilterExcludeDom(JsonNode json, ExcludeMatcher matcher, List<PathSegment> stack, ref int indexDepth, ref bool isFiltered)
    {
        if (json is JsonObject jo)
        {
            foreach (var jn in jo.ToArray())
            {
                stack.Add(PathSegment.ForName(jn.Key));

                if (matcher.IsMatch(stack, indexDepth > 0))
                {
                    jo.Remove(jn.Key);
                    isFiltered = true;
                }
                else if (jn.Value is not null)
                    FilterExcludeDom(jn.Value, matcher, stack, ref indexDepth, ref isFiltered);

                stack.RemoveAt(stack.Count - 1);
            }
        }
        else if (json is JsonArray ja)
        {
            for (var i = ja.Count - 1; i >= 0; i--)
            {
                var jn = ja[i];
                stack.Add(PathSegment.ForIndex(i));
                indexDepth++;

                if (matcher.IsMatch(stack, true))
                {
                    ja.RemoveAt(i);
                    isFiltered = true;
                }
                else if (jn is not null)
                    FilterExcludeDom(jn, matcher, stack, ref indexDepth, ref isFiltered);

                indexDepth--;
                stack.RemoveAt(stack.Count - 1);
            }
        }
    }

    /// <summary>
    /// Recursively copies the current JSON value from <paramref name="reader"/> to <paramref name="writer"/>, skipping any property or array element whose path-segment stack matches the <paramref name="matcher"/>.
    /// This is used for the <see cref="JsonFilterOption.Exclude"/> option against raw UTF-8 JSON bytes (see <see cref="TryExcludeUtf8Json"/>), without ever materializing a <see cref="JsonNode"/> document.
    /// </summary>
    private static void FilterExcludeStream(ref Utf8JsonReader reader, Utf8JsonWriter writer, ExcludeMatcher matcher, List<PathSegment> stack, ref int indexDepth, ref bool isFiltered)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                writer.WriteStartObject();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var name = reader.GetString()!;
                    stack.Add(PathSegment.ForName(name));
                    reader.Read(); // Move onto the property's value.

                    if (matcher.IsMatch(stack, indexDepth > 0))
                    {
                        reader.Skip();
                        isFiltered = true;
                    }
                    else
                    {
                        writer.WritePropertyName(name);
                        FilterExcludeStream(ref reader, writer, matcher, stack, ref indexDepth, ref isFiltered);
                    }

                    stack.RemoveAt(stack.Count - 1);
                }

                writer.WriteEndObject();
                break;

            case JsonTokenType.StartArray:
                writer.WriteStartArray();
                var index = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    stack.Add(PathSegment.ForIndex(index));
                    indexDepth++;

                    if (matcher.IsMatch(stack, true))
                    {
                        reader.Skip();
                        isFiltered = true;
                    }
                    else
                        FilterExcludeStream(ref reader, writer, matcher, stack, ref indexDepth, ref isFiltered);

                    indexDepth--;
                    stack.RemoveAt(stack.Count - 1);
                    index++;
                }

                writer.WriteEndArray();
                break;

            case JsonTokenType.String: writer.WriteStringValue(reader.GetString()); break;
            case JsonTokenType.Number: writer.WriteRawValue(reader.ValueSpan, skipInputValidation: true); break;
            case JsonTokenType.True: writer.WriteBooleanValue(true); break;
            case JsonTokenType.False: writer.WriteBooleanValue(false); break;
            case JsonTokenType.Null: writer.WriteNullValue(); break;
        }
    }

    /// <summary>
    /// Parses a normalized, root-prefixed JSON <paramref name="path"/> (see <see cref="PrependRootPath"/>/<see cref="NormalizeDoubleQuoteBrackets"/>) into its individual <see cref="PathSegment"/>s.
    /// </summary>
    private static PathSegment[] ParseSegments(string path)
    {
        var segments = new List<PathSegment>();
        var i = 0;

        if (i < path.Length && path[i] == '$')
            i++;

        while (i < path.Length)
        {
            var c = path[i];
            if (c == '.')
            {
                var start = ++i;
                while (i < path.Length && path[i] != '.' && path[i] != '[')
                    i++;
                segments.Add(PathSegment.ForName(path[start..i]));
            }
            else if (c == '[')
            {
                i++;
                if (i < path.Length && (path[i] == '\'' || path[i] == '"'))
                {
                    var quote = path[i++];
                    var start = i;
                    while (i < path.Length && path[i] != quote)
                        i++;
                    segments.Add(PathSegment.ForName(path[start..i]));
                    if (i < path.Length) i++; // Skip closing quote.
                }
                else
                {
                    var start = i;
                    while (i < path.Length && path[i] != ']')
                        i++;
                    segments.Add(PathSegment.ForIndex(int.Parse(path[start..i], CultureInfo.InvariantCulture)));
                }

                if (i < path.Length && path[i] == ']')
                    i++;
            }
            else
            {
                // Shouldn't occur for a normalized, root-prefixed path; guard against an infinite loop.
                i++;
            }
        }

        return [.. segments];
    }

    /// <summary>
    /// Represents a single JSON path segment - either a named property or a numeric array index - used by <see cref="ExcludeMatcher"/> to match a traversal path without allocating path strings.
    /// </summary>
    private readonly struct PathSegment
    {
        private readonly string? _name;
        private readonly int _index;

        private PathSegment(string? name, int index)
        {
            _name = name;
            _index = index;
        }

        public static PathSegment ForName(string name) => new(name, -1);

        public static PathSegment ForIndex(int index) => new(null, index);

        public bool IsIndex => _name is null;

        public bool Matches(PathSegment other, StringComparison comparison) =>
            IsIndex == other.IsIndex && (IsIndex ? _index == other._index : string.Equals(_name, other._name, comparison));
    }

    /// <summary>
    /// Compiles a set of <see cref="JsonFilterOption.Exclude"/> paths (plain and recursive-descent) into <see cref="PathSegment"/> arrays, and matches them against a live traversal path-segment stack -
    /// shared by both the <see cref="JsonNode"/>-based (<see cref="FilterExcludeDom"/>) and <see cref="Utf8JsonReader"/>-based (<see cref="FilterExcludeStream"/>) traversal engines, so both engines encode
    /// the exact same matching semantics; only the tree-walking mechanics differ.
    /// </summary>
    private sealed class ExcludeMatcher
    {
        private readonly List<PathSegment[]> _plainPatterns = [];
        private readonly List<PathSegment[]> _recursivePatterns = [];
        private readonly StringComparison _comparison;

        private ExcludeMatcher(StringComparison comparison) => _comparison = comparison;

        /// <summary>
        /// Gets a value indicating whether any patterns were compiled.
        /// </summary>
        public bool HasPatterns => _plainPatterns.Count > 0 || _recursivePatterns.Count > 0;

        /// <summary>
        /// Compiles the specified <paramref name="excludePaths"/> into an <see cref="ExcludeMatcher"/>.
        /// </summary>
        public static ExcludeMatcher Create(IEnumerable<string>? excludePaths, StringComparison comparison)
        {
            var matcher = new ExcludeMatcher(comparison);

            foreach (var path in excludePaths ?? [])
            {
                string? tail = null;
                if (path.StartsWith("$..", StringComparison.Ordinal))
                    tail = path[3..];
                else if (path.StartsWith("..", StringComparison.Ordinal))
                    tail = path[2..];

                if (tail is null)
                {
                    matcher._plainPatterns.Add(ParseSegments(NormalizeDoubleQuoteBrackets(PrependRootPath(path))));
                    continue;
                }

                if (tail.Length == 0)
                    throw new ArgumentException($"The recursive descent path '{path}' must specify a property to match at any depth.", nameof(excludePaths));

                var normalizedTail = NormalizeDoubleQuoteBrackets(tail.StartsWith('[') ? tail : $".{tail}");
                matcher._recursivePatterns.Add(ParseSegments($"{JsonRootPath}{normalizedTail}"));
            }

            return matcher;
        }

        /// <summary>
        /// Determines whether the current traversal <paramref name="stack"/> matches any compiled exclude pattern.
        /// </summary>
        /// <param name="stack">The current (indexed) path-segment stack.</param>
        /// <param name="hadIndexes">Indicates whether <paramref name="stack"/> contains at least one array-index segment (enabling the index-stripped/blanket-array comparison).</param>
        public bool IsMatch(List<PathSegment> stack, bool hadIndexes)
        {
            foreach (var pattern in _plainPatterns)
            {
                if (SegmentsEqual(stack, pattern, _comparison))
                    return true;
            }

            foreach (var pattern in _recursivePatterns)
            {
                if (EndsWithSegments(stack, pattern, _comparison))
                    return true;
            }

            if (!hadIndexes)
                return false;

            var stripped = StripIndexes(stack);

            foreach (var pattern in _plainPatterns)
            {
                if (SegmentsEqual(stripped, pattern, _comparison))
                    return true;
            }

            foreach (var pattern in _recursivePatterns)
            {
                if (EndsWithSegments(stripped, pattern, _comparison))
                    return true;
            }

            return false;
        }

        private static List<PathSegment> StripIndexes(List<PathSegment> stack)
        {
            var stripped = new List<PathSegment>(stack.Count);
            foreach (var segment in stack)
            {
                if (!segment.IsIndex)
                    stripped.Add(segment);
            }

            return stripped;
        }

        private static bool SegmentsEqual(List<PathSegment> stack, PathSegment[] pattern, StringComparison comparison)
        {
            if (stack.Count != pattern.Length)
                return false;

            for (var i = 0; i < pattern.Length; i++)
            {
                if (!stack[i].Matches(pattern[i], comparison))
                    return false;
            }

            return true;
        }

        private static bool EndsWithSegments(List<PathSegment> stack, PathSegment[] pattern, StringComparison comparison)
        {
            if (stack.Count < pattern.Length)
                return false;

            var offset = stack.Count - pattern.Length;
            for (var i = 0; i < pattern.Length; i++)
            {
                if (!stack[offset + i].Matches(pattern[i], comparison))
                    return false;
            }

            return true;
        }
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
        /// Gets the recursive descent path suffixes (see <see cref="SplitRecursivePaths"/>) to match at any depth within the JSON hierarchy.
        /// </summary>
        public List<string> RecursiveSuffixes { get; init; } = [];

        /// <summary>
        /// Gets the <see cref="StringComparison"/> used to match <see cref="Paths"/> and <see cref="RecursiveSuffixes"/>.
        /// </summary>
        public StringComparison Comparison { get; init; }

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