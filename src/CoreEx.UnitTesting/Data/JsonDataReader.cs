namespace CoreEx.UnitTesting.Data;

/// <summary>
/// Provides a hierarchical mutating reader for JSON or YAML data with dynamic property substitution support using the venerable <see cref="JsonNode"/>.
/// </summary>
/// <remarks>This is <i>not</i> intended for high-volume or high-performance use; more for the likes of basic dynamic data seeding scenarios in unit tests.</remarks>
public sealed partial class JsonDataReader
{
    private static readonly Regex _regex = EmbeddedDynamicParametersRegex();

    private readonly JsonNode _rootNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDataReader"/> class.
    /// </summary>
    private JsonDataReader(JsonNode root, JsonDataReaderOptions? options)
    {
        _rootNode = root.ThrowIfNull().ThrowWhen(root => root is not JsonObject, "JSON root node must be a JsonObject.");
        Options = options ?? new JsonDataReaderOptions();
    }

    /// <summary>
    /// Gets the <see cref="JsonDataReaderOptions"/>.
    /// </summary>
    public JsonDataReaderOptions Options { get; }

    /// <summary>
    /// Gets the root <see cref="JsonNode"/>.
    /// </summary>
    public JsonNode RootNode => _rootNode;

    /// <summary>
    /// Tries to get the <see cref="JsonNode"/> for the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The qualified path to support child navigation.</param>
    /// <param name="jsonNode">The <see cref="JsonNode"/> found.</param>
    /// <returns><see langword="true"/> indicates found; otherwise, false.</returns>
    public bool TryGetPath(string path, out JsonNode? jsonNode)
    {
        jsonNode = JsonFilter.GetMatched(_rootNode.DeepClone(), path);
        return jsonNode is not null;
    }

    /// <summary>
    /// Tries to create the <see cref="JsonNode"/> replacing any dynamic parameters for the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The qualified path to support child navigation.</param>
    /// <param name="jsonNode">The resulting <see cref="JsonNode"/> with all dynamic parameters replaced where possible.</param>
    /// <returns><see langword="true"/> indicates created; otherwise, false.</returns>
    public bool TryCreateData(string path, [NotNullWhen(true)] out JsonNode? jsonNode)
    {
        jsonNode = null;

        if (!TryGetPath(path, out var jn))
            return false;

        jsonNode = CopyAndReplace(jn, new JsonDataReaderArgs(Options.Parameters) { Root = jn, CurrentPropertyName = null, CurrentNode = null, Properties = Options.Properties, ApplyProperties = true });
        return jsonNode is not null;
    }

    /// <summary>
    /// Copies the JSON and replaces any dynamic parameters.
    /// </summary>
    private JsonNode? CopyAndReplace(JsonNode? jn, JsonDataReaderArgs args)
    {
        if (jn is null)
            return null;

        switch (jn)
        {
            case JsonArray ja:
                var newArray = new JsonArray();
                for (int i = 0; i < ja.Count; i++)
                {
                    var item = ja[i];
                    var newItem = CopyAndReplace(item, new JsonDataReaderArgs(args.Parameters) { Root = args.Root, CurrentPropertyName = null, CurrentNode = null, Index = i, Properties = args.Properties, ApplyProperties = args.ApplyProperties });
                    newArray.Add(newItem);
                }

                return newArray;

            case JsonObject jo:
                if (args.ApplyProperties)
                    Options.RootNodePreProcessor?.Invoke(new JsonDataReaderArgs(args.Parameters) { Root = args.Root, CurrentPropertyName = args.CurrentPropertyName, CurrentNode = jo, Properties = args.Properties, Index = args.Index });

                var newObject = new JsonObject();
                foreach (var kvp in jo)
                {
                    var newValue = CopyAndReplace(kvp.Value, new JsonDataReaderArgs(args.Parameters) { Root = args.Root, CurrentPropertyName = kvp.Key, CurrentNode = kvp.Value, Properties = args.Properties, Index = args.Index });
                    newObject[kvp.Key] = newValue;
                }

                if (args.ApplyProperties && Options.Properties.Count > 0)
                    ApplyPropertiesWhereNotFound(jo, newObject, new JsonDataReaderArgs(args.Parameters) { Root = args.Root, CurrentPropertyName = null, CurrentNode = null, Properties = args.Properties, Index = args.Index });

                args.ApplyProperties = false;
                return newObject;

            case JsonValue jv:
                return ReplaceDynamicParameter(jv, args);

            default:
                return jn.DeepClone();
        }
    }

