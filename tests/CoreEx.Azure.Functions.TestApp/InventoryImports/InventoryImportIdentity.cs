namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Builds collision-safe, credential-free report and business identities.</summary>
public static class InventoryImportIdentity
{
    /// <summary>Gets the logical integration definition selected by the host route.</summary>
    public const string DefinitionName = InventoryProcessor.DefinitionName;

    /// <summary>Gets the pinned mapping and business interpretation version.</summary>
    public const string DefinitionVersion = InventoryProcessor.DefinitionVersion;

    /// <summary>Gets a stable durable command key excluding the delivery attempt.</summary>
    public static string UnitKey(FileProcessingIdentity identity, string tenantId, string commandId)
        => Hash([tenantId, identity.DefinitionName, identity.DefinitionVersion, identity.SourceId, identity.SourceRevision, commandId]);

    /// <summary>Gets an isolated attempt report name including definition, full source, revision, and attempt.</summary>
    public static string ReportName(FileProcessingIdentity identity)
        => Hash([identity.DefinitionName, identity.DefinitionVersion, identity.SourceId, identity.SourceRevision, identity.AttemptId]) + ".jsonl";

    /// <summary>Hashes an unambiguously framed sequence, rather than concatenating potentially ambiguous identifiers.</summary>
    private static string Hash(string?[] values) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(values))).ToLowerInvariant();
}
