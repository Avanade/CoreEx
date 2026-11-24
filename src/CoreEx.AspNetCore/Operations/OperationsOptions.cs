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
    /// Adds an operation type to the registry.
    /// </summary>
    /// <typeparam name="TMessage">The initiating message type.</typeparam>
    /// <param name="operationName">The stable application-owned operation name, independent of the CLR type name.</param>
    /// <param name="configure">The optional type-specific configuration.</param>
    public void AddOperation<TMessage>(string operationName, Action<OperationTypeOptions<TMessage>>? configure = null)
    {
        operationName.ThrowIfNullOrWhiteSpace();
        if (_types.ContainsKey(operationName))
            throw new InvalidOperationException($"Operation '{operationName}' is already registered.");

        var options = new OperationTypeOptions<TMessage>();
        configure?.Invoke(options);
        _types.Add(operationName, new OperationTypeDefinition(operationName, typeof(TMessage), options.CancellationEnabled, options.AuthorizeAsync, options.ReadResultAsync, options.MapError, options.RetryAfter));
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
    }

    /// <summary>
    /// Resolves an explicitly named operation, or the sole registration for the published message type.
    /// </summary>
    internal OperationTypeDefinition Resolve(Type messageType, string? operationName)
    {
        if (operationName is not null)
        {
            if (_types.TryGetValue(operationName, out var named) && named.MessageType == messageType)
                return named;
        }
        else
        {
            var matches = _types.Values.Where(d => d.MessageType == messageType).Take(2).ToArray();
            if (matches.Length == 1)
                return matches[0];
        }

        throw new InvalidOperationException($"Tracked publishing requires an unambiguous registered operation name for message type '{messageType.FullName}'.");
    }

    /// <summary>
    /// Determines whether the execution user can own and observe an operation.
    /// </summary>
    internal static bool HasOwner(ExecutionContext context)
        => !ReferenceEquals(context.User, AuthenticationUser.EnvironmentUser)
            && context.User.Type is AuthenticationType.ApplicationUser or AuthenticationType.AccountUser or AuthenticationType.SystemUser
            && !string.IsNullOrWhiteSpace(context.User.UserName);

    /// <summary>
    /// Enforces scope, username ownership and any additional registered authorization policy.
    /// </summary>
    internal static async Task<bool> IsAuthorizedAsync(IServiceProvider services, ExecutionContext context, OperationState state, OperationTypeDefinition definition, CancellationToken cancellationToken)
    {
        if (!HasOwner(context) || string.IsNullOrWhiteSpace(state.InitiatedBy)
            || !string.Equals(state.InitiatedBy, context.User.UserName, StringComparison.Ordinal)
            || !string.Equals(state.DomainName, services.GetRequiredService<IHostSettings>().DomainName, StringComparison.Ordinal)
            || !string.Equals(state.TenantId, string.IsNullOrEmpty(context.TenantId) ? null : context.TenantId, StringComparison.Ordinal)
            || !string.Equals(state.OperationName, definition.OperationName, StringComparison.Ordinal))
            return false;

        return definition.AuthorizeAsync is null || await definition.AuthorizeAsync(context, state, cancellationToken).ConfigureAwait(false);
    }
}
