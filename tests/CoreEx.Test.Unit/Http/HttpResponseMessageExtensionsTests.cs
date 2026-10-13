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

    private class Widget
    {
        public string? Name { get; set; }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, new MediaTypeHeaderValue("application/json")) };

    private static HttpResponseMessage Empty(HttpStatusCode status = HttpStatusCode.OK) => new(status);

    [Test]
    public async Task ToResultAsync_Value_ReturnsValue()
    {
        var r = await Json("{\"name\":\"abc\"}").ToResultAsync<Widget>();
        r.IsSuccess.Should().BeTrue();
        r.Value.Name.Should().Be("abc");
    }

    [Test]
    public async Task ToResultAsync_NullBody_IsFailure()
    {
        var r = await Json("null").ToResultAsync<Widget>();
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<HttpRequestException>();
    }

    [Test]
    public async Task ToResultAsync_EmptyBody_IsFailure()
    {
        var r = await Empty(HttpStatusCode.NoContent).ToResultAsync<Widget>();
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<HttpRequestException>();
        r.Error.Message.Should().Contain("The response content was empty or null");
        r.Error.Message.Should().NotContain("Response status code does not indicate success");
    }

    [Test]
    public async Task ToResultAsync_NotSuccess_IsFailure()
    {
        var r = await Empty(HttpStatusCode.InternalServerError).ToResultAsync<Widget>();
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<HttpRequestException>();
    }

    [Test]
    public async Task ToResultOrDefaultAsync_Value_ReturnsValue()
    {
        var r = await Json("{\"name\":\"abc\"}").ToResultOrDefaultAsync<Widget>();
        r.IsSuccess.Should().BeTrue();
        r.Value!.Name.Should().Be("abc");
    }

    [Test]
    public async Task ToResultOrDefaultAsync_NullBody_IsSuccessWithNull()
    {
        var r = await Json("null").ToResultOrDefaultAsync<Widget>();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeNull();
    }

    [Test]
    public async Task ToResultOrDefaultAsync_EmptyBody_IsSuccessWithNull()
    {
        var r = await Empty(HttpStatusCode.NoContent).ToResultOrDefaultAsync<Widget>();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeNull();
    }

    [Test]
    public async Task ToResultOrDefaultAsync_NotSuccess_IsFailure()
    {
        var r = await Empty(HttpStatusCode.InternalServerError).ToResultOrDefaultAsync<Widget>();
        r.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ToResultOrDefaultAsync_NullContent_IsSuccessWithNull()
    {
        var r = await Empty(HttpStatusCode.OK).ToResultOrDefaultAsync<Widget>();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeNull();
    }

    [Test]
    public async Task ToStringResultAsync_Value_ReturnsValue()
    {
        var r = await new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("hello world") }.ToStringResultAsync();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be("hello world");
    }

    [Test]
    public async Task ToStringResultAsync_NullContent_IsSuccessWithNull()
    {
        var r = await Empty(HttpStatusCode.OK).ToStringResultAsync();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeNull();
    }

    [Test]
    public async Task ToStringResultAsync_EmptyString_IsSuccessWithNull()
    {
        var r = await new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) }.ToStringResultAsync();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeNull();
    }

    [Test]
    public async Task ToStringResultAsync_NoContent_IsSuccessWithNull()
    {
        var r = await Empty(HttpStatusCode.NoContent).ToStringResultAsync();
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeNull();
    }

    [Test]
    public async Task ToStringResultAsync_NotSuccess_IsFailure()
    {
        var r = await Empty(HttpStatusCode.InternalServerError).ToStringResultAsync();
        r.IsFailure.Should().BeTrue();
        r.Error.Should().BeOfType<HttpRequestException>();
    }

    [Test]
    public async Task GetStringAsync_Value_ReturnsValue()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("test-data") };
        var value = await response.GetStringAsync();
        value.Should().Be("test-data");
    }

    [Test]
    public async Task GetStringAsync_NullContent_ReturnsNull()
    {
        var value = await Empty(HttpStatusCode.OK).GetStringAsync();
        value.Should().BeNull();
    }

    [Test]
    public async Task GetStringAsync_EmptyString_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
        var value = await response.GetStringAsync();
        value.Should().BeNull();
    }

    [Test]
    public async Task GetStringAsync_NoContent_ReturnsNull()
    {
        var value = await Empty(HttpStatusCode.NoContent).GetStringAsync();
        value.Should().BeNull();
    }

    [Test]
    public async Task GetStringAsync_NotSuccess_Throws()
    {
        var act = async () => await Empty(HttpStatusCode.InternalServerError).GetStringAsync();
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task GetValueAsync_Value_ReturnsValue()
        => (await Json("{\"name\":\"abc\"}").GetValueAsync<Widget>()).Name.Should().Be("abc");

    [Test]
    public async Task GetValueAsync_NullBody_Throws()
    {
        var act = async () => await Json("null").GetValueAsync<Widget>();
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task GetValueOrDefaultAsync_NullBody_ReturnsNull()
        => (await Json("null").GetValueOrDefaultAsync<Widget>()).Should().BeNull();

    [Test]
    public async Task GetValueOrDefaultAsync_NotSuccess_Throws()
    {
        var act = async () => await Empty(HttpStatusCode.InternalServerError).GetValueOrDefaultAsync<Widget>();
        await act.Should().ThrowAsync<HttpRequestException>();
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
