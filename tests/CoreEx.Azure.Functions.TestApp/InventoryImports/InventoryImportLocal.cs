namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Provides an optional bounded immutable local source using the same Blob reports and SQL application boundary.</summary>
public static class InventoryImportLocal
{
    /// <summary>Gets the maximum local snapshot size; larger feeds should use a protected immutable source instead.</summary>
    public const int MaximumSnapshotBytes = 1024 * 1024;

    /// <summary>Snapshots one bounded file and opens a fresh read-only view over exactly those bytes for every pass.</summary>
    public static async Task<FileProcessingSummary> RunFileAsync(string path, InventoryImportRunner runner, string attemptId, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        byte[] bytes;
        await using (var source = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
        {
            if (source.Length > MaximumSnapshotBytes)
                throw new InvalidDataException("The local demonstration snapshot exceeds its one MiB budget.");

            bytes = new byte[checked((int)source.Length)];
            await source.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            if (await source.ReadAsync(new byte[1], cancellationToken).ConfigureAwait(false) != 0)
                throw new IOException("The local source grew while its bounded snapshot was read.");
        }

        var identity = new FileProcessingIdentity(InventoryImportIdentity.DefinitionName, InventoryImportIdentity.DefinitionVersion,
            new Uri(fullPath).AbsoluteUri, attemptId, Convert.ToHexString(SHA256.HashData(bytes)));
        return await runner.RunAsync(ct =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
        }, identity, cancellationToken).ConfigureAwait(false);
    }
}
