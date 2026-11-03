namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Demonstrates a native Event Grid-backed Blob trigger delegating to an application service.
/// </summary>
/// <param name="service">The file-processing service.</param>
public sealed class BlobFunctions(IBlobImportService service)
{
    /// <summary>
    /// Processes the triggering blob without converting storage notifications into CoreEx events.
    /// </summary>
    [Function(nameof(ImportBlob))]
    public Task ImportBlob([BlobTrigger("imports/{name}", Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
        => service.ProcessAsync(blob, cancellationToken);
}
