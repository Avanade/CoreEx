using CoreEx.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace CoreEx.AspNetCore.Test.Unit;

public class ExecutionContextMiddlewareTests
{
    [Test]
    public void AddMessagesHeader_ResponseAlreadyStarted_DoesNotTouchHeaders()
    {
        // Regression: AddMessagesHeader must not attempt to mutate response headers once the response has started - a real IHeaderDictionary throws
        // InvalidOperationException at that point ("Headers are read-only, response has already started."). UseExecutionContext() is general-purpose
        // middleware applied to the whole pipeline, not just CoreEx WebApi endpoints, so any downstream handler that starts the response itself must not crash it.
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new AlreadyStartedResponseFeature());

        var ec = new ExecutionContext();
        ec.AddInfoMessage("test-message");

        Assert.DoesNotThrow(() => ExecutionContextMiddleware.AddMessagesHeader(context, ec));
        context.Response.Headers.Should().NotContainKey(HttpNames.InfoMessagesHeaderName);
    }

    private sealed class AlreadyStartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }
}
