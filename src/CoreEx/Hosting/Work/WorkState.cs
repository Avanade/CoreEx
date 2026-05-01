namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents the status and result of a long-running <see cref="WorkOrchestrator"/>-tracked work instance.
/// </summary>
public class WorkState : IIdentifier<string?>
{
    /// <inheritdoc/>
    /// <remarks>The identifier must be globally unique across all work types.</remarks>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkOrchestrator"/> type name.
    /// </summary>
    /// <remarks>Enables separation between one or more <see cref="WorkState"/> types.</remarks>
    [JsonPropertyName("type")]
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets the related entity key where applicable.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the trace parent (defaults to see <see cref="System.Diagnostics.Activity.Id"/>).
    /// </summary>
    public string? TraceParent { get; set; }

    /// <summary>
    /// Gets or sets the trace state (defaults to see <see cref="System.Diagnostics.Activity.TraceStateString"/>).
    /// </summary>
    public string? TraceState { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus"/>.
    /// </summary>
    public WorkStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the owning <see cref="AuthenticationUser"/>.
    /// </summary>
    /// <remarks>This provides a basic authorization-style opportunity by verifying <i>only</i> the initiating user has ongoing access.</remarks>
    public AuthenticationUser? User { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Created"/> <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkOrchestrator"/> expiry <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <remarks>Where the work has not <see cref="WorkStatus.Finished"/> by the expiry it will be automatically <see cref="WorkStatus.Expired"/>.</remarks>
    public DateTimeOffset Expiry { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Started"/> <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset? Started { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Indeterminate"/> <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset? Indeterminate { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Finished"/> <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset? Finished { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Failed"/> or <see cref="WorkStatus.Expired"/> reason.
    /// </summary>
    public string? Reason { get; set; }
}