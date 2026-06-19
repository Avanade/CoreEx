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
    /// <param name="id">The work identifier.</param>
    /// <returns>The newly instantiated <see cref="WorkArgs"/>.</returns>
    public static WorkArgs Create<T>(string? id = null) => new(GetTypeName<T>(), id);

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkArgs"/> class.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <param name="id">The identifier.</param>
    public WorkArgs(string typeName, string? id = null)
    {
        TypeName = typeName.ThrowIfNullOrEmpty();
        Id = id.ThrowIfEmpty() ?? Runtime.NewId();
    }

    /// <summary>
    /// Gets or sets the <see cref="WorkOrchestrator"/> type name.
    /// </summary>
    /// <remarks>Enables separation between one or more <see cref="WorkState"/> types; see <see cref="WorkOrchestrator.GetAsync(string, CancellationToken)"/> to minimize cross-type access challenges.</remarks>
    public string TypeName { get; }

    /// <inheritdoc/>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the expiry <see cref="TimeSpan"/>.
    /// </summary>
    /// <remarks>The <see cref="WorkState.Expiry"/> will default to the <see cref="WorkOrchestrator.ExpiryTimeSpan"/> where not specified.</remarks>
    public TimeSpan? Expiry { get; set; }

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
    /// <remarks>This provides a basic authorization-style opportunity by verifying <i>only</i> the initiating user has ongoing access.</remarks>
    public AuthenticationUser? User { get; set; }
}