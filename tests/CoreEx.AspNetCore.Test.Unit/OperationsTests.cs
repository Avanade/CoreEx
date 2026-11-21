namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture]
public class OperationsTests : WithApiTester<Api.Program>
{
    private static readonly CoreEx.Security.AuthenticationUser _owner = new() { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "owner" };

    /// <summary>
    /// Provides a fixed UTC time for work-store clock tests.
    /// </summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Test]
    public async Task Status_RetryAfterUsesWorkProviderTime()
    {
        using var test = ApiTester.Create<Api.Program>();
        var now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var work = new CoreEx.Hosting.Work.WorkOrchestrator(new CoreEx.Hosting.Work.InMemoryWorkProvider(timeProvider: new FixedTimeProvider(now)));
        test.ConfigureServices(services =>
        {
            services.AddSingleton(work);
            services.AddExecutionContext((_, ec) => ec.User = _owner);
        });
        var domain = test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName;
        var key = OperationKey.ForDomain(domain, "webapi-operations", "retry-after-clock");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<Api.Entities.Person>(key) with { User = _owner })).IsSuccess.Should().BeTrue();

        var response = test.Http().Run(HttpMethod.Get, "/operations/retry-after-clock").Assert(HttpStatusCode.OK);
        response.Response.Headers.RetryAfter.ThrowIfNull().ToString().Should().Be("1");
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public async Task Status_UsesExecutionUserAndConditionalHeaders(int locationMode)
    {
        using var test = ApiTester.Create<Api.Program>();
        var work = new CoreEx.Hosting.Work.WorkOrchestrator(new CoreEx.Hosting.Work.InMemoryWorkProvider());
        test.ConfigureServices(services =>
        {
            services.AddSingleton(work);
            services.AddExecutionContext((_, ec) => ec.User = _owner);
            services.AddHttpWebApi((_, webApi) =>
            {
                webApi.UseAbsolutePaths = locationMode == 1;
                webApi.BaseAddress = locationMode == 2 ? new Uri("https://public.example/gateway/") : null;
            });
        });
        var domain = test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName;
        var key = OperationKey.ForDomain(domain, "webapi-operations", "status-test");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<Api.Entities.Person>(key) with { User = _owner })).IsSuccess.Should().BeTrue();

        var response = test.Http().Run(HttpMethod.Get, "/operations/status-test").Assert(HttpStatusCode.OK);
        using var status = JsonDocument.Parse(response.GetContent()!);
        status.RootElement.GetProperty("status").GetString().Should().Be("not-started");
        var etag = response.Response.Headers.ETag.ThrowIfNull().ToString();

        test.Http().Run(HttpMethod.Get, "/operations/status-test", r => { r.Headers.TryAddWithoutValidation("If-None-Match", $"\"other\", W/{etag}"); })
            .Assert(HttpStatusCode.NotModified);
        test.Http().Run(HttpMethod.Get, "/operations/status-test", r => { r.Headers.TryAddWithoutValidation("If-None-Match", "*"); })
            .Assert(HttpStatusCode.NotModified);
        test.Http().Run(HttpMethod.Get, "/operations/status-test", r => { r.Headers.TryAddWithoutValidation("If-None-Match", "\"other\""); })
            .Assert(HttpStatusCode.OK);

        var started = await work.StartAsync(key, "worker");
        (await work.CompleteAsync(started.Lease.ThrowIfNull(), "/products/123?v=2#details")).Outcome.Should().Be(CoreEx.Hosting.Work.WorkTransitionOutcome.Applied);
        var completed = test.Http().Run(HttpMethod.Get, "/operations/status-test").Assert(HttpStatusCode.OK);
        var expectedLocation = locationMode switch
        {
            1 => "http://localhost/products/123?v=2#details",
            2 => "https://public.example/gateway/products/123?v=2#details",
            _ => "/products/123?v=2#details"
        };
        completed.Response.Headers.Location.Should().NotBeNull().And.Subject.ToString().Should().Be(expectedLocation);
        using var completedStatus = JsonDocument.Parse(completed.GetContent()!);
        completedStatus.RootElement.GetProperty("resultLocation").GetString().Should().Be(expectedLocation);

        var other = OperationKey.ForDomain(domain, "webapi-operations", "other-owner");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<Api.Entities.Person>(other) with { User = _owner with { UserName = "other" } })).IsSuccess.Should().BeTrue();
        test.Http().Run(HttpMethod.Get, "/operations/other-owner").Assert(HttpStatusCode.NotFound);
        test.Http().Run(HttpMethod.Get, "/operations/unknown").Assert(HttpStatusCode.NotFound);
    }

    [TestCase("tenant-a", HttpStatusCode.OK)]
    [TestCase("tenant-b", HttpStatusCode.NotFound)]
    [TestCase(null, HttpStatusCode.NotFound)]
    public async Task TenantScope_UsesExecutionContextTenant(string? tenant, HttpStatusCode expected)
    {
        using var test = ApiTester.Create<Api.Program>();
        var work = new CoreEx.Hosting.Work.WorkOrchestrator(new CoreEx.Hosting.Work.InMemoryWorkProvider());
        test.ConfigureServices(services =>
        {
            services.AddSingleton(work);
            services.AddExecutionContext((_, ec) => { ec.User = _owner; ec.TenantId = tenant; });
            services.Configure<CoreEx.AspNetCore.Operations.OperationsOptions>(options => options.Scope = OperationKeyScope.Tenant);
        });
        var domain = test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName;
        var key = OperationKey.ForTenant(domain, "tenant-a", "webapi-operations", "tenant-test");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<Api.Entities.Person>(key) with { User = _owner })).IsSuccess.Should().BeTrue();

        test.Http().Run(HttpMethod.Get, "/operations/tenant-test").Assert(expected);
    }

    /// <summary>
    /// Provides an operation type with a result reader.
    /// </summary>
    private sealed class ResultMessage;

    /// <summary>
    /// Provides an operation type with cancellation disabled.
    /// </summary>
    private sealed class NoCancelMessage;

    [Test]
    public async Task Result_RequiresSuccessAndReturnsValueOrNoContent()
    {
        using var test = ApiTester.Create<Api.Program>();
        var work = new CoreEx.Hosting.Work.WorkOrchestrator(new CoreEx.Hosting.Work.InMemoryWorkProvider());
        test.ConfigureServices(services =>
        {
            services.AddSingleton(work);
            services.AddExecutionContext((_, ec) => ec.User = _owner);
            services.Configure<CoreEx.AspNetCore.Operations.OperationsOptions>(options => options.AddOperation<ResultMessage>(o => o.ReadResultAsync = (_, state, _) => Task.FromResult<object?>(new { state.Id, Value = 42 })));
        });
        var domain = test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName;

        var key = OperationKey.ForDomain(domain, "webapi-operations", "result-none");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<Api.Entities.Person>(key) with { User = _owner })).IsSuccess.Should().BeTrue();
        test.Http().Run(HttpMethod.Get, "/operations/result-none/result").Assert(HttpStatusCode.Conflict);
        var started = await work.StartAsync(key, "worker");
        (await work.CompleteAsync(started.Lease.ThrowIfNull())).Outcome.Should().Be(CoreEx.Hosting.Work.WorkTransitionOutcome.Applied);
        test.Http().Run(HttpMethod.Get, "/operations/result-none/result").Assert(HttpStatusCode.NoContent);

        key = OperationKey.ForDomain(domain, "webapi-operations", "result-value");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<ResultMessage>(key) with { User = _owner })).IsSuccess.Should().BeTrue();
        started = await work.StartAsync(key, "worker");
        (await work.CompleteAsync(started.Lease.ThrowIfNull())).Outcome.Should().Be(CoreEx.Hosting.Work.WorkTransitionOutcome.Applied);
        var response = test.Http().Run(HttpMethod.Get, "/operations/result-value/result").Assert(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(response.GetContent()!);
        json.RootElement.GetProperty("value").GetInt32().Should().Be(42);
        response.Response.Headers.CacheControl.ThrowIfNull().ToString().Should().Be("no-store, private");

        test.Http().Run(HttpMethod.Get, "/operations/unknown/result").Assert(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Cancel_AcceptsConflictsAndRejectsWhenDisabled()
    {
        using var test = ApiTester.Create<Api.Program>();
        var work = new CoreEx.Hosting.Work.WorkOrchestrator(new CoreEx.Hosting.Work.InMemoryWorkProvider());
        test.ConfigureServices(services =>
        {
            services.AddSingleton(work);
            services.AddExecutionContext((_, ec) => ec.User = _owner);
            services.Configure<CoreEx.AspNetCore.Operations.OperationsOptions>(options => options.AddOperation<NoCancelMessage>(o => o.CancellationEnabled = false));
        });
        var domain = test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName;

        var key = OperationKey.ForDomain(domain, "webapi-operations", "cancel-test");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<Api.Entities.Person>(key) with { User = _owner })).IsSuccess.Should().BeTrue();
        _ = await work.StartAsync(key, "worker");
        var response = test.Http().Run(HttpMethod.Post, "/operations/cancel-test:cancel").Assert(HttpStatusCode.Accepted);
        response.Response.Headers.Location.ThrowIfNull().ToString().Should().Be("/operations/cancel-test");
        using var json = JsonDocument.Parse(response.GetContent()!);
        json.RootElement.GetProperty("cancelRequested").GetBoolean().Should().BeTrue();

        var done = OperationKey.ForDomain(domain, "webapi-operations", "cancel-done");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<Api.Entities.Person>(done) with { User = _owner })).IsSuccess.Should().BeTrue();
        var started = await work.StartAsync(done, "worker");
        (await work.CompleteAsync(started.Lease.ThrowIfNull())).Outcome.Should().Be(CoreEx.Hosting.Work.WorkTransitionOutcome.Applied);
        test.Http().Run(HttpMethod.Post, "/operations/cancel-done:cancel").Assert(HttpStatusCode.Conflict);

        var disabled = OperationKey.ForDomain(domain, "webapi-operations", "cancel-disabled");
        (await work.CreateAsync(CoreEx.Hosting.Work.WorkArgs.Create<NoCancelMessage>(disabled) with { User = _owner })).IsSuccess.Should().BeTrue();
        var rejected = test.Http().Run(HttpMethod.Post, "/operations/cancel-disabled:cancel").Assert(HttpStatusCode.MethodNotAllowed);
        rejected.Response.Content.Headers.Allow.Should().Contain("GET");

        test.Http().Run(HttpMethod.Post, "/operations/unknown:cancel").Assert(HttpStatusCode.NotFound);
    }
}
