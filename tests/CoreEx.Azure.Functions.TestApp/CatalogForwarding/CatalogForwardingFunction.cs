namespace CoreEx.Azure.Functions.TestApp.CatalogForwarding;

/// <summary>Forwards a revision-pinned catalog CSV and writes a separate report for each attempt.</summary>
public sealed class CatalogForwardingFunction(CatalogForwardingProcessor processor, [FromKeyedServices(CatalogForwardingFunction.ReportContainerKey)] BlobContainerClient reports)
{
    /// <summary>The worker-owned catalog report container registration.</summary>
    public const string ReportContainerKey = "CatalogImportReports";

    /// <summary>Validates the entire file before publishing, then reports the attempt outcome.</summary>
    [Function(nameof(ImportCatalog))]
    public async Task ImportCatalog([BlobTrigger("catalog-imports/{name}", Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
    {
        var source = await BlobReadSource.CreateAsync(blob, cancellationToken).ConfigureAwait(false);
        var context = CatalogForwardingProcessor.CreateContext(source.SourceUri.AbsoluteUri, Guid.NewGuid().ToString("N"), source.ETag.ToString());
        var identity = context.Identity;
        var revision = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { identity.SourceId, identity.SourceRevision })));
        var reportName = $"{identity.DefinitionName}/{identity.DefinitionVersion}/{revision}/{identity.AttemptId}.jsonl";
        AppendBlobClient reportBlob = reports.GetAppendBlobClient(reportName);
        var report = new BlobJsonlFileReportAdapter(new BlobJsonlReport(reportBlob),
            attempt: ctx => new { kind = "attempt", ctx.Identity },
            diagnostic: args => new { kind = "diagnostic", args.Diagnostic.Code, args.Diagnostic.Location },
            completion: result => new { kind = "outcome", outcome = result.Outcome, sendsReturned = result.Progress.UnitsExecuted });

        var summary = await FileProcessingReporter.RunAsync(processor, source.OpenReadAsync, context, report, cancellationToken).ConfigureAwait(false);
        if (summary.Outcome == FileProcessingOutcome.ExecutionFailed)
            summary.ExecutionResult.ThrowOnError();
    }
}
