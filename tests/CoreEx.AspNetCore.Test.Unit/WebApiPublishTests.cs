namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture(typeof(WebApi), typeof(Microsoft.AspNetCore.Mvc.IActionResult))]
[TestFixture(typeof(CoreEx.AspNetCore.Http.WebApi), typeof(IResult))]
public class WebApiPublishTests<TWebApi, TResult> : WithApiTester<Api.Program> where TWebApi : WebApi<TResult>
{
    [Test]
    public void Callback_PublishesWithoutReadingBody()
    {
        ConfigurePublisher();
        using var cancellation = new CancellationTokenSource();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.RouteValues["id"] = "123";
        request.ContentType = "application/json";
        using var body = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("not JSON"));
        request.Body = body;
        request.ContentLength = body.Length;
        Task<CoreEx.Events.EventData> PrepareAsync(WebApiOptions ro, CancellationToken ct)
        {
            ro.Request.Should().BeSameAs(request);
            ro.OperationType.Should().Be(OperationType.Create);
            ct.Should().Be(cancellation.Token);
            var value = new Api.Entities.Person { Id = (string?)ro.Request.RouteValues["id"], FirstName = "Bob" };
            return Task.FromResult(CoreEx.Events.EventData.CreateCommandWith("sales", value, "submit").WithKey("custom-key"));
        }

        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>(async (ro, ct) => Result.Ok(await PrepareAsync(ro, ct)))
            .WithDestination("commands");

        Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("commands", (_, _, actual) =>
            {
                actual.Event.Type.Should().Be("sales.person.submit.v1");
                actual.Event.Subject.Should().Be("custom-key");
                actual.Event["operationid"].Should().BeNull();
                using var payload = JsonDocument.Parse(actual.Event.Data.Should().BeOfType<BinaryData>().Which.ToMemory());
                payload.RootElement.GetProperty("id").GetString().Should().Be("123");
            }))
            .Run(w => w.PublishAsync(request, options, cancellation.Token))
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted).GetContent().Should().BeNullOrEmpty();

        body.Position.Should().Be(0);
    }

    [Test]
    public void Callback_PublishesPayloadUsingDefaultEnvelopeAndEventTransform()
    {
        ConfigurePublisher();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithValueFactory<Api.Entities.Person>((_, _) => Task.FromResult(Result.Ok(new Api.Entities.Person { Id = "123" })))
            .WithDestination("events")
            .WithEvent(message =>
            {
                message.MessageType.Should().Be(CoreEx.Events.MessageType.Command);
                message.Action.Should().Be("Publish");
                message.MessageType = CoreEx.Events.MessageType.Event;
                return message.WithAction("created").WithDomain("sales").WithKey("custom-key");
            });

        Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("events", (_, _, actual) =>
            {
                actual.Event.Type.Should().Be("sales.person.created.v1");
                actual.Event.Subject.Should().Be("custom-key");
                using var payload = JsonDocument.Parse(actual.Event.Data.Should().BeOfType<BinaryData>().Which.ToMemory());
                payload.RootElement.GetProperty("id").GetString().Should().Be("123");
            }))
            .Run(w => w.PublishAsync(request, options))
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
    }

    [Test]
    public void Callback_PreservesExplicitEventInsteadOfApplyingDefaults()
    {
        ConfigurePublisher();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var message = CoreEx.Events.EventData.CreateEventWith(new Api.Entities.Person { Id = "123" }, CoreEx.Events.EventAction.Created)
            .WithDomain("warehouse")
            .WithKey("event-key");
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) => Task.FromResult(Result.Ok(message)));

        Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                actual.Event.Type.Should().Be("warehouse.person.created.v1");
                actual.Event.Subject.Should().Be("event-key");
                using var payload = JsonDocument.Parse(actual.Event.Data.Should().BeOfType<BinaryData>().Which.ToMemory());
                payload.RootElement.GetProperty("id").GetString().Should().Be("123");
            }))
            .Run(w => w.PublishAsync(request, options))
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Callback_FailureDoesNotResolvePublisherOrCreateTracking(bool returned)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var exception = ValidationException.Create("name", "Name is required.");
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) => returned
                ? Task.FromResult(Result<CoreEx.Events.EventData>.Fail(exception))
                : throw exception)
            .WithOperationOptions(new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions())
            .WithPublisher(_ => throw new InvalidOperationException("The publisher must not be resolved after preparation fails."));

        Test.Type<TWebApi>()
            .Run(w => w.PublishAsync(request, options))
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.BadRequest);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Callback_CancellationPropagatesWithoutDispatch(bool returned)
    {
        using var cancellation = new CancellationTokenSource();
        var exception = new OperationCanceledException(cancellation.Token);
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) => returned
                ? Task.FromResult(Result<CoreEx.Events.EventData>.Fail(exception))
                : throw exception)
            .WithPublisher(_ => throw new InvalidOperationException("The publisher must not be resolved after preparation is canceled."));

        Test.Type<TWebApi>()
            .Run(w =>
            {
                w.ConvertUnhandledExceptionsToProblemDetails = true;
                return w.PublishAsync(request, options, cancellation.Token);
            })
            .AssertException<OperationCanceledException>();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public async Task Callback_TrackedPublishingRetainsMessageTypeAndOperationMetadata(int locationMode)
    {
        ConfigurePublisher();
        var provider = new CoreEx.Hosting.Work.InMemoryWorkProvider();
        Test.ConfigureServices(services => services.AddSingleton(sp => new CoreEx.Hosting.Work.WorkOrchestrator(provider, sp.GetRequiredService<CoreEx.Hosting.IHostSettings>())));
        CoreEx.ExecutionContext.Current.User = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "bob" };
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.Scheme = "https";
        request.Host = new HostString("internal.example");
        request.PathBase = "/gateway";
        var settings = Test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>();
        var message = new CoreEx.Events.EventData { MessageType = CoreEx.Events.MessageType.Command }.WithValue(new Api.Entities.Person { Id = "123" }).WithKey("tracked-key");
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) => Task.FromResult(Result.Ok(message)))
            .WithDestination(null)
            .WithOperationOptions(new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions());
        string? key = null;
        DateTimeOffset? startBy = null;

        var response = Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                var id = actual.Event["operationid"].Should().BeOfType<string>().Subject;
                actual.Event["operationdomain"].Should().BeNull();
                actual.Event["operationtenantid"].Should().BeNull();
                actual.Event["operationscope"].Should().BeNull();
                actual.Event["operationname"].Should().Be("webapi-operations");
                actual.Event.Subject.Should().Be("tracked-key");
                key = id;
                startBy = DateTimeOffset.Parse(actual.Event["operationstartby"].Should().BeOfType<string>().Subject);
            }))
            .Run(w =>
            {
                w.UseAbsolutePaths = locationMode == 1;
                w.BaseAddress = locationMode == 2 ? new Uri("https://public.example/gateway/") : null;
                return w.PublishAsync(request, options);
            })
            .ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);

        using var document = JsonDocument.Parse(response.GetContent()!);
        document.RootElement.GetProperty("id").GetString().Should().Be(key.ThrowIfNull());
        document.RootElement.GetProperty("status").GetString().Should().Be("not-started");
        var operationLocation = response.Response.Headers.GetValues("Operation-Location").Should().ContainSingle().Which;
        operationLocation.Should().Be(locationMode switch
        {
            1 => $"https://internal.example/gateway/operations/{key.ThrowIfNull()}",
            2 => $"https://public.example/gateway/operations/{key.ThrowIfNull()}",
            _ => $"/gateway/operations/{key.ThrowIfNull()}"
        });
        operationLocation.Should().Contain(key.ThrowIfNull());
        response.Response.Headers.Location.Should().NotBeNull().And.Subject.ToString().Should().Be(operationLocation);
        var state = (await provider.GetAsync(settings.DomainName, null, "webapi-operations", key.ThrowIfNull(), CancellationToken.None)).State;
        state.Should().NotBeNull();
        state.ThrowIfNull().TypeName.Should().Be(typeof(Api.Entities.Person).FullName);
        state.ThrowIfNull().StartBy.Should().Be(startBy);
    }

    [Test]
    public async Task EndpointMetadata_EnablesTrackingWithoutPerCallOperationOptions()
    {
        ConfigurePublisher();
        var provider = new CoreEx.Hosting.Work.InMemoryWorkProvider();
        Test.ConfigureServices(services => services.AddSingleton(sp => new CoreEx.Hosting.Work.WorkOrchestrator(provider, sp.GetRequiredService<CoreEx.Hosting.IHostSettings>())));
        CoreEx.ExecutionContext.Current.User = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "bob" };
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.HttpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new PublishTrackingAttribute()), "tracked"));
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.Create(new Api.Entities.Person { Id = "123" });

        var response = Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                actual.Event["operationid"].Should().BeOfType<string>();
                actual.Event["operationstartby"].Should().BeOfType<string>();
            }))
            .Run(w => w.PublishAsync(request, options))
            .ToHttpResponseMessageAssertor(request)
            .Assert(HttpStatusCode.Accepted);

        using var document = JsonDocument.Parse(response.GetContent()!);
        document.RootElement.GetProperty("status").GetString().Should().Be("not-started");
        (await provider.GetAsync(Test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName, null, "webapi-operations", document.RootElement.GetProperty("id").GetString()!, CancellationToken.None)).State.Should().NotBeNull();
    }

    [Test]
    public void EndpointWithoutTrackingMetadata_RejectsOperationOptions()
    {
        ConfigurePublisher();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.HttpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "untracked"));
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) => Task.FromResult(Result.Ok(CoreEx.Events.EventData.CreateCommandWith("sales", new Api.Entities.Person { Id = "123" }, "submit"))))
            .WithOperationOptions(new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions());

        Test.Type<TWebApi>()
            .Run(w => w.PublishAsync(request, options))
            .AssertException<InvalidOperationException>();
    }

    [TestCase(false, "another-domain")]
    [TestCase(false, "")]
    [TestCase(true, "another-domain")]
    [TestCase(true, "")]
    public void TrackedPublishing_RejectsDestinationOverrideBeforeResolvingPublisher(bool useEndpointMetadata, string destination)
    {
        ConfigurePublisher();
        Test.ConfigureServices(services => services.AddSingleton(sp => new CoreEx.Hosting.Work.WorkOrchestrator(new UnusedWorkProvider(), sp.GetRequiredService<CoreEx.Hosting.IHostSettings>())));
        CoreEx.ExecutionContext.Current.User = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "bob" };
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        if (useEndpointMetadata)
            request.HttpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new PublishTrackingAttribute()), "tracked"));

        var publisherResolved = false;
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.Create(new Api.Entities.Person { Id = "123" })
            .WithDestination(destination)
            .WithPublisher(services =>
            {
                publisherResolved = true;
                return services.GetRequiredService<CoreEx.Events.Publishing.IEventPublisher>();
            });

        if (!useEndpointMetadata)
            options.WithOperationOptions(new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions());

        Test.Type<TWebApi>()
            .Run(w => w.PublishAsync(request, options))
            .AssertException<InvalidOperationException>();

        publisherResolved.Should().BeFalse();
    }

    [TestCase("event")]
    [TestCase("other-domain")]
    [TestCase("other-tenant")]
    [TestCase("already-tracked")]
    public void Callback_TrackedPublishing_RejectsInvalidMessageBeforeCreatingState(string mode)
    {
        ConfigurePublisher();
        Test.ConfigureServices(services => services.AddSingleton(sp => new CoreEx.Hosting.Work.WorkOrchestrator(new UnusedWorkProvider(), sp.GetRequiredService<CoreEx.Hosting.IHostSettings>())));
        CoreEx.ExecutionContext.Current.User = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "bob" };
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) =>
            {
                var value = new Api.Entities.Person { Id = "123" };
                var message = mode switch
                {
                    "event" => CoreEx.Events.EventData.CreateEventWith(value, "created"),
                    "other-domain" => CoreEx.Events.EventData.CreateCommandWith("elsewhere", value, "submit"),
                    "other-tenant" => new CoreEx.Events.EventData { MessageType = CoreEx.Events.MessageType.Command, TenantId = "other" }.WithValue(value),
                    _ => new CoreEx.Events.EventData { MessageType = CoreEx.Events.MessageType.Command }.WithValue(value).SetAttribute("operationid", "existing")
                };

                return Task.FromResult(Result.Ok(message));
            })
            .WithOperationOptions(new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions());

        Test.Type<TWebApi>()
            .Run(w => w.PublishAsync(request, options))
            .AssertException<InvalidOperationException>();
    }

    [Test]
    public void Callback_UntrackedPublishing_AllowsEventToAnyDomain()
    {
        ConfigurePublisher();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) => Task.FromResult(Result.Ok(CoreEx.Events.EventData.CreateEventWith(new Api.Entities.Person { Id = "123" }, "created").WithDomain("elsewhere"))));

        Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) => actual.Event["operationid"].Should().BeNull()))
            .Run(w => w.PublishAsync(request, options))
            .ToHttpResponseMessageAssertor(request)
            .Assert(HttpStatusCode.Accepted);
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

    /// <summary>
    /// A work provider that fails if operation state is touched.
    /// </summary>
    private sealed class UnusedWorkProvider : CoreEx.Hosting.Work.IWorkProvider
    {
        public Task<CoreEx.Hosting.Work.WorkSnapshot> GetAsync(string domainName, string? tenantId, string operationName, string id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> CreateAsync(CoreEx.Hosting.Work.WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken) => throw new NotSupportedException("Operation state must not be created for an invalid message.");

        public Task<bool> UpdateAsync(CoreEx.Hosting.Work.WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(CoreEx.Hosting.Work.WorkState state, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
