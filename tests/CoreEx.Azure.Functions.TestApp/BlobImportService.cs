namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Defines the application boundary for processing a native blob.
/// </summary>
public interface IBlobImportService
{
    /// <summary>
    /// Processes a blob revision and persists its outcome; operational failures propagate.
    /// </summary>
    /// <param name="blob">The triggering blob client.</param>
    /// <param name="cancellationToken">The invocation cancellation token.</param>
    Task ProcessAsync(BlobClient blob, CancellationToken cancellationToken = default);
}

/// <summary>
/// Demonstrates revision-safe small JSON file processing with repeatable per-revision output.
/// </summary>
/// <param name="storage">The output storage client.</param>
/// <param name="logger">The application logger.</param>
[ScopedService<IBlobImportService>]
public sealed class BlobImportService(BlobServiceClient storage, ILogger<BlobImportService> logger) : IBlobImportService
{
    /// <inheritdoc/>
    public async Task ProcessAsync(BlobClient blob, CancellationToken cancellationToken = default)
    {
        var properties = (await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Value;
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["BlobName"] = blob.Name, ["ETag"] = properties.ETag.ToString() });
        var download = await blob.DownloadContentAsync(new BlobDownloadOptions
        {
            Conditions = new BlobRequestConditions { IfMatch = properties.ETag }
        }, cancellationToken).ConfigureAwait(false);

        string? name = null;
        var status = "accepted";
        try
        {
            var value = JsonSerializer.Deserialize<BlobImportRequest>(download.Value.Content.ToMemory().Span, JsonDefaults.SerializerOptions);
            name = value?.Name.Required() ?? throw new ValidationException("A file value is required.");
        }
        catch (JsonException)
        {
            status = "rejected";
            logger.LogWarning("Blob rejected because its content is not a valid JSON request.");
        }
        catch (ValidationException)
        {
            status = "rejected";
            logger.LogWarning("Blob rejected because its name is required.");
        }

        var report = new BlobImportReport(blob.Name, properties.ETag.ToString(), status, name);
        var output = storage.GetBlobContainerClient("import-results").GetBlobClient(BlobImportReport.GetReportName(blob.Name, report.ETag));
        await output.UploadAsync(BinaryData.FromObjectAsJson(report, JsonDefaults.SerializerOptions), overwrite: true, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Blob processing completed with status {Status}; report {ReportName}.", status, output.Name);
    }
}
