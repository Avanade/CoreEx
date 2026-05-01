namespace CoreEx.Validation;

/// <summary>
/// Represents optional arguments for a validation.
/// </summary>
public sealed record class ValidationArgs
{
    private static bool? _defaultUseJsonNames;
    private bool? _useJsonNames;

    /// <summary>
    /// Gets or sets the default <see cref="UseJsonNames"/> for all validations unless explicitly overridden. Defaults to <see langword="true"/>.
    /// </summary>
    public static bool DefaultUseJsonNames
    {
        get => _defaultUseJsonNames ??= Internal.GetConfigurationValue("CoreEx:Validation:DefaultUseJsonNames", true);
        set => _defaultUseJsonNames = value;
    }

    /// <summary>
    /// Indicates whether to use the JSON name for the <see cref="MessageItem"/> <see cref="MessageItem.Property"/> for all validation messages.
    /// </summary>
    /// <remarks>Defaults to <see cref="DefaultUseJsonNames"/>.</remarks>
    public bool UseJsonNames
    { 
        get => _useJsonNames ??= DefaultUseJsonNames;
        set => _useJsonNames = value;
    }

    /// <summary>
    /// Gets or sets the entity prefix used for fully qualified <i>entity.property</i> naming.
    /// </summary>
    /// <remarks>This is only used for root-level validations.</remarks>
    public string? FullyQualifiedEntityName { get; set; }

    /// <summary>
    /// Gets or sets the entity prefix used for fully qualified <i>entity.property</i> JSON naming.
    /// </summary>
    /// <remarks>This is only used for root-level validations.</remarks>
    public string? FullyQualifiedJsonEntityName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> used for JSON property naming.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> to use when resolving services.
    /// </summary>
    /// <remarks>The <see cref="ExecutionContext.ServiceProvider"/> will be used as the default where not specified.</remarks>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// Gets or sets the additional parameters.
    /// </summary>
    /// <remarks>Additional parameters provide a means to pass values down through the validation stack.</remarks>
    public IDictionary<string, object?> Parameters { get; set => field = value.ThrowIfNull(); } = new Dictionary<string, object?>();
}