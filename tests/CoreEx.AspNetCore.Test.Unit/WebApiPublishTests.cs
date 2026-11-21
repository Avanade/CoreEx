namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture(typeof(WebApi), typeof(Microsoft.AspNetCore.Mvc.IActionResult))]
[TestFixture(typeof(CoreEx.AspNetCore.Http.WebApi), typeof(IResult))]
public class WebApiPublishTests<TWebApi, TResult> : WithApiTester<Api.Program> where TWebApi : WebApi<TResult>
{
    [TestCase(false)]
    [TestCase(true)]
    public void Callback_PublishesWithoutReadingBody(bool resultStyle)
    {
        ConfigurePublisher();
        using var cancellation = new CancellationTokenSource();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.RouteValues["id"] = "123";
        request.ContentType = "application/json";
        using var body = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("not JSON"));
        request.Body = body;
        request.ContentLength = body.Length;
        var options = new CoreEx.AspNetCore.Publishing.WebApiPublishOptions { Action = "submit", DomainName = "sales", Destination = "commands" };

        Task<CoreEx.Events.EventData> PrepareAsync(WebApiOptions ro, CancellationToken ct)
        {
            ro.Request.Should().BeSameAs(request);
            ro.OperationType.Should().Be(OperationType.Create);
            ct.Should().Be(cancellation.Token);
            var value = new Api.Entities.Person { Id = (string?)ro.Request.RouteValues["id"], FirstName = "Bob" };
            return Task.FromResult(options.CreateDefaultMessage(value).WithKey("custom-key"));
        }

        Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("commands", (_, _, actual) =>
            {
                actual.Event.Type.Should().Be("sales.person.submit.v1");
                actual.Event.Subject.Should().Be("custom-key");
                actual.Event["operationid"].Should().BeNull();
                using var payload = JsonDocument.Parse(actual.Event.Data.Should().BeOfType<BinaryData>().Which.ToMemory());
                payload.RootElement.GetProperty("id").GetString().Should().Be("123");
            }))
            .Run(w => resultStyle
                ? w.PublishWithResultAsync<Api.Entities.Person>(request, async (ro, ct) => Result.Ok(await PrepareAsync(ro, ct)), options, cancellation.Token)
                : w.PublishAsync<Api.Entities.Person>(request, PrepareAsync, options, cancellation.Token))
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted).GetContent().Should().BeNullOrEmpty();

        body.Position.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Callback_PreservesExplicitEventInsteadOfApplyingDefaults(bool resultStyle)
    {
        ConfigurePublisher();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var options = new CoreEx.AspNetCore.Publishing.WebApiPublishOptions { Action = "submit", DomainName = "sales" };
        var message = CoreEx.Events.EventData.CreateEventWith(new Api.Entities.Person { Id = "123" }, CoreEx.Events.EventAction.Created)
            .WithDomain("warehouse")
            .WithKey("event-key");

        Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                actual.Event.Type.Should().Be("warehouse.person.created.v1");
                actual.Event.Subject.Should().Be("event-key");
                using var payload = JsonDocument.Parse(actual.Event.Data.Should().BeOfType<BinaryData>().Which.ToMemory());
                payload.RootElement.GetProperty("id").GetString().Should().Be("123");
            }))
            .Run(w => resultStyle
                ? w.PublishWithResultAsync<Api.Entities.Person>(request, (_, _) => Task.FromResult(Result.Ok(message)), options)
                : w.PublishAsync<Api.Entities.Person>(request, (_, _) => Task.FromResult(message), options))
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Callback_FailureDoesNotResolvePublisherOrCreateTracking(bool returned)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var exception = ValidationException.Create("name", "Name is required.");
        var options = new CoreEx.AspNetCore.Publishing.WebApiPublishOptions
        {
            Operation = new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions(),
            PublisherProvider = _ => throw new InvalidOperationException("The publisher must not be resolved after preparation fails.")
        };

        Test.Type<TWebApi>()
            .Run(w => returned
                ? w.PublishWithResultAsync<Api.Entities.Person>(request, (_, _) => Task.FromResult(Result<CoreEx.Events.EventData>.Fail(exception)), options)
                : w.PublishAsync<Api.Entities.Person>(request, (_, _) => throw exception, options))
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.BadRequest);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Callback_CancellationPropagatesWithoutDispatch(bool returned)
    {
        using var cancellation = new CancellationTokenSource();
        var exception = new OperationCanceledException(cancellation.Token);
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var options = new CoreEx.AspNetCore.Publishing.WebApiPublishOptions
        {
            PublisherProvider = _ => throw new InvalidOperationException("The publisher must not be resolved after preparation is canceled.")
        };

        Test.Type<TWebApi>()
            .Run(w =>
            {
                w.ConvertUnhandledExceptionsToProblemDetails = true;
                return returned
                    ? w.PublishWithResultAsync<Api.Entities.Person>(request, (_, _) => Task.FromResult(Result<CoreEx.Events.EventData>.Fail(exception)), options, cancellation.Token)
                    : w.PublishAsync<Api.Entities.Person>(request, (_, _) => throw exception, options, cancellation.Token);
            })
            .AssertException<OperationCanceledException>();
    }

    [TestCase(false, 0)]
    [TestCase(true, 0)]
    [TestCase(false, 1)]
    [TestCase(false, 2)]
    public async Task Callback_TrackedPublishingRetainsMessageTypeAndOperationMetadata(bool resultStyle, int locationMode)
    {
        ConfigurePublisher();
        var provider = new CoreEx.Hosting.Work.InMemoryWorkProvider();
        Test.ConfigureServices(services => services.AddSingleton(new CoreEx.Hosting.Work.WorkOrchestrator(provider)));
        CoreEx.ExecutionContext.Current.User = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "bob" };
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.Scheme = "https";
        request.Host = new HostString("internal.example");
        request.PathBase = "/gateway";
        var settings = Test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>();
        var options = new CoreEx.AspNetCore.Publishing.WebApiPublishOptions { Operation = new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions() };
        OperationKey? key = null;
        DateTimeOffset? startBy = null;

        var response = Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                var id = actual.Event["operationid"].Should().BeOfType<string>().Subject;
                actual.Event["operationdomain"].Should().Be(settings.DomainName);
                actual.Event["operationscope"].Should().Be("domain");
                actual.Event["operationname"].Should().Be("webapi-operations");
                actual.Event.Subject.Should().Be("tracked-key");
                key = OperationKey.ForDomain(settings.DomainName, "webapi-operations", id);
                startBy = DateTimeOffset.Parse(actual.Event["operationstartby"].Should().BeOfType<string>().Subject);
            }))
            .Run(w =>
            {
                w.UseAbsolutePaths = locationMode == 1;
                w.BaseAddress = locationMode == 2 ? new Uri("https://public.example/gateway/") : null;
                return resultStyle
                    ? w.PublishWithResultAsync<Api.Entities.Person>(request, (_, _) => Task.FromResult(Result.Ok(options.CreateDefaultMessage(new Api.Entities.Person { Id = "123" }).WithKey("tracked-key"))), options)
                    : w.PublishAsync<Api.Entities.Person>(request, (_, _) => Task.FromResult(options.CreateDefaultMessage(new Api.Entities.Person { Id = "123" }).WithKey("tracked-key")), options);
            })
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);

        using var document = JsonDocument.Parse(response.GetContent()!);
        document.RootElement.GetProperty("id").GetString().Should().Be(key.ThrowIfNull().InstanceId);
        document.RootElement.GetProperty("status").GetString().Should().Be("not-started");
        var operationLocation = response.Response.Headers.GetValues("Operation-Location").Should().ContainSingle().Which;
        operationLocation.Should().Be(locationMode switch
        {
            1 => $"https://internal.example/gateway/operations/{key.ThrowIfNull().InstanceId}",
            2 => $"https://public.example/gateway/operations/{key.ThrowIfNull().InstanceId}",
            _ => $"/gateway/operations/{key.ThrowIfNull().InstanceId}"
        });
        operationLocation.Should().Contain(key.ThrowIfNull().InstanceId);
        response.Response.Headers.Location.Should().NotBeNull().And.Subject.ToString().Should().Be(operationLocation);
        var state = (await provider.GetAsync(key.ThrowIfNull(), CancellationToken.None)).State;
        state.Should().NotBeNull();
        state.ThrowIfNull().TypeName.Should().Be(typeof(Api.Entities.Person).FullName);
        state.ThrowIfNull().StartBy.Should().Be(startBy);
    }

    [Test]
    public async Task EndpointMetadata_EnablesTrackingWithoutPerCallOperationOptions()
    {
        ConfigurePublisher();
        var provider = new CoreEx.Hosting.Work.InMemoryWorkProvider();
        Test.ConfigureServices(services => services.AddSingleton(new CoreEx.Hosting.Work.WorkOrchestrator(provider)));
        CoreEx.ExecutionContext.Current.User = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "bob" };
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.HttpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new PublishTrackingAttribute()), "tracked"));
        var options = new CoreEx.AspNetCore.Publishing.WebApiPublishOptions();

        var response = Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                actual.Event["operationid"].Should().BeOfType<string>();
                actual.Event["operationstartby"].Should().BeOfType<string>();
            }))
            .Run(w => w.PublishAsync<Api.Entities.Person>(request, (_, _) =>
                Task.FromResult(options.CreateDefaultMessage(new Api.Entities.Person { Id = "123" })), options))
            .ToHttpResponseMessageAssertor(request)
            .Assert(HttpStatusCode.Accepted);

        using var document = JsonDocument.Parse(response.GetContent()!);
        document.RootElement.GetProperty("status").GetString().Should().Be("not-started");
        (await provider.GetAsync(
            OperationKey.ForDomain(Test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName, "webapi-operations", document.RootElement.GetProperty("id").GetString()!),
            CancellationToken.None)).State.Should().NotBeNull();
    }

    [Test]
    public void EndpointWithoutTrackingMetadata_RejectsOperationOptions()
    {
        ConfigurePublisher();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.HttpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "untracked"));
        var options = new CoreEx.AspNetCore.Publishing.WebApiPublishOptions { Operation = new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions() };

        Test.Type<TWebApi>()
            .Run(w => w.PublishAsync<Api.Entities.Person>(request, (_, _) =>
                Task.FromResult(options.CreateDefaultMessage(new Api.Entities.Person { Id = "123" })), options))
            .AssertException<InvalidOperationException>();
    }

    [TestCase(false, "/gateway/api/person/123")]
    [TestCase(true, "https://internal.example/gateway/api/person/123")]
    [TestCase(false, "https://public.example/edge/api/person/123")]
    public void Location_UsesWebApiLinkConfiguration(bool absolute, string expected)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.Scheme = "https";
        request.Host = new HostString("internal.example");
        request.PathBase = "/gateway";

        var response = Test.Type<TWebApi>()
            .Run(w =>
            {
                w.UseAbsolutePaths = absolute;
                if (expected.StartsWith("https://public.example", StringComparison.Ordinal))
                    w.BaseAddress = new Uri("https://public.example/edge/");

                return w.PostAsync(request, (ro, _) =>
                {
                    ro.WithLocationUri(() => new Uri("/api/person/123", UriKind.Relative));
                    return Task.CompletedTask;
                }, HttpStatusCode.Created);
            })
            .ToHttpResponseMessageAssertor(request)
            .Assert(HttpStatusCode.Created);

        response.Response.Headers.Location.ThrowIfNull().ToString().Should().Be(expected);
    }

    [Test]
    public void Location_PreservesAbsoluteUri()
    {
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.PathBase = "/gateway";
        var absoluteLocation = new Uri("https://resources.example/api/person/123");

        var response = Test.Type<TWebApi>()
            .Run(w =>
            {
                w.BaseAddress = new Uri("https://public.example/edge/");
                return w.PostAsync(request, (ro, _) =>
                {
                    ro.WithLocationUri(() => absoluteLocation);
                    return Task.CompletedTask;
                }, HttpStatusCode.Created);
            })
            .ToHttpResponseMessageAssertor(request)
            .Assert(HttpStatusCode.Created);

        response.Response.Headers.Location.Should().Be(absoluteLocation);
    }

    /// <summary>
    /// Configures the existing publisher capture helpers with deterministic message formatting.
    /// </summary>
    private void ConfigurePublisher() => Test.ConfigureServices(services => services
        .AddSingleton<CoreEx.Events.IEventFormatter>(new CoreEx.Events.EventFormatter { DomainName = "sales", SourceBaseUri = new Uri("urn:coreex:test") })
        .AddEventPublisher("EventPublisher", sp => ActivatorUtilities.CreateInstance<CoreEx.Events.Publishing.NoOpEventPublisher>(sp))
        .UseExpectedEventPublisher());
}
