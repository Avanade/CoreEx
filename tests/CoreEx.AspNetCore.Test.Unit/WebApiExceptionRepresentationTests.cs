namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture(typeof(SpecializedMvcWebApi), typeof(Microsoft.AspNetCore.Mvc.IActionResult))]
[TestFixture(typeof(SpecializedHttpWebApi), typeof(IResult))]
public class WebApiExceptionRepresentationTests<TWebApi, TResult> : WithApiTester<Api.Program> where TWebApi : WebApi<TResult>, IRepresentationTestWebApi, new()
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void UnexpectedErrors_RespectOptIn(bool convert, bool returned)
    {
        var exception = new InvalidOperationException("private diagnostic");
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var webApi = new TWebApi { ConvertUnhandledExceptionsToProblemDetails = convert };
        var result = Test.Type<TWebApi>().Run(_ => webApi.PostWithResultAsync(request, (_, _) =>
            returned ? Task.FromResult(Result.Fail(exception)) : throw exception));

        if (convert)
        {
            result.ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.InternalServerError)
                .AssertContentType(RepresentationProbe.MediaType)
                .GetContent().Should().NotContain("private diagnostic");
            webApi.Probe.Exception.Should().BeSameAs(exception);
            webApi.Probe.Context.Should().BeSameAs(request.HttpContext);
            webApi.Probe.RenderCount.Should().Be(1);
            webApi.Probe.LoggedExceptions.Should().ContainSingle().Which.Should().BeSameAs(exception);
        }
        else
        {
            result.AssertException<InvalidOperationException>();
            webApi.Probe.RenderCount.Should().Be(0);
            webApi.Probe.LoggedExceptions.Should().BeEmpty();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExpectedErrors_PreserveClassificationAndHeaders(bool returned)
    {
        var exception = new TransientException { RetryAfter = TimeSpan.FromSeconds(7), ShouldBeLogged = true };
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var webApi = new TWebApi();
        Test.Type<TWebApi>().Run(_ => webApi.PostWithResultAsync(request, (_, _) =>
            returned ? Task.FromResult(Result.Fail(exception)) : throw exception))
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.ServiceUnavailable)
            .AssertContentType(RepresentationProbe.MediaType).AssertNamedHeader("Retry-After", "7");
        webApi.Probe.ExtendedException.Should().BeSameAs(exception);
        webApi.Probe.LoggedExceptions.Should().ContainSingle();
        webApi.Probe.RenderCount.Should().Be(1);
        webApi.ConvertUnhandledExceptionsToProblemDetails.Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ValidationAndNotFound_ReachRenderer(bool validation)
    {
        Exception exception = validation ? ValidationException.Create("name", "Name is required.") : new NotFoundException();
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        var webApi = new TWebApi();
        Test.Type<TWebApi>().Run(_ => webApi.GetAsync<string>(request, (_, _) => throw exception))
            .ToHttpResponseMessageAssertor(request).Assert(validation ? HttpStatusCode.BadRequest : HttpStatusCode.NotFound)
            .AssertContentType(RepresentationProbe.MediaType);
        webApi.Probe.Exception.Should().BeSameAs(exception);
        webApi.Probe.LoggedExceptions.Should().BeEmpty();
        if (validation)
            webApi.Probe.ExtendedException.Should().BeOfType<ValidationException>().Which.Messages.Should().HaveCount(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Cancellation_PreservesOwnTokenPolicy(bool returned)
    {
        using var source = new CancellationTokenSource();
        var exception = new OperationCanceledException(source.Token);
        var webApi = new TWebApi { ConvertUnhandledExceptionsToProblemDetails = true };
        Test.Type<TWebApi>().Run(_ => webApi.PostWithResultAsync(Test.CreateHttpRequest(HttpMethod.Post), (_, _) =>
            returned ? Task.FromResult(Result.Fail(exception)) : throw exception, cancellationToken: source.Token))
            .AssertException<OperationCanceledException>();
        webApi.Probe.RenderCount.Should().Be(0);
        webApi.Probe.LoggedExceptions.Should().BeEmpty();
    }

    [Test]
    public void Cancellation_UnrelatedToken_IsAnUnexpectedFailure()
    {
        using var source = new CancellationTokenSource();
        using var unrelated = new CancellationTokenSource();
        var webApi = new TWebApi { ConvertUnhandledExceptionsToProblemDetails = true };
        Test.Type<TWebApi>().Run(_ => webApi.PostAsync(Test.CreateHttpRequest(HttpMethod.Post),
            (_, _) => throw new OperationCanceledException(unrelated.Token), cancellationToken: source.Token))
            .ToHttpResponseMessageAssertor().Assert(HttpStatusCode.InternalServerError);
        webApi.Probe.RenderCount.Should().Be(1);
        webApi.Probe.LoggedExceptions.Should().ContainSingle();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RendererFailure_PropagatesWithoutReentry(bool returned)
    {
        var webApi = new TWebApi { ConvertUnhandledExceptionsToProblemDetails = true };
        webApi.Probe.ThrowWhenRendering = true;
        Test.Type<TWebApi>().Run(_ => webApi.PostWithResultAsync(Test.CreateHttpRequest(HttpMethod.Post),
            (_, _) => returned ? Task.FromResult(Result.Fail(new NotFoundException())) : throw new NotFoundException()))
            .AssertException<InvalidOperationException>();
        webApi.Probe.RenderCount.Should().Be(1);
        webApi.Probe.LoggedExceptions.Should().BeEmpty();
    }
}

public interface IRepresentationTestWebApi
{
    RepresentationProbe Probe { get; }
}

public sealed class RepresentationProbe
{
    public const string MediaType = "application/vnd.coreex.error+json";
    public Exception? Exception { get; private set; }
    public IExtendedException? ExtendedException { get; private set; }
    public HttpContext? Context { get; private set; }
    public int RenderCount { get; private set; }
    public bool ThrowWhenRendering { get; set; }
    public List<Exception?> LoggedExceptions { get; } = [];

    public string Render(HttpContext context, Exception exception, IExtendedException? extendedException, JsonSerializerOptions options)
    {
        Exception = exception;
        ExtendedException = extendedException;
        Context = context;
        RenderCount++;
        if (ThrowWhenRendering)
            throw new InvalidOperationException("renderer failed");

        return JsonSerializer.Serialize(new { ErrorCategory = extendedException?.ErrorType ?? "unexpected" }, options);
    }
}

public sealed class RepresentationLogger<T>(RepresentationProbe probe) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => probe.LoggedExceptions.Add(exception);
}

public sealed class SpecializedMvcWebApi : Mvc.WebApi, IRepresentationTestWebApi
{
    public SpecializedMvcWebApi() : this(new RepresentationProbe()) { }
    private SpecializedMvcWebApi(RepresentationProbe probe) : base(logger: new RepresentationLogger<Mvc.WebApi>(probe)) => Probe = probe;
    public RepresentationProbe Probe { get; }

    protected override Microsoft.AspNetCore.Mvc.IActionResult CreateResultForException(HttpContext httpContext, Exception exception, IExtendedException? extendedException)
        => new Microsoft.AspNetCore.Mvc.ContentResult
        {
            Content = Probe.Render(httpContext, exception, extendedException, JsonSerializerOptions),
            ContentType = RepresentationProbe.MediaType,
            StatusCode = (int)(extendedException?.StatusCode ?? HttpStatusCode.InternalServerError)
        };
}

public sealed class SpecializedHttpWebApi : Http.WebApi, IRepresentationTestWebApi
{
    public SpecializedHttpWebApi() : this(new RepresentationProbe()) { }
    private SpecializedHttpWebApi(RepresentationProbe probe) : base(logger: new RepresentationLogger<Http.WebApi>(probe)) => Probe = probe;
    public RepresentationProbe Probe { get; }

    protected override IResult CreateResultForException(HttpContext httpContext, Exception exception, IExtendedException? extendedException)
        => Microsoft.AspNetCore.Http.Results.Content(Probe.Render(httpContext, exception, extendedException, JsonSerializerOptions), RepresentationProbe.MediaType,
            statusCode: (int)(extendedException?.StatusCode ?? HttpStatusCode.InternalServerError));
}
