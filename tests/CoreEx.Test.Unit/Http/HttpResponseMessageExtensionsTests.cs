using System.Net;
using System.Net.Http.Headers;

namespace CoreEx.Test.Unit.Http;

[TestFixture]
public class HttpResponseMessageExtensionsTests
{
    [Test]
    public async Task ToProblemDetailsAsync_OperationCanceled_Propagates_NotSwallowed()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new ThrowingContent(new OperationCanceledException(), "application/problem+json")
        };

        Func<Task> act = async () => await response.ToProblemDetailsAsync();
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task ToProblemDetailsAsync_OtherException_StillSwallowedAsNotProblemDetails()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new ThrowingContent(new InvalidOperationException("boom"), "application/problem+json")
        };

        var result = await response.ToProblemDetailsAsync();
        result.Should().BeNull();
    }

    private sealed class ThrowingContent : HttpContent
    {
        private readonly Exception _exception;

        public ThrowingContent(Exception exception, string mediaType)
        {
            _exception = exception;
            Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw _exception;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) => throw _exception;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
