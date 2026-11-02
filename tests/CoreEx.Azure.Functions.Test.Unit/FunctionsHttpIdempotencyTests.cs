namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class FunctionsHttpIdempotencyTests
{
    [Test]
    public void FunctionInvocation_ProducesRenderedResponse()
    {
        using var test = FunctionTester.Create<IdempotencyFunctions>();
        var id = Runtime.NewGuid().ToString("N");
        var request = test.CreateJsonHttpRequest(HttpMethod.Post, $"idempotency/{id}", "example");
        using var body = new MemoryStream();
        request.HttpContext.Response.Body = body;
        request.Headers["Idempotency-Key"] = id;
        var invocation = test.HttpTrigger<IdempotencyFunctions>()
            .Run(f => f.CreateIdempotent(request, id, CancellationToken.None));
        var position = body.Position;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            invocation.SnapshotHttpResponse(request.HttpContext.Response)
                .AssertCreated()
                .AssertNamedHeader("Location", $"/api/idempotency/{id}")
                .AssertValue(new IdempotencyResponse(id, "example", 1));
            request.HttpContext.Response.Body.Should().BeSameAs(body);
            body.CanRead.Should().BeTrue();
            body.Position.Should().Be(position);
        }

        request.HttpContext.RequestServices.GetRequiredService<IdempotencyOperationService>().GetCount(id).Should().Be(1);
    }

    [Test]
    public async Task ResponseIsRenderedInsideCapture_AndReplayDoesNotInvokeApplication()
    {
        using var services = CreateServices();
        var webApi = new WebApi();
        var first = CreateRequest(services);
        var calls = 0;
        var result = await webApi.FunctionWithIdempotencyAsync(first, _ =>
        {
            calls++;
            return Task.FromResult<IActionResult>(new ContentResult { Content = "rendered once", StatusCode = 201, ContentType = "text/plain" });
        });

        result.Should().BeOfType<EmptyResult>();
        await result.ExecuteResultAsync(new ActionContext { HttpContext = first.HttpContext });
        ReadBody(first).Should().Be("rendered once");
        first.HttpContext.Response.StatusCode.Should().Be(201);
        var replay = CreateRequest(services);
        (await webApi.FunctionWithIdempotencyAsync(replay, _ => throw new AssertionException("Replay must not invoke application work."))).Should().BeOfType<EmptyResult>();
        ReadBody(replay).Should().Be("rendered once");
        replay.HttpContext.Response.StatusCode.Should().Be(201);
        calls.Should().Be(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ApplicationOrRendererFailure_PropagatesOnceAndAllowsRetry(bool rendering)
    {
        using var services = CreateServices();
        var webApi = new WebApi { ConvertUnhandledExceptionsToProblemDetails = true };
        var first = CreateRequest(services);
        var originalBody = first.HttpContext.Response.Body;
        var error = new InvalidOperationException("The original failure.");
        var result = new FailingResult(error);
        Func<Task> execute = () => webApi.FunctionWithIdempotencyAsync(first, _ => rendering ? Task.FromResult<IActionResult>(result) : Task.FromException<IActionResult>(error));
        (await execute.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
        first.HttpContext.Response.Body.Should().BeSameAs(originalBody);
        result.Executions.Should().Be(rendering ? 1 : 0);

        var retry = CreateRequest(services);
        await webApi.FunctionWithIdempotencyAsync(retry, _ => Task.FromResult<IActionResult>(new ContentResult { Content = "retry" }));
        ReadBody(retry).Should().Be("retry");
    }

    [Test]
    public async Task CancellationDuringApplication_PropagatesAndAllowsRetry()
    {
        using var services = CreateServices();
        using var cancellation = new CancellationTokenSource();
        var webApi = new WebApi { ConvertUnhandledExceptionsToProblemDetails = true };
        var error = new OperationCanceledException(cancellation.Token);
        Func<Task> execute = () => webApi.FunctionWithIdempotencyAsync(CreateRequest(services), _ =>
        {
            cancellation.Cancel();
            return Task.FromException<IActionResult>(error);
        }, cancellationToken: cancellation.Token);
        (await execute.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(error);

        var retry = CreateRequest(services);
        await webApi.FunctionWithIdempotencyAsync(retry, _ => Task.FromResult<IActionResult>(new ContentResult { Content = "retry" }));
        ReadBody(retry).Should().Be("retry");
    }

    [Test]
    public async Task PreProcessingFailure_UsesSuppliedWebApiRenderer()
    {
        using var services = CreateServices();
        var request = CreateRequest(services);
        request.Headers.Remove("Idempotency-Key");
        var webApi = new CustomWebApi();
        var result = await webApi.FunctionWithIdempotencyAsync(request, _ => throw new AssertionException("A missing required key must prevent application execution."), isRequired: true);
        result.Should().BeOfType<ContentResult>().Which.Content.Should().Be("custom error");
        webApi.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    public void CreateErrorResult_PreservesUnhandledAndCancellationPolicy()
    {
        using var services = CreateServices();
        var request = CreateRequest(services);
        var error = new InvalidOperationException("Private diagnostic.");
        var webApi = new WebApi();
        Action unhandled = () => webApi.CreateErrorResult(request, error);
        unhandled.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(error);
        webApi.ConvertUnhandledExceptionsToProblemDetails = true;
        webApi.CreateErrorResult(request, error).Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().NotContain(error.Message);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceled = new OperationCanceledException(cancellation.Token);
        Action cancel = () => webApi.CreateErrorResult(request, canceled, cancellation.Token);
        cancel.Should().Throw<OperationCanceledException>().Which.Should().BeSameAs(canceled);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddMvc();
        services.AddSingleton<CoreEx.Caching.IHybridCache>(new CoreEx.Caching.MemoryOnlyHybridCache());
        services.AddHybridCacheIdempotencyProvider();
        return services.BuildServiceProvider();
    }

    private static HttpRequest CreateRequest(IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        context.Request.Method = "POST";
        context.Request.Path = "/idempotency";
        context.Request.Headers["Idempotency-Key"] = "test-key-12345678";
        return context.Request;
    }

    private static string ReadBody(HttpRequest request)
    {
        var stream = (MemoryStream)request.HttpContext.Response.Body;
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class FailingResult(Exception error) : IActionResult
    {
        public int Executions { get; private set; }

        public Task ExecuteResultAsync(ActionContext context)
        {
            Executions++;
            return Task.FromException(error);
        }
    }

    private sealed class CustomWebApi : WebApi
    {
        public Exception? Error { get; private set; }

        protected override IActionResult CreateResultForException(HttpContext httpContext, Exception exception, CoreEx.Abstractions.IExtendedException? extendedException)
        {
            Error = exception;
            return new ContentResult { Content = "custom error", StatusCode = 400 };
        }
    }
}
