namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Selects the current Blob revision at invocation time and pins both processor reads to its ETag.</summary>
/// <remarks>This does not identify a historical Event Grid event revision. Replaced/deleted selected revisions fail operationally rather than silently reading new bytes.</remarks>
public sealed class InventoryBlobImport(InventoryImportRunner runner)
{
    /// <summary>Runs the routed inventory import; returned business failures propagate to native trigger retry handling.</summary>
    public Task ProcessAsync(BlobClient blob, string attemptId, CancellationToken cancellationToken = default)
        => ProcessDefinitionAsync(blob, attemptId, InventoryProcessor.DefinitionName, cancellationToken);

    /// <summary>Processes the definition selected by a native input route, not by untrusted file content.</summary>
    public async Task ProcessDefinitionAsync(BlobClient blob, string attemptId, string definitionName, CancellationToken cancellationToken = default)
    {
        var properties = (await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Value;
        var source = new UriBuilder(blob.Uri) { Query = "", Fragment = "", UserName = "", Password = "" }.Uri.AbsoluteUri;
        var identity = new FileProcessingIdentity(definitionName, InventoryImportIdentity.DefinitionVersion, source, attemptId, properties.ETag.ToString());
        var summary = await runner.RunAsync(async ct =>
        {
            var response = await blob.DownloadStreamingAsync(new BlobDownloadOptions
            {
                Conditions = new BlobRequestConditions { IfMatch = properties.ETag }
            }, ct).ConfigureAwait(false);
            return response.Value.Content;
        }, identity, cancellationToken).ConfigureAwait(false);
        if (summary.Outcome == FileProcessingOutcome.ExecutionFailed)
            summary.ExecutionResult.ThrowOnError();
    }
}
