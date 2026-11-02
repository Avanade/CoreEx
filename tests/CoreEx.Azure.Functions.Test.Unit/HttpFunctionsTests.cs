namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class HttpFunctionsTests
{
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
    public void Failure_UsesSafeProblemDetails(string kind, HttpStatusCode statusCode)
    {
        using var test = FunctionTester.Create<HttpFunctions>();
        var response = test.HttpTrigger<HttpFunctions>()
            .Run(f => f.Failure(test.CreateHttpRequest(HttpMethod.Get, $"failure/{kind}"), kind, CancellationToken.None))
            .ToHttpResponseMessageAssertor()
            .Assert(statusCode);

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
}
