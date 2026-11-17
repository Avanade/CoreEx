namespace CoreEx.Azure.Storage.Blobs.Test.Unit;

[TestFixture]
public class BlobReadSourceTests
{
    [TestCase("versionid=2026-01-02T03%3A04%3A05Z", "versionid=2026-01-02T03%3A04%3A05Z")]
    [TestCase("snapshot=2026-01-02T03%3A04%3A05Z", "snapshot=2026-01-02T03%3A04%3A05Z")]
    [TestCase("", "")]
    public async Task Selection_RemovesCredentialsAndPinsEveryFreshRead(string selector, string expectedQuery)
    {
        var blob = new SelectedBlob(new Uri($"https://user:password@example.invalid/container/a.csv?sig=secret&{selector}#private"));
        using var cancellation = new CancellationTokenSource();
        var source = await BlobReadSource.CreateAsync(blob, cancellation.Token);
        source.SourceUri.AbsoluteUri.Should().Be("https://example.invalid/container/a.csv" + (expectedQuery.Length == 0 ? "" : "?" + expectedQuery));
        source.ContentLength.Should().Be(3);
        await using var first = await source.OpenReadAsync(cancellation.Token);
        await using var second = await source.OpenReadAsync(cancellation.Token);
        first.Should().NotBeSameAs(second);
        blob.PropertyTokens.Should().Equal(cancellation.Token);
        blob.ReadTokens.Should().Equal(cancellation.Token, cancellation.Token);
        blob.Conditions.Should().Equal(new ETag("\"selected\""), new ETag("\"selected\""));
    }

    [Test]
    public async Task ChangedRevision_PropagatesWithoutSelectingANewerVersion()
    {
        var blob = new SelectedBlob(new Uri("https://example.invalid/container/a.csv"));
        var source = await BlobReadSource.CreateAsync(blob);
        await using var first = await source.OpenReadAsync();
        blob.ReadFailure = new RequestFailedException(412, "Revision changed.");
        Func<Task> read = async () => { await using var ignored = await source.OpenReadAsync(); };
        (await read.Should().ThrowAsync<RequestFailedException>()).Which.Status.Should().Be(412);
        blob.PropertyTokens.Should().HaveCount(1);
        blob.Conditions.Should().Equal(source.ETag, source.ETag);
    }

    [Test]
    public async Task CancelledRead_PropagatesTheToken()
    {
        var blob = new SelectedBlob(new Uri("https://example.invalid/container/a.csv"));
        var source = await BlobReadSource.CreateAsync(blob);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> read = async () => { await using var ignored = await source.OpenReadAsync(cancellation.Token); };
        await read.Should().ThrowAsync<OperationCanceledException>();
        blob.ReadTokens.Should().Equal(cancellation.Token);
    }

    private sealed class SelectedBlob(Uri uri) : BlobClient
    {
        public override Uri Uri => uri;
        public Exception? ReadFailure { get; set; }
        public List<ETag?> Conditions { get; } = [];
        public List<CancellationToken> PropertyTokens { get; } = [];
        public List<CancellationToken> ReadTokens { get; } = [];

        public override Task<Response<BlobProperties>> GetPropertiesAsync(BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default)
        {
            PropertyTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobProperties(contentLength: 3, eTag: new ETag("\"selected\"")), new StubResponse()));
        }

        public override Task<Response<BlobDownloadStreamingResult>> DownloadStreamingAsync(BlobDownloadOptions? options = null, CancellationToken cancellationToken = default)
        {
            Conditions.Add(options?.Conditions.IfMatch);
            ReadTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (ReadFailure is not null)
                throw ReadFailure;

            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(content: new MemoryStream([1, 2, 3])), new StubResponse()));
        }
    }
}
