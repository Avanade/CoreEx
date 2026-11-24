namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents the <see cref="WorkOrchestrator.CreateAsync"/> arguments.
/// </summary>
public record class WorkArgs : IReadOnlyIdentifier<string>
{
    /// <summary>
    /// Gets the underlying <see cref="Type"/> name for the specified <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to infer the <see cref="WorkState.TypeName"/> enabling state separation.</typeparam>
    /// <returns>The <see cref="Type.FullName"/>.</returns>
    public static string GetTypeName<T>() => typeof(T).FullName ?? typeof(T).Name;

    /// <summary>
    /// Creates the <see cref="WorkArgs"/> using the <typeparamref name="T"/> <see cref="Type.FullName"/> as the <see cref="TypeName"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to infer the <see cref="WorkState.TypeName"/> enabling state separation.</typeparam>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="key">The operation instance key; its canonical string must not be empty or whitespace.</param>
    /// <returns>The newly instantiated <see cref="WorkArgs"/>.</returns>
    public static WorkArgs Create<T>(string operationName, CompositeKey key) => new(GetTypeName<T>(), operationName, key);

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkArgs"/> class.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="key">The operation instance key; its canonical string must not be empty or whitespace.</param>
    /// <remarks>The complete identity is qualified by the <see cref="WorkOrchestrator"/> with the host domain name and, where present, the ambient <see cref="ExecutionContext.TenantId"/>.</remarks>
    public WorkArgs(string typeName, string operationName, CompositeKey key)
    {
        TypeName = typeName.ThrowIfNullOrEmpty();
        OperationName = operationName.ThrowIfNullOrWhiteSpace();
        Key = key;
        Id = key.ToCanonicalString().ThrowIfNullOrWhiteSpace(nameof(key));
    }

    /// <summary>
    /// Gets or sets the <see cref="WorkOrchestrator"/> type name.
    /// </summary>
    /// <remarks>Enables separation between one or more <see cref="WorkState"/> types; caller-facing access also checks the operation type with <see cref="WorkOrchestrator.GetWithTypeAsync(string, string, CompositeKey, CancellationToken)"/>.</remarks>
    public string TypeName { get; }

    /// <summary>
    /// Gets the stable application-defined operation name.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets the operation instance key.
    /// </summary>
    public CompositeKey Key { get; }

    /// <inheritdoc/>
    /// <remarks>Captured from the <see cref="Key"/> using <see cref="CompositeKey.ToCanonicalString"/> at construction; the instance identifier is not globally unique.</remarks>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the expiry <see cref="TimeSpan"/>.
    /// </summary>
    /// <remarks>The <see cref="WorkState.ExpiryOn"/> will default to the <see cref="WorkOrchestrator.ExpiryTimeSpan"/> where not specified.</remarks>
    public TimeSpan? Expiry { get; set; }

    /// <summary>
    /// Gets or sets the maximum time a created work item may wait before starting.
    /// </summary>
    public TimeSpan? StartTimeout { get; set; }

    /// <summary>
    /// Gets or sets the trace parent (defaults to see <see cref="System.Diagnostics.Activity.Id"/>).
    /// </summary>
    public string? TraceParent { get; set; }

    /// <summary>
    /// Gets or sets the trace state (defaults to see <see cref="System.Diagnostics.Activity.TraceStateString"/>).
    /// </summary>
    public string? TraceState { get; set; }

    /// <summary>
    /// Gets or sets the owning <see cref="AuthenticationUser"/>.
    /// </summary>
    /// <remarks>Defaults to the current execution user, or <see cref="AuthenticationUser.EnvironmentUser"/> without an execution context. An unauthenticated user creates unowned work.</remarks>
    public AuthenticationUser? User { get; set; }
}
