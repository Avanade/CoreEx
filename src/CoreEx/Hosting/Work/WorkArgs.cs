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
    /// <param name="operationKey">The explicit operation identity.</param>
    /// <returns>The newly instantiated <see cref="WorkArgs"/>.</returns>
    public static WorkArgs Create<T>(OperationKey operationKey) => new(GetTypeName<T>(), operationKey);

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkArgs"/> class.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <param name="operationKey">The explicit operation identity.</param>
    public WorkArgs(string typeName, OperationKey operationKey)
    {
        TypeName = typeName.ThrowIfNullOrEmpty();
        OperationKey = operationKey.ThrowIfNull();
    }

    /// <summary>
    /// Gets or sets the <see cref="WorkOrchestrator"/> type name.
    /// </summary>
    /// <remarks>Enables separation between one or more <see cref="WorkState"/> types; caller-facing access also checks the operation type with <see cref="WorkOrchestrator.GetWithTypeAsync(string, OperationKey, CancellationToken)"/>.</remarks>
    public string TypeName { get; }

    /// <summary>
    /// Gets the explicit operation identity.
    /// </summary>
    public OperationKey OperationKey { get; }

    /// <inheritdoc/>
    /// <remarks>The instance identifier is not globally unique; use <see cref="OperationKey"/> for lookup.</remarks>
    public string Id => OperationKey.InstanceId;

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
