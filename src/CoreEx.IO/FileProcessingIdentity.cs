namespace CoreEx.IO;

/// <summary>Identifies a definition, source revision, and attempt independently of transport.</summary>
/// <remarks>Identity is supplied by the caller, not authenticated from file fields. Never include credentials in a source identifier. A business idempotency key additionally needs a stable unit identity.</remarks>
public sealed class FileProcessingIdentity
{
    /// <summary>Initializes immutable identity metadata shared by both processing passes.</summary>
    /// <param name="definitionName">The logical definition name.</param>
    /// <param name="definitionVersion">The definition version.</param>
    /// <param name="sourceId">The credential-free stable source identity, including its containing store when applicable.</param>
    /// <param name="attemptId">The caller-supplied attempt identifier; retries use different attempts for the same source revision.</param>
    /// <param name="sourceRevision">The opaque source revision, or null when unavailable.</param>
    public FileProcessingIdentity(string definitionName, string definitionVersion, string sourceId, string attemptId, string? sourceRevision = null)
    {
        definitionName.ThrowIfNullOrWhiteSpace();
        definitionVersion.ThrowIfNullOrWhiteSpace();
        sourceId.ThrowIfNullOrWhiteSpace();
        attemptId.ThrowIfNullOrWhiteSpace();
        if (sourceRevision is not null)
            sourceRevision.ThrowIfNullOrWhiteSpace();

        DefinitionName = definitionName;
        DefinitionVersion = definitionVersion;
        SourceId = sourceId;
        AttemptId = attemptId;
        SourceRevision = sourceRevision;
    }

    /// <summary>Gets the logical definition name.</summary>
    public string DefinitionName { get; }

    /// <summary>Gets the definition version.</summary>
    public string DefinitionVersion { get; }

    /// <summary>Gets the credential-free source identity.</summary>
    public string SourceId { get; }

    /// <summary>Gets the opaque source revision; the stream factory, not this label, enforces replay consistency.</summary>
    public string? SourceRevision { get; }

    /// <summary>Gets the processing attempt identifier, not a business idempotency key.</summary>
    public string AttemptId { get; }
}
