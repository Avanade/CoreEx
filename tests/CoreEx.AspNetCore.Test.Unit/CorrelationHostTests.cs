namespace CoreEx.AspNetCore.Test.Unit;

[NonParallelizable]
public class CorrelationHostTests
{
    [TestCase(null, null)]
    [TestCase("Workflow-AbC", "Workflow-AbC")]
    [TestCase("initial", "replacement")]
    [TestCase("clear", null)]
    public async Task NonWebApiEndpoint_CapturesAndReturnsEffectiveValue(string? supplied, string? expected)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddExecutionContext();
        await using var app = builder.Build();
        app.UseExecutionContext();
        app.Run(async context =>
        {
            var ec = CoreEx.ExecutionContext.Current;
            ec.CorrelationId.Should().Be(supplied);
            await Task.Yield();
            if (supplied == "initial")
                ec.CorrelationId = "replacement";
            else if (supplied == "clear")
                ec.CorrelationId = null;

            await context.Response.WriteAsync("ok");
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (supplied is not null)
            request.Headers.Add(CoreEx.Http.HttpNames.CorrelationIdHeaderName, supplied);

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReadCorrelation(response).Should().Be(expected);
    }

    [TestCase("a b")]
    [TestCase("a,b")]
    [TestCase("\u00e9")]
    [TestCase("duplicate")]
    [TestCase("oversized")]
    public async Task InvalidHeader_Returns400BeforeEndpointWithoutWebApi(string supplied)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddExecutionContext();
        await using var app = builder.Build();
        var called = false;
        app.UseExecutionContext();
        app.Run(context => { called = true; return context.Response.WriteAsync("wrong"); });
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CoreEx.Http.HttpNames.CorrelationIdHeaderName,
            supplied == "duplicate" ? ["a", "b"] : [supplied == "oversized" ? new string('x', 257) : supplied]);
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ReadCorrelation(response).Should().BeNull();
        called.Should().BeFalse();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task ServerSpan_UsesEffectiveCorrelationWithoutRewritingTraceIdentity()
    {
        var stopped = new TaskCompletionSource<System.Diagnostics.Activity>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity => stopped.TrySetResult(activity)
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddExecutionContext();
        await using var app = builder.Build();
        app.UseExecutionContext();
        app.Run(async context =>
        {
            System.Diagnostics.Activity.Current?.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("initial");
            CoreEx.ExecutionContext.Current.CorrelationId = "final";
            await context.Response.WriteAsync("ok");
        });
        await app.StartAsync();
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, app.Urls.Single());
        request.Headers.Add(CoreEx.Http.HttpNames.CorrelationIdHeaderName, "initial");
        request.Headers.Add("traceparent", "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01");
        using var response = await client.SendAsync(request);
        ReadCorrelation(response).Should().Be("final");
        var activity = await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10));
        activity.GetTagItem(CoreEx.Diagnostics.Correlation.ActivityTagName).Should().Be("final");
        activity.TraceId.ToString().Should().Be("0123456789abcdef0123456789abcdef");
        activity.ParentSpanId.ToString().Should().Be("0123456789abcdef");
        activity.Baggage.Should().BeEmpty();
    }

    [Test]
    public async Task EmptyWireHeader_IsRejectedByRealHttpServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddExecutionContext();
        await using var app = builder.Build();
        app.UseExecutionContext();
        app.Run(context => context.Response.WriteAsync("wrong"));
        await app.StartAsync();
        var address = new Uri(app.Urls.Single());
        using var socket = new System.Net.Sockets.TcpClient();
        await socket.ConnectAsync("127.0.0.1", address.Port);
        await using var stream = socket.GetStream();
        await stream.WriteAsync(System.Text.Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: localhost\r\nX-Correlation-Id:\r\nConnection: close\r\n\r\n"));
        using var reader = new StreamReader(stream);
        var response = await reader.ReadToEndAsync();
        response.Should().StartWith("HTTP/1.1 400").And.NotContain("wrong");
    }

    [Test]
    public async Task ExceptionInsideExecutionBoundary_ReturnsCorrelation()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddExecutionContext().AddHttpWebApi();
        await using var app = builder.Build();
        app.UseExecutionContext();
        app.UseCoreExExceptionHandler();
        app.Run(_ => throw new InvalidOperationException("Private diagnostics."));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(CoreEx.Http.HttpNames.CorrelationIdHeaderName, "failure");
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ReadCorrelation(response).Should().Be("failure");
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Private diagnostics.");
    }

    [Test]
    public async Task FactoryAndCallback_PrecedenceAreExplicit()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddExecutionContext((Func<IServiceProvider, CoreEx.ExecutionContext>)(_ => new() { CorrelationId = "factory" }));
        await using var app = builder.Build();
        app.UseExecutionContext((context, ec) =>
        {
            ec.CorrelationId.Should().Be(context.Request.Headers.ContainsKey(CoreEx.Http.HttpNames.CorrelationIdHeaderName) ? "supplied" : "factory");
            ec.CorrelationId = "callback";
            return Task.CompletedTask;
        });
        app.Run(context => context.Response.WriteAsync("ok"));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var absent = await client.GetAsync("/");
        ReadCorrelation(absent).Should().Be("callback");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(CoreEx.Http.HttpNames.CorrelationIdHeaderName, "supplied");
        using var supplied = await client.SendAsync(request);
        ReadCorrelation(supplied).Should().Be("callback");
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task Idempotency_ExecutionBoundaryOwnsCurrentAndLegacyReplayCorrelation(bool cacheCorrelation, bool legacy)
    {
        var cache = new CoreEx.Caching.MemoryOnlyHybridCache();
        var provider = new Idempotency.HybridCacheIdempotencyProvider(cache);
        if (cacheCorrelation)
            provider.HttpResponseHeadersToExcludeFromCache.Remove(CoreEx.Http.HttpNames.CorrelationIdHeaderName);

        if (legacy)
        {
            var original = new DefaultHttpContext();
            original.Request.Method = "POST";
            original.Request.Path = "/";
            var cached = await Idempotency.IdempotencyKey.CreateFromHttpRequestAsync(original, provider.HttpRequestHeadersToIncludeInFingerprint);
            cached.Status = Idempotency.IdempotencyStatus.CompletedAndReplayable;
            cached.Body = BinaryData.FromString("once");
            cached.Headers = new Dictionary<string, string?[]> { ["x-correlation-id"] = ["original"] };
            await cache.SetByKeyAsync("Idempotency:correlation-key", cached);
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddExecutionContext();
        await using var app = builder.Build();
        app.UseExecutionContext();
        app.Use((context, next) => provider.OnInvokeAsync(new Mvc.IdempotencyKeyAttribute(), context, _ => next()));
        var calls = 0;
        app.Run(async context =>
        {
            calls++;
            context.Response.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "original";
            await context.Response.WriteAsync("once");
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var correlation in new string?[] { "A", "B", null })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Headers.Add(CoreEx.Http.HttpNames.IdempotencyKeyHeaderName, "correlation-key");
            if (correlation is not null)
                request.Headers.Add(CoreEx.Http.HttpNames.CorrelationIdHeaderName, correlation);

            using var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            ReadCorrelation(response).Should().Be(correlation);
            (await response.Content.ReadAsStringAsync()).Should().Be("once");
        }
        calls.Should().Be(legacy ? 0 : 1);
    }

    [Test]
    public async Task Idempotency_GenericReplayHonorsCachedHeaders()
    {
        var replay = new DefaultHttpContext();
        replay.Response.Body = new MemoryStream();
        replay.Response.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "current";
        await new Idempotency.IdempotencyKey
        {
            Body = BinaryData.FromString("legacy"),
            Headers = new Dictionary<string, string?[]> { ["x-correlation-id"] = ["original"] }
        }.WriteToHttpResponseAsync(replay);
        replay.Response.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName].ToString().Should().Be("original");
    }

    [Test]
    public async Task Idempotency_ConfiguredFingerprintIncludesCorrelation()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "A";
        var first = await Idempotency.IdempotencyKey.CreateFromHttpRequestAsync(context, [CoreEx.Http.HttpNames.CorrelationIdHeaderName]);
        context.Request.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "B";
        var second = await Idempotency.IdempotencyKey.CreateFromHttpRequestAsync(context, [CoreEx.Http.HttpNames.CorrelationIdHeaderName]);
        second.Fingerprint.Should().NotBe(first.Fingerprint);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Idempotency_ResponseCacheHonorsConfiguredExclusions(bool cacheCorrelation)
    {
        var provider = new Idempotency.HybridCacheIdempotencyProvider(new CoreEx.Caching.MemoryOnlyHybridCache());
        if (cacheCorrelation)
            provider.HttpResponseHeadersToExcludeFromCache.Remove(CoreEx.Http.HttpNames.CorrelationIdHeaderName);

        for (var i = 0; i < 2; i++)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Headers[CoreEx.Http.HttpNames.IdempotencyKeyHeaderName] = "configured-cache";
            context.Response.Body = new MemoryStream();
            await provider.OnInvokeAsync(new Mvc.IdempotencyKeyAttribute(), context, ctx =>
            {
                i.Should().Be(0);
                ctx.Response.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "original";
                return ctx.Response.WriteAsync("once");
            });
            if (i == 1)
                context.Response.Headers.ContainsKey(CoreEx.Http.HttpNames.CorrelationIdHeaderName).Should().Be(cacheCorrelation);
        }
    }

    private static string? ReadCorrelation(HttpResponseMessage response)
        => response.Headers.TryGetValues(CoreEx.Http.HttpNames.CorrelationIdHeaderName, out var values) ? values.Single() : null;
}