    /// <summary>
    /// Apply properties where not found in the source JSON.
    /// </summary>
    private void ApplyPropertiesWhereNotFound(JsonObject sourceObject, JsonObject targetObject, JsonDataReaderArgs args)
    {
        // Apply the args properties first.
        if (args.Properties is not null)
        {
            foreach (var ap in args.Properties)
            {
                if (sourceObject.ContainsKey(ap.Key))
                    continue;

                object? val = ap.Value;
                if (ap.Value is string str && !string.IsNullOrEmpty(str) && str.Length > 1 && str[0] == '^')
                {
                    if (TryGetDynamicValue(str[1..], args, out var v))
                        val = v;
                }

                if (val is not null)
                    targetObject[ap.Key] = CreateJsonValue(val);
            }
        }

        // Apply the options properties second.
        foreach (var p in Options.Properties)
        {
            if (sourceObject.ContainsKey(p.Key) || (args.Properties is not null && args.Properties.ContainsKey(p.Key)))
                continue;

            object? val = p.Value;
            if (p.Value is string str && !string.IsNullOrEmpty(str) && str.Length > 1 && str[0] == '^')
            {
                if (TryGetDynamicValue(str[1..], args, out var v))
                    val = v;
            }

            if (val is not null)
                targetObject[p.Key] = CreateJsonValue(val);
        }
    }

    /// <summary>
    /// Replace any '^xxx' dynamic placeholders.
    /// </summary>
    private static JsonNode? ReplaceDynamicParameter(JsonValue jv, JsonDataReaderArgs args)
    {
        if (jv.GetValueKind() != JsonValueKind.String)
            return jv.DeepClone();

        var str = jv.GetValue<string>();
        if (!string.IsNullOrEmpty(str) && str.Length > 1 && str[0] == '^')
        {
            if (TryGetDynamicValue(str[1..], args, out var val))
            {
                if (val is string str2 && str2 is not null && ReplaceEmbeddedDynamicParameters(ref str2!, args))
                    val = str2;

                return CreateJsonValue(val);
            }
        }

        if (ReplaceEmbeddedDynamicParameters(jv, args, out var replacedNode))
            return replacedNode;

        return jv.DeepClone();
    }

