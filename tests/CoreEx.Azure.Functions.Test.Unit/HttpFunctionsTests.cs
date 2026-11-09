namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class HttpFunctionsTests
{
    [TestCase(null)]
    [TestCase("Workflow-AbC")]
    public async Task Correlation_DeferredResponseEchoesInput(string? correlation)
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        var request = test.CreateHttpRequest(HttpMethod.Get, "context");
        var responseFeature = new DeferredResponseFeature();
        request.HttpContext.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(responseFeature);
        if (correlation is not null)
            request.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = correlation;

        var invocation = test.HttpTrigger<HttpFunctions>().Run(f => f.ReadContext(request, CancellationToken.None));
        invocation.ToHttpResponseMessageAssertor(request).AssertOK();
        CoreEx.ExecutionContext.HasCurrent.Should().BeFalse();
        await responseFeature.CommitAsync();
        request.HttpContext.Response.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName].ToString().Should().Be(correlation ?? "");
        CoreEx.ExecutionContext.HasCurrent.Should().BeFalse();
    }

    [Test]
    public async Task DisabledCorrelation_IgnoresInvalidHeaderAndDoesNotOverrideResponse()
    {
        using var test = FunctionTester.Create<HttpFunctions>(additionalConfiguration: new Dictionary<string, string?> { ["CoreEx:Correlation:Enabled"] = "false" });
        var request = test.CreateHttpRequest(HttpMethod.Get, "messages");
        var responseFeature = new DeferredResponseFeature();
        responseFeature.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "application";
        request.HttpContext.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(responseFeature);
        request.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "invalid value";
        test.HttpTrigger<HttpFunctions>().WithNoRouteCheck().Run(f => f.Messages(request, CancellationToken.None)).ToHttpResponseMessageAssertor(request).AssertOK();
        await responseFeature.CommitAsync();
        request.HttpContext.Response.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName].ToString().Should().Be("application");
        request.HttpContext.Response.Headers[CoreEx.Http.HttpNames.InfoMessagesHeaderName].ToString().Should().Contain("Processed.");
        CoreEx.ExecutionContext.HasCurrent.Should().BeFalse();
    }

    [TestCase("bad value")]
    [TestCase("")]
    [TestCase("duplicate")]
    [TestCase("oversized")]
    public void Correlation_InvalidHeaderIsRenderedBeforeFunction(string supplied)
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        var request = test.CreateHttpRequest(HttpMethod.Get, "context");
        using var body = new MemoryStream();
        request.HttpContext.Response.Body = body;
        request.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = supplied == "duplicate"
            ? new Microsoft.Extensions.Primitives.StringValues(["a", "b"])
            : supplied == "oversized" ? new string('x', 257) : supplied;
        test.HttpTrigger<HttpFunctions>()
            .Run(f => f.ReadContext(request, CancellationToken.None))
            .SnapshotHttpResponse(request.HttpContext.Response)
            .AssertBadRequest();
        request.HttpContext.Response.Headers.ContainsKey(CoreEx.Http.HttpNames.CorrelationIdHeaderName).Should().BeFalse();
        CoreEx.ExecutionContext.HasCurrent.Should().BeFalse();
    }

    [TestCase("late", "late")]
    [TestCase("clear", null)]
    public async Task Correlation_DeferredResponseUsesFinalInvocationValue(string change, string? expected)
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        var request = test.CreateHttpRequest(HttpMethod.Get, "messages");
        var responseFeature = new DeferredResponseFeature();
        responseFeature.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "cached-original";
        request.HttpContext.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(responseFeature);
        request.QueryString = new Microsoft.AspNetCore.Http.QueryString($"?correlation={change}");
        request.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "initial";
        test.HttpTrigger<HttpFunctions>().WithNoRouteCheck().Run(f => f.Messages(request, CancellationToken.None)).ToHttpResponseMessageAssertor(request).AssertOK();
        CoreEx.ExecutionContext.HasCurrent.Should().BeFalse();
        await responseFeature.CommitAsync();
        request.HttpContext.Response.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName].ToString().Should().Be(expected ?? "");
        CoreEx.ExecutionContext.HasCurrent.Should().BeFalse();
    }

    [Test]
    public void Context_IsScopedAndAvailableAcrossAwait()
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        var request = test.CreateHttpRequest(HttpMethod.Get, "context");
        var response = test.HttpTrigger<HttpFunctions>()
            .Run(f => f.ReadContext(request, CancellationToken.None))
            .ToHttpResponseMessageAssertor(request)
            .AssertOK();

        response.AssertValue(new { InvocationId = request.HttpContext.RequestServices.GetRequiredService<CoreEx.ExecutionContext>().Attributes["invocation-id"], SameContext = true, SameService = true });
    }

    [Test]
    public void Create_UsesWebApiParsingAndLocation()
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        var request = test.CreateJsonHttpRequest(HttpMethod.Post, "values", "example");
        test.HttpTrigger<HttpFunctions>()
            .Run(f => f.Create(request, CancellationToken.None))
            .ToHttpResponseMessageAssertor(request)
            .AssertCreated()
            .AssertNamedHeader("Location", "/api/values/example")
            .AssertValue("example");
    }

    [Test]
    public void MissingBody_UsesProblemDetails()
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        test.HttpTrigger<HttpFunctions>()
            .Run(f => f.Create(test.CreateHttpRequest(HttpMethod.Post, "values"), CancellationToken.None))
            .ToHttpResponseMessageAssertor()
            .AssertBadRequest();
    }

    [TestCase("known", HttpStatusCode.NotFound)]
    [TestCase("unexpected", HttpStatusCode.InternalServerError)]
    public async Task Failure_UsesSafeProblemDetails(string kind, HttpStatusCode statusCode)
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        var request = test.CreateHttpRequest(HttpMethod.Get, $"failure/{kind}");
        request.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName] = "failure-workflow";
        var responseFeature = new DeferredResponseFeature();
        request.HttpContext.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(responseFeature);
        var response = test.HttpTrigger<HttpFunctions>()
            .Run(f => f.Failure(request, kind, CancellationToken.None))
            .ToHttpResponseMessageAssertor(request)
            .Assert(statusCode);

        CoreEx.ExecutionContext.HasCurrent.Should().BeFalse();
        await responseFeature.CommitAsync();
        request.HttpContext.Response.Headers[CoreEx.Http.HttpNames.CorrelationIdHeaderName].ToString().Should().Be("failure-workflow");
        response.AssertContentType("application/problem+json");
        var problem = response.GetValue<JsonElement>("application/problem+json");
        problem.GetProperty("status").GetInt32().Should().Be((int)statusCode);
        if (kind == "unexpected")
            problem.ToString().Should().NotContain("Internal diagnostic");
    }

    [Test]
    public void Cancellation_Propagates()
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        test.HttpTrigger<HttpFunctions>()
            .Run(f => f.ReadContext(test.CreateHttpRequest(HttpMethod.Get, "context"), cancellation.Token))
            .AssertException<TaskCanceledException>();
    }

    [Test]
    public void NonHttp_UsesSameContextMiddleware()
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        test.Trigger<TimerFunctions>().Run(f => f.Scheduled(new(), CancellationToken.None)).AssertSuccess();
        test.Trigger<TimerFunctions>().Run(f => f.FailingScheduled(new(), CancellationToken.None)).AssertException<InvalidOperationException>();
    }

    // DefaultHttpContext does not execute OnStarting; model response commit after deferred result rendering and execution-context reset.
    private sealed class DeferredResponseFeature : Microsoft.AspNetCore.Http.Features.HttpResponseFeature
    {
        private readonly Stack<(Func<object, Task> Callback, object State)> _callbacks = new();
        private bool _hasStarted;

        public override bool HasStarted => _hasStarted;

        public override void OnStarting(Func<object, Task> callback, object state) => _callbacks.Push((callback, state));

        public async Task CommitAsync()
        {
            while (_callbacks.TryPop(out var callback))
                await callback.Callback(callback.State);

            _hasStarted = true;
        }
    }
}
