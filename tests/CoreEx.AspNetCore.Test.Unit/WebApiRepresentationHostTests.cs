namespace CoreEx.AspNetCore.Test.Unit;

public class WebApiRepresentationHostTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task AcceptGuard_RejectsBeforeMutation_UsesKeyedDefaultAndOverride(bool mvc)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers();
        builder.Services.AddMvcWebApi().AddHttpWebApi();
        builder.Services.AddKeyedScoped<Mvc.WebApi>("accept", (_, _) => new Mvc.WebApi { DefaultJsonMediaType = "application/vnd.example+json" });
        builder.Services.AddKeyedScoped<Http.WebApi>("accept", (_, _) => new Http.WebApi { DefaultJsonMediaType = "application/vnd.example+json" });
        await using var app = builder.Build();
        var mutations = 0;
        app.MapPost("/{kind}", (RequestDelegate)HandleAsync);
        async Task HandleAsync(HttpContext context)
        {
            var kind = context.Request.RouteValues["kind"]?.ToString();
            Task<string> MutateAsync(WebApiResponseOptions<string> options, CancellationToken cancellationToken)
            {
                if (kind == "override")
                    options.WithContentType("application/vnd.operation+json");

                if (kind != "unchecked")
                    options.WithAccept();

                mutations++;
                return Task.FromResult("response");
            }

            if (mvc)
            {
                var webApi = kind == "keyed" ? context.RequestServices.GetRequiredKeyedService<Mvc.WebApi>("accept") : context.RequestServices.GetRequiredService<Mvc.WebApi>();
                var result = await webApi.PostAsync<string>(context.Request, MutateAsync, cancellationToken: context.RequestAborted);
                await result.ExecuteResultAsync(new Microsoft.AspNetCore.Mvc.ActionContext(context, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
            }
            else
            {
                var webApi = kind == "keyed" ? context.RequestServices.GetRequiredKeyedService<Http.WebApi>("accept") : context.RequestServices.GetRequiredService<Http.WebApi>();
                var result = await webApi.PostAsync<string>(context.Request, MutateAsync, cancellationToken: context.RequestAborted);
                await result.ExecuteAsync(context);
            }
        }

        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var (kind, accept, expected) in new[]
        {
            ("ordinary", "application/xml", HttpStatusCode.NotAcceptable),
            ("keyed", "application/json", HttpStatusCode.NotAcceptable),
            ("ordinary", "invalid", HttpStatusCode.BadRequest),
            ("keyed", "application/vnd.example+json", HttpStatusCode.Created),
            ("override", "application/vnd.operation+json", HttpStatusCode.Created),
            ("ordinary", "application/json", HttpStatusCode.Created),
            ("unchecked", "application/xml", HttpStatusCode.Created)
        })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/" + kind);
            request.Headers.TryAddWithoutValidation("Accept", accept);
            var before = mutations;
            using var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(expected);
            mutations.Should().Be(before + (expected == HttpStatusCode.Created ? 1 : 0));
            if (kind != "unchecked")
                response.Headers.Vary.Should().Contain("Accept");

            if (expected is HttpStatusCode.NotAcceptable or HttpStatusCode.BadRequest)
            {
                response.Content.Headers.ContentType?.MediaType.Should().Be(MediaTypeNames.Application.ProblemJson);
                (await response.Content.ReadAsStringAsync()).Should().NotContain("System.").And.NotContain("StackTrace");
            }
            else
                response.Content.Headers.ContentType?.MediaType.Should().Be(kind == "unchecked" ? MediaTypeNames.Application.Json : accept);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task KeyedRepresentation_IsolatedFromDefaultsAndMiddleware(bool mvc)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["CoreEx:IncludeExceptionInProblemDetails"] = "false";
        var logs = new HostLogProvider();
        builder.Logging.ClearProviders().AddProvider(logs);
        builder.Services.AddControllers();
        builder.Services.AddMvcWebApi().AddHttpWebApi();
        var specialized = mvc ? (WebApiBase)new SpecializedMvcWebApi() : new SpecializedHttpWebApi();
        var probe = ((IRepresentationTestWebApi)specialized).Probe;
        specialized.DefaultJsonMediaType = "application/vnd.coreex.resource+json";
        specialized.JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        if (mvc)
            builder.Services.AddKeyedScoped<Mvc.WebApi>("specialized", (_, _) => (Mvc.WebApi)specialized);
        else
            builder.Services.AddKeyedScoped<Http.WebApi>("specialized", (_, _) => (Http.WebApi)specialized);

        await using var app = builder.Build();
        app.UseCoreExExceptionHandler();
        app.MapGet("/{kind}/{outcome}", (RequestDelegate)HandleAsync);
        async Task HandleAsync(HttpContext context)
        {
            var keyed = context.Request.RouteValues["kind"]?.ToString() == "keyed";
            var outcome = context.Request.RouteValues["outcome"]?.ToString();
            if (outcome == "outside")
                throw new InvalidOperationException("private diagnostic outside WebApi");

            var value = new { DisplayName = "example" };
            if (mvc)
            {
                var webApi = keyed ? context.RequestServices.GetRequiredKeyedService<Mvc.WebApi>("specialized") : context.RequestServices.GetRequiredService<Mvc.WebApi>();
                var result = await webApi.GetAsync(context.Request, (_, _) =>
                    outcome == "error" ? throw new NotFoundException() : Task.FromResult(value), cancellationToken: context.RequestAborted);
                await result.ExecuteResultAsync(new Microsoft.AspNetCore.Mvc.ActionContext(context, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
            }
            else
            {
                var webApi = keyed ? context.RequestServices.GetRequiredKeyedService<Http.WebApi>("specialized") : context.RequestServices.GetRequiredService<Http.WebApi>();
                var result = await webApi.GetAsync(context.Request, (_, _) =>
                    outcome == "error" ? throw new NotFoundException() : Task.FromResult(value), cancellationToken: context.RequestAborted);
                await result.ExecuteAsync(context);
            }
        }

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var keyedSuccess = await client.GetAsync("/keyed/success");
        keyedSuccess.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.coreex.resource+json");
        (await keyedSuccess.Content.ReadAsStringAsync()).Should().Be("{\"display_name\":\"example\"}");
        using var ordinarySuccess = await client.GetAsync("/ordinary/success");
        ordinarySuccess.Content.Headers.ContentType?.MediaType.Should().Be(MediaTypeNames.Application.Json);
        (await ordinarySuccess.Content.ReadAsStringAsync()).Should().Be("{\"displayName\":\"example\"}");
        using var keyedError = await client.GetAsync("/keyed/error");
        keyedError.StatusCode.Should().Be(HttpStatusCode.NotFound);
        keyedError.Content.Headers.ContentType?.MediaType.Should().Be(RepresentationProbe.MediaType);
        (await keyedError.Content.ReadAsStringAsync()).Should().Be("{\"error_category\":\"not-found\"}");
        probe.RenderCount.Should().Be(1);
        using var ordinaryError = await client.GetAsync("/ordinary/error");
        ordinaryError.Content.Headers.ContentType?.MediaType.Should().Be(MediaTypeNames.Application.ProblemJson);
        using var outside = await client.GetAsync("/keyed/outside");
        outside.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        outside.Content.Headers.ContentType?.MediaType.Should().Be(MediaTypeNames.Application.ProblemJson);
        (await outside.Content.ReadAsStringAsync()).Should().NotContain("private diagnostic");
        probe.RenderCount.Should().Be(1);
        logs.Exceptions.Should().ContainSingle();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Middleware_BypassesDuplicateLoggingAndPreservesRetryAfter(bool customUnkeyed)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddHttpWebApi();
        var specialized = new SpecializedHttpWebApi();
        if (customUnkeyed)
            builder.Services.AddScoped<Http.WebApi>(_ => specialized);

        await using var app = builder.Build();
        app.UseCoreExExceptionHandler();
        app.MapGet("/outside", (HttpContext _) => Task.FromException(new TransientException { RetryAfter = TimeSpan.FromSeconds(7), ShouldBeLogged = true }));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/outside");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter?.Delta.Should().Be(TimeSpan.FromSeconds(7));
        response.Content.Headers.ContentType?.MediaType.Should().Be(customUnkeyed ? RepresentationProbe.MediaType : MediaTypeNames.Application.ProblemJson);
        specialized.Probe.LoggedExceptions.Should().BeEmpty();
        specialized.Probe.RenderCount.Should().Be(customUnkeyed ? 1 : 0);
    }

    private sealed class HostLogProvider : ILoggerProvider
    {
        public List<Exception?> Exceptions { get; } = [];
        public ILogger CreateLogger(string categoryName) => new HostLogger(Exceptions);
        public void Dispose() { }
    }

    private sealed class HostLogger(List<Exception?> exceptions) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
                exceptions.Add(exception);
        }
    }
}