    /// <summary>
    /// Replace any embedded '(^xxx)' dynamic placeholders.
    /// </summary>
    private static bool ReplaceEmbeddedDynamicParameters(JsonValue jv, JsonDataReaderArgs args, out JsonNode? result)
    {
        result = null;

        if (jv.TryGetValue<string>(out var str))
        {
            if (ReplaceEmbeddedDynamicParameters(ref str, args))
            {
                result = CreateJsonValue(str);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Replace any embedded '(^xxx)' dynamic placeholders.
    /// </summary>
    private static bool ReplaceEmbeddedDynamicParameters(ref string? str, JsonDataReaderArgs args)
    {
        if (string.IsNullOrEmpty(str))
            return false;

        var sb = new StringBuilder();
        int i = 0;
        foreach (var match in _regex.EnumerateMatches(str))
        {
            sb.Append(str.AsSpan(i, match.Index - i));
            var key = str.Substring(match.Index, match.Length);
            if (TryGetDynamicValue(key[2..^1], args, out var val))
            {
                var str2 = val?.ToString();
                ReplaceEmbeddedDynamicParameters(ref str2, args);
                sb.Append(str2);
            }
            else
                sb.Append(key);

            i = match.Index + match.Length;
        }

        if (sb.Length == 0)
            return false;

        sb.Append(str.AsSpan(i, str.Length - i));
        str = sb.ToString();
        return true;
    }

    /// <summary>
    /// Tries to get the dynamic value.
    /// </summary>
    private static bool TryGetDynamicValue(string key, JsonDataReaderArgs args, out object? value)
    {
        if (args.Parameters.TryGetValue(key, out var func))
        {
            value = func(args);
            if (value is string str && str is not null && ReplaceEmbeddedDynamicParameters(ref str!, args))
                value = str;

            return true;
        }
        else if (int.TryParse(key, out var i))
        {
            value = new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Creates a <see cref="JsonValue"/> from a .NET value.
    /// </summary>
    private static JsonValue? CreateJsonValue(object? val)
    {
        if (val is null)
            return null;

        return val switch
        {
            string sv => JsonValue.Create(sv),
            Guid gv => JsonValue.Create(gv),
            DateTime dv => JsonValue.Create(dv),
            DateTimeOffset ov => JsonValue.Create(ov),
            DateOnly dv => JsonValue.Create(dv.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
            TimeOnly tv => JsonValue.Create(tv.ToString("HH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture)),
            bool bv => JsonValue.Create(bv),
            short nsv => JsonValue.Create(nsv),
            int niv => JsonValue.Create(niv),
            long nlv => JsonValue.Create(nlv),
            ushort nusv => JsonValue.Create(nusv),
            uint nuiv => JsonValue.Create(nuiv),
            ulong nulv => JsonValue.Create(nulv),
            decimal ndv => JsonValue.Create(ndv),
            double n2v => JsonValue.Create(n2v),
            float nfv => JsonValue.Create(nfv),
            _ => JsonValue.Create(val.ToString())
        };
    }

    /// <summary>
    /// Tries to create a new <see cref="JsonNode"/> replacing any dynamic parameters for the specified <paramref name="path"/> and deserializes to the specified <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to deserialize to.</typeparam>
    /// <param name="path">The qualified path to support child navigation.</param>
    /// <param name="options">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>The deserialized value where found; otherwise, <see langword="default"/>.</returns>
    /// <remarks>Where specifying the <paramref name="options"/> then the <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> should be aligned with the <see cref="JsonDataReaderOptions.NamingConvention"/>.
    /// Where not specified (i.e. <see langword="null"/>), then the <see cref="JsonDataReaderOptions.NamingConvention"/> will automatically be used.</remarks>
    public T? Deserialize<T>(string path, JsonSerializerOptions? options = null)
    {
        if (!TryCreateData(path, out var jsonNode))
            return default;

        if (options is null)
        {
            if (Options.SerializerOptions is not null)
                options = Options.SerializerOptions;
            else
                options = new JsonSerializerOptions(JsonDefaults.SerializerOptions)
                {
                    PropertyNamingPolicy = Options.NamingConvention switch
                    {
                        JsonPropertyNamingConvention.CamelCase => JsonNamingPolicy.CamelCase,
                        JsonPropertyNamingConvention.SnakeCase => JsonNamingPolicy.SnakeCaseLower,
                        JsonPropertyNamingConvention.KebabCase => JsonNamingPolicy.KebabCaseLower,
                        _ => null
                    }
                };
        }

        return JsonSerializer.Deserialize<T>(jsonNode, options);
    }

    #region ParseYaml+Json

    /// <summary>
    /// Reads and parses the YAML from the named embedded resource <see cref="Stream"/> within the <see name="Assembly"/> inferred from the <typeparamref name="TResource"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="System.Reflection.Assembly"/> to find manifest resources (see <see cref="System.Reflection.Assembly.GetManifestResourceStream(string)"/>).</typeparam>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="options">The optional <see cref="JsonDataReaderOptions"/>.</param>
    /// <returns>The <see cref="JsonDataReader"/>.</returns>
    public static JsonDataReader ParseYaml<TResource>(string resourceName, JsonDataReaderOptions? options = null) => ParseYaml(CoreEx.Abstractions.Resource.GetStream<TResource>(resourceName), options);

    /// <summary>
    /// Reads and parses the YAML <see cref="string"/>.
    /// </summary>
    /// <param name="yaml">The YAML <see cref="string"/>.</param>
    /// <param name="options">The optional <see cref="JsonDataReaderOptions"/>.</param>
    /// <returns>The <see cref="JsonDataReader"/>.</returns>
    public static JsonDataReader ParseYaml(string yaml, JsonDataReaderOptions? options = null)
    {
        using var sr = new StringReader(yaml);
        return ParseYaml(sr, options);
    }

    /// <summary>
    /// Reads and parses the YAML <see cref="Stream"/>.
    /// </summary>
    /// <param name="s">The YAML <see cref="Stream"/>.</param>
    /// <param name="options">The optional <see cref="JsonDataReaderOptions"/>.</param>
    /// <returns>The <see cref="JsonDataReader"/>.</returns>
    public static JsonDataReader ParseYaml(Stream s, JsonDataReaderOptions? options = null) => ParseYaml(new StreamReader(s), options);

    /// <summary>
    /// Reads and parses the YAML <see cref="TextReader"/>.
    /// </summary>
    /// <param name="tr">The YAML <see cref="TextReader"/>.</param>
    /// <param name="options">The optional <see cref="JsonDataReaderOptions"/>.</param>
    /// <returns>The <see cref="JsonDataReader"/>.</returns>
    public static JsonDataReader ParseYaml(TextReader tr, JsonDataReaderOptions? options = null)
    {
        var yaml = new DeserializerBuilder().WithNodeTypeResolver(new YamlNodeTypeResolver()).Build().Deserialize(tr);
        var json = new SerializerBuilder().JsonCompatible().Build().Serialize(yaml!);
        return new(JsonNode.Parse(json) ?? throw new InvalidOperationException("JsonNode.Parse resulted in a null."), options);
    }

    /// <summary>
    /// Reads and parses the JSON from the named embedded resource <see cref="Stream"/> within the <see name="Assembly"/> inferred from the <typeparamref name="TResource"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TResource">The <see cref="Type"/> to infer the <see cref="System.Reflection.Assembly"/> to find manifest resources (see <see cref="System.Reflection.Assembly.GetManifestResourceStream(string)"/>).</typeparam>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="options">The optional <see cref="JsonDataReaderOptions"/>.</param>
    /// <returns>The <see cref="JsonDataReader"/>.</returns>
    public static JsonDataReader ParseJson<TResource>(string resourceName, JsonDataReaderOptions? options = null) => ParseJson(CoreEx.Abstractions.Resource.GetStream<TResource>(resourceName), options);

    /// <summary>
    /// Reads and parses the JSON <see cref="string"/>.
    /// </summary>
    /// <param name="json">The JSON <see cref="string"/>.</param>
    /// <param name="options">The optional <see cref="JsonDataReaderOptions"/>.</param>
    /// <returns>The <see cref="JsonDataReader"/>.</returns>
    public static JsonDataReader ParseJson([StringSyntax(StringSyntaxAttribute.Json)] string json, JsonDataReaderOptions? options = null)
        => new(JsonNode.Parse(json) ?? throw new InvalidOperationException("JsonNode.Parse resulted in a null."), options);

    /// <summary>
    /// Reads and parses the JSON <see cref="Stream"/>.
    /// </summary>
    /// <param name="s">The JSON <see cref="Stream"/>.</param>
    /// <param name="options">The optional <see cref="JsonDataReaderOptions"/>.</param>
    /// <returns>The <see cref="JsonDataReader"/>.</returns>
    public static JsonDataReader ParseJson(Stream s, JsonDataReaderOptions? options = null) => new(JsonNode.Parse(s) ?? throw new InvalidOperationException("JsonNode.Parse resulted in a null."), options);

    /// <summary>
    /// Reads and parses the <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="jsonNode">The <see cref="JsonNode"/>.</param>
    /// <param name="options">The optional <see cref="JsonDataReaderOptions"/>.</param>
    /// <returns>The <see cref="JsonDataReader"/>.</returns>
    public static JsonDataReader ParseJson(JsonNode jsonNode, JsonDataReaderOptions? options = null) => new(jsonNode, options);

    #endregion

    /// <summary>
    /// A custom <see cref="INodeTypeResolver"/> to support the YAML to JSON conversion of boolean and number types.
    /// </summary>
    private sealed class YamlNodeTypeResolver : INodeTypeResolver
    {
        private static readonly string[] boolValues = ["true", "false"];

        /// <inheritdoc/>
        bool INodeTypeResolver.Resolve(NodeEvent? nodeEvent, ref Type currentType)
        {
            if (nodeEvent is Scalar scalar && scalar.Style == YamlDotNet.Core.ScalarStyle.Plain)
            {
                if (decimal.TryParse(scalar.Value, out _))
                {
                    if (scalar.Value.Length > 1 && scalar.Value.StartsWith('0')) // Valid JSON does not support a number that starts with a zero.
                        currentType = typeof(string);
                    else
                        currentType = typeof(decimal);

                    return true;
                }

                if (boolValues.Contains(scalar.Value))
                {
                    currentType = typeof(bool);
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Provides the generated <see cref="Regex"/> for <see cref="ReplaceEmbeddedDynamicParameters(ref string?, JsonDataReaderArgs)"/>.
    /// </summary>
    [GeneratedRegex(@"\(\^(.*?)\)", RegexOptions.Compiled)]
    private static partial Regex EmbeddedDynamicParametersRegex();
}