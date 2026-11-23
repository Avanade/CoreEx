namespace CoreEx.AspNetCore.Operations;

/// <summary>
/// Configures the domain's generic asynchronous operations resource.
/// </summary>
public sealed class OperationsOptions
{
    private readonly Dictionary<string, OperationTypeDefinition> _types = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the route prefix relative to the application.
    /// </summary>
    public string RoutePrefix { get; set; } = "/operations";

    /// <summary>
    /// Gets or sets the stable operation name that, with the operation identifier, forms the work identity; qualified by the host domain name and the ambient <see cref="ExecutionContext.TenantId"/> (where present).
    /// </summary>
    public string OperationName { get; set; } = "webapi-operations";

    /// <summary>
    /// Adds an operation type to the registry.
    /// </summary>
    /// <typeparam name="TMessage">The message type used as the persisted operation type.</typeparam>
    /// <param name="configure">The optional type-specific configuration.</param>
    public void AddOperation<TMessage>(Action<OperationTypeOptions<TMessage>>? configure = null)
    {
        var typeName = WorkArgs.GetTypeName<TMessage>();
        if (_types.ContainsKey(typeName))
            throw new InvalidOperationException($"Operation type '{typeName}' is already registered.");

        var options = new OperationTypeOptions<TMessage>();
        configure?.Invoke(options);
        _types.Add(typeName, new OperationTypeDefinition(typeName, options.CancellationEnabled, options.AuthorizeAsync, options.ReadResultAsync, options.MapError, options.RetryAfter));
    }

    /// <summary>
    /// Gets the registered operation types.
    /// </summary>
    internal IReadOnlyDictionary<string, OperationTypeDefinition> Types => _types;

    /// <summary>
    /// Validates the operation resource configuration.
    /// </summary>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(RoutePrefix) || !RoutePrefix.StartsWith('/') || RoutePrefix.StartsWith("//", StringComparison.Ordinal) || RoutePrefix.Contains('?') || RoutePrefix.Contains('#'))
            throw new InvalidOperationException("The operations route prefix must be an app-relative absolute path.");

        if (string.IsNullOrWhiteSpace(OperationName))
            throw new InvalidOperationException("The operations resource name is required.");

    }
}
