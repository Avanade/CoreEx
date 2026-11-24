namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture]
public class OperationsTests
{
    private static readonly CoreEx.Security.AuthenticationUser _owner = new() { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "owner", Id = "not-the-username" };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Test]
    public void Status_RetryAfterUsesLiveTimeRatherThanFrozenExecutionTimestamp()
    {
        var store = new OperationSnapshotStore();
        using var test = CreateTester(store);
        test.ConfigureServices(services => services.AddSingleton<TimeProvider>(new FixedTimeProvider(store.State.CreatedOn.AddMinutes(2))));
        var response = test.Http().Run(HttpMethod.Get, Location(store)).Assert(HttpStatusCode.OK);
        response.Response.Headers.RetryAfter.ThrowIfNull().ToString().Should().Be("15");
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Status_UsesRepresentationETagsAndConfiguredLinks(int locationMode)
    {
        var store = new OperationSnapshotStore();
        using var test = CreateTester(store);
        test.ConfigureServices(services => services.AddHttpWebApi((_, webApi) =>
        {
            webApi.UseAbsolutePaths = locationMode == 1;
            webApi.BaseAddress = locationMode == 2 ? new Uri("https://public.example/gateway/") : null;
        }));
        var response = test.Http().Run(HttpMethod.Get, Location(store)).Assert(HttpStatusCode.OK);
        var etag = response.Response.Headers.ETag.ThrowIfNull().ToString();
        test.Http().Run(HttpMethod.Get, Location(store), r => r.Headers.TryAddWithoutValidation("If-None-Match", $"\"other\", W/{etag}")).Assert(HttpStatusCode.NotModified);
        test.Http().Run(HttpMethod.Get, Location(store), r => r.Headers.TryAddWithoutValidation("If-None-Match", "*")).Assert(HttpStatusCode.NotModified);

        // xmin can remain unchanged across mutations and operation incarnations.
        store.State = store.State with { Status = CoreEx.Operations.OperationStatus.Succeeded, ResultLocation = "/products/123?v=2#details", AbandonAfter = null };
        var completed = test.Http().Run(HttpMethod.Get, Location(store), r => r.Headers.TryAddWithoutValidation("If-None-Match", etag)).Assert(HttpStatusCode.OK);
        var expectedLocation = locationMode switch
        {
            1 => "http://localhost/products/123?v=2#details",
            2 => "https://public.example/gateway/products/123?v=2#details",
            _ => "/products/123?v=2#details"
        };
        completed.Response.Headers.Location.ThrowIfNull().ToString().Should().Be(expectedLocation);
        using var status = JsonDocument.Parse(completed.GetContent()!);
        status.RootElement.GetProperty("resultLocation").GetString().Should().Be(expectedLocation);
        test.Http().Run(HttpMethod.Get, $"{Location(store)}/result").Assert(HttpStatusCode.NoContent);

        var completedEtag = completed.Response.Headers.ETag.ThrowIfNull().ToString();
        var oldLocation = Location(store);
        store.State = store.State with { OperationId = Guid.NewGuid() };
        test.Http().Run(HttpMethod.Get, Location(store), r => r.Headers.TryAddWithoutValidation("If-None-Match", completedEtag)).Assert(HttpStatusCode.OK);
        test.Http().Run(HttpMethod.Get, oldLocation).Assert(HttpStatusCode.NotFound);
    }

    [TestCase(CoreEx.Operations.OperationStatus.Accepted, "accepted")]
    [TestCase(CoreEx.Operations.OperationStatus.Running, "running")]
    [TestCase(CoreEx.Operations.OperationStatus.Retrying, "retrying")]
    [TestCase(CoreEx.Operations.OperationStatus.Succeeded, "succeeded")]
    [TestCase(CoreEx.Operations.OperationStatus.Failed, "failed")]
    [TestCase(CoreEx.Operations.OperationStatus.Cancelled, "canceled")]
    [TestCase(CoreEx.Operations.OperationStatus.Expired, "expired")]
    public void Status_ProjectsLifecycleWithoutPrivateState(CoreEx.Operations.OperationStatus lifecycle, string expected)
    {
        var store = new OperationSnapshotStore();
        store.State = store.State with { Status = lifecycle, ErrorCode = "private-code", ErrorMessage = "private-diagnostics" };
        using var test = CreateTester(store);
        var response = test.Http().Run(HttpMethod.Get, Location(store)).Assert(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(response.GetContent()!);
        json.RootElement.GetProperty("status").GetString().Should().Be(expected);
        foreach (var name in new[] { "instanceId", "data", "initiatedBy", "inactivityTimeout", "terminalRetention", "attempt", "claimOwner", "claimExpiresOn", "etag" })
            json.RootElement.TryGetProperty(name, out _).Should().BeFalse();

        response.GetContent().Should().NotContain("private-diagnostics").And.NotContain("private-code");
        response.Response.Headers.CacheControl.ThrowIfNull().ToString().Should().Be("no-store, private");
        (response.Response.Headers.RetryAfter is null).Should().Be(store.State.IsFinished);
    }

    [TestCase("owner", "Test", null, HttpStatusCode.OK)]
    [TestCase("Owner", "Test", null, HttpStatusCode.NotFound)]
    [TestCase("", "Test", null, HttpStatusCode.NotFound)]
    [TestCase(null, "Test", null, HttpStatusCode.NotFound)]
    [TestCase("owner", "elsewhere", null, HttpStatusCode.NotFound)]
    [TestCase("owner", "Test", "tenant-a", HttpStatusCode.NotFound)]
    public void Ownership_IsOrdinalUsernameAndScopeBeforePolicy(string? initiatedBy, string domain, string? tenant, HttpStatusCode expected)
    {
        var store = new OperationSnapshotStore();
        store.State = store.State with { InitiatedBy = initiatedBy, DomainName = domain, TenantId = tenant };
        using var test = CreateTester(store);
        var authorized = 0;
        test.ConfigureServices(services => services.Configure<CoreEx.AspNetCore.Operations.OperationsOptions>(options =>
            options.AddOperation<ResultMessage>("policy-operation", o => o.AuthorizeAsync = (_, _, _) => { authorized++; return Task.FromResult(true); })));
        store.State = store.State with { OperationName = "policy-operation" };

        test.Http().Run(HttpMethod.Get, Location(store), r => r.Headers.TryAddWithoutValidation("If-None-Match", "\"nonmatching\"")).Assert(expected);
        test.Http().Run(HttpMethod.Get, $"{Location(store)}/result").Assert(expected == HttpStatusCode.OK ? HttpStatusCode.Conflict : expected);
        if (expected == HttpStatusCode.NotFound)
            test.Http().Run(HttpMethod.Post, $"{Location(store)}:cancel").Assert(expected);

        authorized.Should().Be(expected == HttpStatusCode.OK ? 2 : 0);
    }

    [TestCase(CoreEx.Security.AuthenticationType.Unknown, "owner")]
    [TestCase(CoreEx.Security.AuthenticationType.Unauthenticated, "owner")]
    [TestCase(CoreEx.Security.AuthenticationType.AccountUser, " ")]
    public void Ownership_RejectsMissingAuthenticatedUsername(CoreEx.Security.AuthenticationType type, string username)
    {
        var store = new OperationSnapshotStore();
        using var test = CreateTester(store, _owner with { Type = type, UserName = username });
        test.Http().Run(HttpMethod.Get, Location(store), r => r.Headers.TryAddWithoutValidation("If-None-Match", "*")).Assert(HttpStatusCode.NotFound);
        test.Http().Run(HttpMethod.Post, $"{Location(store)}:cancel").Assert(HttpStatusCode.NotFound);
    }

    [Test]
    public void Ownership_DoesNotTreatDefaultEnvironmentUserAsAuthenticatedHttpCaller()
    {
        var store = new OperationSnapshotStore();
        store.State = store.State with { InitiatedBy = CoreEx.Security.AuthenticationUser.EnvironmentUser.UserName };
        using var test = CreateTester(store, CoreEx.Security.AuthenticationUser.EnvironmentUser);
        test.Http().Run(HttpMethod.Get, Location(store)).Assert(HttpStatusCode.NotFound);
    }

    [Test]
    public void Result_RequiresSuccessAndUsesApplicationReader()
    {
        var store = new OperationSnapshotStore();
        using var test = CreateTester(store);
        test.ConfigureServices(services => services.Configure<CoreEx.AspNetCore.Operations.OperationsOptions>(options =>
            options.AddOperation<ResultMessage>("result-operation", o => o.ReadResultAsync = (_, state, _) => Task.FromResult<object?>(new { state.OperationId, Value = 42 }))));
        test.Http().Run(HttpMethod.Get, $"{Location(store)}/result").Assert(HttpStatusCode.Conflict);
        store.State = store.State with { Status = CoreEx.Operations.OperationStatus.Succeeded };
        test.Http().Run(HttpMethod.Get, $"{Location(store)}/result").Assert(HttpStatusCode.NoContent);
        store.State = store.State with { OperationName = "result-operation" };
        var response = test.Http().Run(HttpMethod.Get, $"{Location(store)}/result").Assert(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(response.GetContent()!);
        json.RootElement.GetProperty("value").GetInt32().Should().Be(42);
        response.Response.Headers.CacheControl.ThrowIfNull().ToString().Should().Be("no-store, private");
    }

    [TestCase(CoreEx.Operations.OperationStatus.Accepted)]
    [TestCase(CoreEx.Operations.OperationStatus.Retrying)]
    [TestCase(CoreEx.Operations.OperationStatus.Running)]
    public async Task Cancel_UsesActualCooperativeStoreTransition(CoreEx.Operations.OperationStatus lifecycle)
    {
        using var test = ApiTester.Create<Api.Program>();
        test.ConfigureServices(services =>
        {
            services.AddSingleton<IOperationStore, InMemoryOperationStore>();
            services.AddExecutionContext((_, ec) => ec.User = _owner);
        });
        var store = test.Services.GetRequiredService<IOperationStore>();
        var created = await store.CreateAsync("person-submit", CoreEx.Entities.CompositeKey.Create("cancel"), _owner.UserName);
        if (lifecycle != CoreEx.Operations.OperationStatus.Accepted)
        {
            var claim = (await store.ClaimAsync(created.State.OperationId, "worker", TimeSpan.FromMinutes(1))).Claim.ThrowIfNull();
            if (lifecycle == CoreEx.Operations.OperationStatus.Retrying)
                (await store.ReleaseAsync(claim)).Should().BeTrue();
        }

        var location = $"/operations/{created.State.OperationId:D}";
        var response = test.Http().Run(HttpMethod.Post, $"{location}:cancel").Assert(HttpStatusCode.Accepted);
        response.Response.Headers.Location.ThrowIfNull().ToString().Should().Be(location);
        var actual = (await store.GetByIdAsync(created.State.OperationId)).ThrowIfNull();
        actual.Status.Should().Be(lifecycle == CoreEx.Operations.OperationStatus.Running ? CoreEx.Operations.OperationStatus.Running : CoreEx.Operations.OperationStatus.Cancelled);
        actual.CancelRequestedOn.Should().NotBeNull();
    }

    [TestCase(CoreEx.Operations.OperationStatus.Succeeded)]
    [TestCase(CoreEx.Operations.OperationStatus.Failed)]
    [TestCase(CoreEx.Operations.OperationStatus.Expired)]
    public void Cancel_CompletionRaceDoesNotReportAcceptedCancellation(CoreEx.Operations.OperationStatus finished)
    {
        var store = new OperationSnapshotStore { OnCancel = state => state with { Status = finished } };
        using var test = CreateTester(store);
        test.Http().Run(HttpMethod.Post, $"{Location(store)}:cancel").Assert(HttpStatusCode.Conflict);
    }

    [Test]
    public void Cancel_RejectsTerminalDisabledAndUnknownOperations()
    {
        var store = new OperationSnapshotStore();
        using var test = CreateTester(store);
        test.ConfigureServices(services => services.Configure<CoreEx.AspNetCore.Operations.OperationsOptions>(options =>
            options.AddOperation<ResultMessage>("no-cancel", o => o.CancellationEnabled = false)));
        store.State = store.State with { Status = CoreEx.Operations.OperationStatus.Cancelled };
        test.Http().Run(HttpMethod.Post, $"{Location(store)}:cancel").Assert(HttpStatusCode.Conflict);
        store.State = store.State with { Status = CoreEx.Operations.OperationStatus.Accepted, OperationName = "no-cancel" };
        var rejected = test.Http().Run(HttpMethod.Post, $"{Location(store)}:cancel").Assert(HttpStatusCode.MethodNotAllowed);
        rejected.Response.Content.Headers.Allow.Should().Contain("GET");
        foreach (var id in new[] { "unknown", Guid.Empty.ToString("D"), Guid.NewGuid().ToString("D") })
        {
            test.Http().Run(HttpMethod.Get, $"/operations/{id}").Assert(HttpStatusCode.NotFound);
            test.Http().Run(HttpMethod.Post, $"/operations/{id}:cancel").Assert(HttpStatusCode.NotFound);
        }
    }

    private sealed class ResultMessage;

    private static string Location(OperationSnapshotStore store) => $"/operations/{store.State.OperationId:D}";

    private static UnitTestEx.AspNetCore.ApiTester<Api.Program> CreateTester(OperationSnapshotStore store, CoreEx.Security.AuthenticationUser? user = null)
    {
        var test = ApiTester.Create<Api.Program>();
        test.ConfigureServices(services =>
        {
            services.AddSingleton<IOperationStore>(store);
            services.AddExecutionContext((_, ec) => ec.User = user ?? _owner);
        });
        return test;
    }
}
