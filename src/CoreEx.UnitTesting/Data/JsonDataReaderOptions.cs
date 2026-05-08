namespace CoreEx.UnitTesting.Data;

/// <summary>
/// Provides options for the <see cref="JsonDataReader"/>.
/// </summary>
/// <remarks>The following <see cref="Parameters"/> (note the snake_case convention) are configured out-of-the-box:
/// <list type="bullet">
/// <item><description>'<c>id</c>' - Generates a new identifier using <see cref="Runtime.NewId"/>.</description></item>
/// <item><description>'<c>guid</c>' - Generates a new GUID using <see cref="Runtime.NewGuid"/>.</description></item>
/// <item><description>'<c>now</c>' - Gets the current UTC <see cref="DateTimeOffset"/> using <see cref="Runtime.UtcNow"/>.</description></item>
/// <item><description>'<c>tomorrow</c>' - Gets the UTC <see cref="DateTimeOffset"/> for tomorrow using <see cref="Runtime.UtcNow"/> plus 1 day.</description></item>
/// <item><description>'<c>yesterday</c>' - Gets the UTC <see cref="DateTimeOffset"/> for yesterday using <see cref="Runtime.UtcNow"/> minus 1 day.</description></item>
/// <item><description>'<c>tenant_id</c>' - Gets the current <see cref="ExecutionContext.TenantId"/>.</description></item>
/// <item><description>'<c>user_id</c>' - Gets the current <see cref="ExecutionContext.User"/> <see cref="Security.AuthenticationUser.Id"/>.</description></item>
/// <item><description>'<c>user_name</c>' - Gets the current <see cref="ExecutionContext.User"/> <see cref="Security.AuthenticationUser.UserName"/>.</description></item>
/// <item><description>'<c>index</c>' - Gets the current array index where the <see cref="JsonDataReaderArgs.CurrentNode"/> is an element within a <see cref="JsonArray"/>; otherwise, zero.</description></item>
/// </list>
/// </remarks>
public class JsonDataReaderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDataReaderOptions"/> class.
    /// </summary>
    /// <param name="namingConvention">The JSON property naming convention used by the <see cref="JsonDataReader"/>; defaults to <see cref="JsonPropertyNamingConvention.PascalCase"/>.</param>
    public JsonDataReaderOptions(JsonPropertyNamingConvention namingConvention = JsonPropertyNamingConvention.PascalCase)
    {
        NamingConvention = namingConvention;
        Parameters = new(StringComparer.OrdinalIgnoreCase)
        {
            { "id", _ => Runtime.NewId() },
            { "guid", _ => Runtime.NewGuid() },
            { "now", _ => Runtime.UtcNow },
            { "tomorrow", _ => Runtime.UtcNow.AddDays(1) },
            { "yesterday", _ => Runtime.UtcNow.AddDays(-1) },
            { "tenant_id", _ => ExecutionContext.TryGetCurrent(out var ec) ? ec.TenantId : TenantId },
            { "user_id", _ => ExecutionContext.TryGetCurrent(out var ec) && ec.User is not null ? ec.User.Id : AuthenticationUser.EnvironmentUser.Id },
            { "user_name", _ => ExecutionContext.TryGetCurrent(out var ec) && ec.User is not null ? ec.User.UserName : AuthenticationUser.EnvironmentUser.UserName },
            { "index", args => args?.Index ?? 0 }
        };
    }

    /// <summary>
    /// Gets the JSON property naming convention.
    /// </summary>
    public JsonPropertyNamingConvention NamingConvention { get; }

    /// <summary>
    /// Gets or sets the optional <see cref="JsonSerializerOptions"/> to be used by the <see cref="JsonDataReader"/>.
    /// </summary>
    /// <remarks>Where specifying then the <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> should be aligned with the <see cref="NamingConvention"/></remarks>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>
    /// Gets the dynamic runtime parameters and their corresponding functions.
    /// </summary>
    public Dictionary<string, Func<JsonDataReaderArgs, object?>> Parameters { get; }

    /// <summary>
    /// Gets the properties that will be applied to the root-most <see cref="JsonObject"/> where not already present.
    /// </summary>
    /// <remarks>Root-most means the top-level JSON object in the hierarchy; i.e. if the root is a JSON array, then the direct child objects would be considered root-most.</remarks>
    public Dictionary<string, object?> Properties { get; } = [];

    /// <summary>
    /// Gets or sets the default <see cref="IReadOnlyTenantId.TenantId"/> where it can not be obtained from the current <see cref="ExecutionContext"/>.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets a delegate that is invoked for each root-most <see cref="JsonObject"/> to allow pre-processing.
    /// </summary>
    /// <remarks>This can be used to customize processing of the root-most node (see <see cref="JsonDataReaderArgs.CurrentNode"/>) prior to further processing, such as adding or modifying properties, etc. Once complete, then the standard substitution and property application occurs.
    /// <para>Root-most means the top-level JSON object in the hierarchy; i.e. if the root is a JSON array, then the direct child objects would be considered root-most.</para></remarks>
    public Action<JsonDataReaderArgs>? RootNodePreProcessor { get; set; }

    /// <summary>
    /// Adds standard properties to the root <see cref="JsonObject"/> where not already present.
    /// </summary>
    /// <returns>The <see cref="JsonDataReaderOptions"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The following standard properties (converted based on the <see cref="NamingConvention"/>) are included:
    /// <list type="bullet">
    /// <item><description>'<c>CreatedOn</c>' - Set to '<c>^now</c>'.</description></item>
    /// <item><description>'<c>CreatedBy</c>' - Set to '<c>^user_name</c>'.</description></item>
    /// <item><description>'<c>TenantId</c>' - Set to '<c>^tenant_id</c>'.</description></item>
    /// </list>
    /// </remarks>
    public JsonDataReaderOptions AddStandardProperties()
    {
        Properties.TryAdd(ConvertPropertyName(nameof(ChangeLog.CreatedOn)), "^now");
        Properties.TryAdd(ConvertPropertyName(nameof(ChangeLog.CreatedBy)), "^user_name");
        Properties.TryAdd(ConvertPropertyName(nameof(TenantId)), "^tenant_id");
        return this;
    }

    /// <summary>
    /// Creates a <see cref="JsonDataReaderOptions"/> instance configured for reference data.
    /// </summary>
    /// <param name="namingConvention">The JSON property naming convention used by the <see cref="JsonDataReader"/>; defaults to <see cref="JsonPropertyNamingConvention.PascalCase"/>.</param>
    /// <param name="idGenerator">An optional function to generate the <see cref="RefData.Abstractions.IReferenceData"/> <see cref="IIdentifier.Id"/>.</param>
    /// <returns>The <see cref="JsonDataReaderOptions"/>.</returns>
    /// <remarks>This method will configure the <see cref="RootNodePreProcessor"/> to convert a single key/value pair into '<c>code</c>' and '<c>text</c>' properties by convention.
    /// <para>The following additional <see cref="Properties"/> are included in addition to the <see cref="AddStandardProperties"/>:
    /// <list type="bullet">
    /// <item><description>'<c>id</c>' - Uses the <paramref name="idGenerator"/> where provided; otherwise, '<c>^id</c>'.</description></item>
    /// <item><description>'<c>isActive</c>' - Set to <see langword="true"/>.</description></item>
    /// <item><description>'<c>sortOrder</c>' - Uses the current array index where the <see cref="JsonDataReaderArgs.CurrentNode"/> is an element within a <see cref="JsonArray"/>; otherwise, zero.</description></item>
    /// </list>
    /// </para></remarks>
    public static JsonDataReaderOptions CreateForReferenceData(JsonPropertyNamingConvention namingConvention = JsonPropertyNamingConvention.PascalCase, Func<object?>? idGenerator = null)
    {
        var o = new JsonDataReaderOptions(namingConvention).AddStandardProperties();
        o.Properties.TryAdd(o.ConvertPropertyName(nameof(RefData.Abstractions.IReferenceData.Id)), idGenerator is null ? "^id" : "^__idGenerator");
        o.Properties.TryAdd(o.ConvertPropertyName(nameof(RefData.Abstractions.IReferenceData.IsActive)), true);
        o.Properties.TryAdd(o.ConvertPropertyName(nameof(RefData.Abstractions.IReferenceData.SortOrder)), "^index");

        if (idGenerator is not null)
            o.Parameters.TryAdd("__idGenerator", _ => idGenerator());

        o.RootNodePreProcessor = args =>
        {
            // Where only a single property exists, then assume it is the code & text pair by convention.
            if (args.CurrentNode is JsonObject jsonObject && jsonObject.TryGetNonEnumeratedCount(out var count) && count == 1)
            {
                var kvp = jsonObject.First();
                if (kvp.Value is not null && kvp.Value.GetValueKind() == JsonValueKind.String)
                {
                    jsonObject.Remove(kvp.Key);
                    jsonObject.Add(o.ConvertPropertyName(nameof(RefData.Abstractions.IReferenceData.Code)), kvp.Key);
                    jsonObject.Add(o.ConvertPropertyName(nameof(RefData.Abstractions.IReferenceData.Text)), kvp.Value);
                }
            }
        };

        return o;
    }

    /// <summary>
    /// Converts the <paramref name="propertyName"/> based on the configured <see cref="NamingConvention"/>.
    /// </summary>
    /// <param name="propertyName">The property name to convert.</param>
    /// <returns>The converted property name.</returns>
    [return: NotNullIfNotNull(nameof(propertyName))]
    public string? ConvertPropertyName(string? propertyName)
    {
        if (propertyName is null)
            return null;

        return NamingConvention switch
        {
            JsonPropertyNamingConvention.PascalCase => SentenceCase.ToPascalCase(propertyName),
            JsonPropertyNamingConvention.CamelCase => SentenceCase.ToCamelCase(propertyName),
            JsonPropertyNamingConvention.SnakeCase => SentenceCase.ToSnakeCase(propertyName),
            JsonPropertyNamingConvention.KebabCase => SentenceCase.ToKebabCase(propertyName),
            _ => propertyName
        };
    }
}