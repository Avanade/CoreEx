using CoreEx.AspNetCore.Idempotency;
using CoreEx.AspNetCore.Mvc;
using CoreEx.Caching;
using CoreEx.Http;
using Microsoft.AspNetCore.Http;

namespace CoreEx.AspNetCore.Test.Unit;

public class IdempotencyKeyTests
{
    [Test]
    public async Task CreateFromHttpRequestAsync_LargeBody_ComputesStableFingerprint()
    {
        // Regression: the body is hashed in fixed-size chunks read directly from the request stream rather than copied into a second, unbounded, always-in-memory
        // MemoryStream first - must still complete without error and produce the same fingerprint for the same body across separate calls.
        var body = new byte[200_000];
        new Random(42).NextBytes(body);

        async Task<string?> FingerprintAsync()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(body);
            context.Request.ContentLength = body.Length;

            var key = await IdempotencyKey.CreateFromHttpRequestAsync(context, []);
            return key.Fingerprint;
        }

        var fingerprint1 = await FingerprintAsync();
        var fingerprint2 = await FingerprintAsync();

        fingerprint1.Should().NotBeNullOrEmpty();
        fingerprint1.Should().Be(fingerprint2);
    }

    [Test]
    public async Task WriteToHttpResponseAsync_PreservesHeadersNotInCachedSet()
    {
        // Regression: replaying a cached response must not wipe headers already set by earlier middleware for *this* request (Headers.Clear() previously did).
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Response.Headers["X-Correlation-Id"] = "abc123";

        var cached = new IdempotencyKey
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string?[]> { ["Content-Type"] = ["application/json"] },
            Body = new BinaryData("""{"ok":true}""")
        };

        await cached.WriteToHttpResponseAsync(context);

        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be("abc123");
        context.Response.Headers["Content-Type"].ToString().Should().Be("application/json");
    }

    [Test]
    public async Task OnInvokeAsync_SetCookie_ExcludedFromCachedReplay()
    {
        // Regression: Set-Cookie (computed for the original caller) must not be captured into the cache and replayed verbatim to a different caller on a cache hit.
        var cache = new MemoryOnlyHybridCache();
        var provider = new HybridCacheIdempotencyProvider(cache);
        var attribute = new IdempotencyKeyAttribute();
        const string idempotencyKey = "test-key-12345678";

        var context1 = new DefaultHttpContext();
        context1.Request.Method = "POST";
        context1.Request.Headers[HttpNames.IdempotencyKeyHeaderName] = idempotencyKey;
        context1.Response.Body = new MemoryStream();

        await provider.OnInvokeAsync(attribute, context1, ctx =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.Headers["Set-Cookie"] = "session=abc123";
            return ctx.Response.WriteAsync("ok");
        });

        var context2 = new DefaultHttpContext();
        context2.Request.Method = "POST";
        context2.Request.Headers[HttpNames.IdempotencyKeyHeaderName] = idempotencyKey;
        context2.Response.Body = new MemoryStream();

        await provider.OnInvokeAsync(attribute, context2, _ => throw new InvalidOperationException("Should not be invoked - the second request should replay from the cache."));

        context2.Response.Headers.Should().NotContainKey("Set-Cookie");
    }
}
