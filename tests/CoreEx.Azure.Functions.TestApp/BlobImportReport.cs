namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Defines the small JSON file shape used by the integration proof.
/// </summary>
/// <param name="Name">The required name.</param>
public sealed record BlobImportRequest(string? Name);

/// <summary>
/// Describes the processing outcome for one observed blob revision.
/// </summary>
/// <param name="BlobName">The source blob name.</param>
/// <param name="ETag">The revision read under an IfMatch condition.</param>
/// <param name="Status">The accepted or rejected content outcome.</param>
/// <param name="Name">The accepted name, when present.</param>
public sealed record BlobImportReport(string BlobName, string ETag, string Status, string? Name)
{
    /// <summary>
    /// Derives an unambiguous, bounded output name from the source name and observed revision.
    /// </summary>
    /// <param name="blobName">The source blob name.</param>
    /// <param name="etag">The source revision.</param>
    /// <returns>The deterministic report blob name.</returns>
    public static string GetReportName(string blobName, string etag)
        => $"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { blobName, etag }))))}.json";
}
