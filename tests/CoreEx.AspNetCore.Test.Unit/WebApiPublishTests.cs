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
        ConfigureTrackedPublishing();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.Scheme = "https";
        request.Host = new HostString("internal.example");
        request.PathBase = "/gateway";
        var message = new CoreEx.Events.EventData { MessageType = CoreEx.Events.MessageType.Command }.WithValue(new Api.Entities.Person { Id = "123" }).WithKey("tracked-key");
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) => Task.FromResult(Result.Ok(message)))
            .WithDestination(null)
            .WithOperationOptions(new CoreEx.AspNetCore.Publishing.WebApiPublishOperationOptions());
        string? key = null;

        var response = Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                var id = actual.Event["operationid"].Should().BeOfType<string>().Subject;
                actual.Event["operationdomain"].Should().BeNull();
                actual.Event["operationtenantid"].Should().BeNull();
                actual.Event["operationscope"].Should().BeNull();
                actual.Event["operationname"].Should().Be("person-submit");
                actual.Event.Subject.Should().Be("tracked-key");
                key = id;
                actual.Event["operationstartby"].Should().BeNull();
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
        document.RootElement.GetProperty("status").GetString().Should().Be("accepted");
        var operationLocation = response.Response.Headers.GetValues("Operation-Location").Should().ContainSingle().Which;
        operationLocation.Should().Be(locationMode switch
        {
            1 => $"https://internal.example/gateway/operations/{key.ThrowIfNull()}",
            2 => $"https://public.example/gateway/operations/{key.ThrowIfNull()}",
            _ => $"/gateway/operations/{key.ThrowIfNull()}"
        });
        operationLocation.Should().Contain(key.ThrowIfNull());
        response.Response.Headers.Location.Should().NotBeNull().And.Subject.ToString().Should().Be(operationLocation);
        var state = (await Test.Services.GetRequiredService<IOperationStore>().GetByIdAsync(Guid.Parse(key.ThrowIfNull()))).ThrowIfNull();
        state.OperationName.Should().Be("person-submit");
        state.InitiatedBy.Should().Be("bob");
    }

    [Test]
    public async Task EndpointMetadata_EnablesTrackingWithoutPerCallOperationOptions()
    {
        ConfigurePublisher();
        ConfigureTrackedPublishing();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.HttpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new PublishTrackingAttribute()), "tracked"));
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.Create(new Api.Entities.Person { Id = "123" });

        var response = Test.Type<TWebApi>()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                actual.Event["operationid"].Should().BeOfType<string>();
                actual.Event["operationstartby"].Should().BeNull();
            }))
            .Run(w => w.PublishAsync(request, options))
            .ToHttpResponseMessageAssertor(request)
            .Assert(HttpStatusCode.Accepted);

        using var document = JsonDocument.Parse(response.GetContent()!);
        document.RootElement.GetProperty("status").GetString().Should().Be("accepted");
        (await Test.Services.GetRequiredService<IOperationStore>().GetByIdAsync(document.RootElement.GetProperty("id").GetGuid())).Should().NotBeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TrackedPublishing_DuplicateReturnsExistingHandleWithoutResolvingPublisher(bool terminal)
    {
        ConfigureTrackedPublishing();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var count = 0;
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.Create(new Api.Entities.Person { Id = "123", FirstName = "First submission" })
            .WithOperationOptions(new() { Key = CoreEx.Entities.CompositeKey.Create("submission-token"), InactivityTimeout = TimeSpan.FromMinutes(7), TerminalRetention = TimeSpan.FromHours(3) })
            .WithPublisher(_ => new RecordingPublisher(_ => count++));

        var first = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
        using var firstStatus = JsonDocument.Parse(first.GetContent()!);
        first.GetContent().Should().NotContain("submission-token");
        var id = firstStatus.RootElement.GetProperty("id").GetGuid();
        var store = Test.Services.GetRequiredService<IOperationStore>();
        var state = (await store.GetByIdAsync(id)).ThrowIfNull();
        state.InitiatedBy.Should().Be("bob");
        state.InactivityTimeout.Should().Be(TimeSpan.FromMinutes(7));
        state.TerminalRetention.Should().Be(TimeSpan.FromHours(3));
        if (terminal)
        {
            var claim = (await store.ClaimAsync(id, "worker", TimeSpan.FromMinutes(1))).Claim.ThrowIfNull();
            (await store.CompleteAsync(claim, OperationCompletion.Succeeded())).Should().NotBeNull();
        }

        // Same identity wins even when payload changes; no request fingerprint or second publish.
        options.WithValue(new Api.Entities.Person { Id = "123", FirstName = "Changed payload" })
            .WithOperationOptions(new() { Key = CoreEx.Entities.CompositeKey.Create("submission-token"), InactivityTimeout = TimeSpan.FromMinutes(1), TerminalRetention = TimeSpan.FromHours(1) })
            .WithPublisher(_ => throw new InvalidOperationException("Duplicates must not resolve the publisher."));
        var duplicate = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
        using var duplicateStatus = JsonDocument.Parse(duplicate.GetContent()!);
        duplicateStatus.RootElement.GetProperty("id").GetGuid().Should().Be(id);
        duplicateStatus.RootElement.GetProperty("status").GetString().Should().Be(terminal ? "succeeded" : "accepted");
        var existing = (await store.GetByIdAsync(id)).ThrowIfNull();
        existing.InactivityTimeout.Should().Be(TimeSpan.FromMinutes(7));
        existing.TerminalRetention.Should().Be(TimeSpan.FromHours(3));
        count.Should().Be(1);

        Test.Type<TWebApi>().Run(w =>
        {
            w.ExecutionContext.User = w.ExecutionContext.User with { UserName = "Bob" };
            return w.PublishAsync(request, options);
        }).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.NotFound);
        count.Should().Be(1);
    }

    [Test]
    public async Task TrackedPublishing_CustomKeyFactoryUsesRequest()
    {
        ConfigureTrackedPublishing();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.Headers["X-Submission-Id"] = "submission-token";
        var count = 0;
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateWithEventFactory<Api.Entities.Person>((_, _) => Task.FromResult(Result.Ok(CoreEx.Events.EventData.CreateCommandWith("Test", new Api.Entities.Person { Id = "123" }, "submit"))))
            .WithOperationKey(ro => CoreEx.Entities.CompositeKey.Create(ro.Request.Headers["X-Submission-Id"].ToString()))
            .WithOperationOptions(new())
            .WithPublisher(_ => new RecordingPublisher(_ => count++));

        var first = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
        using var firstStatus = JsonDocument.Parse(first.GetContent()!);
        var id = firstStatus.RootElement.GetProperty("id").GetGuid();

        options.WithPublisher(_ => throw new InvalidOperationException("Duplicates must not resolve the publisher."));
        var duplicate = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
        using var duplicateStatus = JsonDocument.Parse(duplicate.GetContent()!);
        duplicateStatus.RootElement.GetProperty("id").GetGuid().Should().Be(id);
        count.Should().Be(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TrackedPublishing_RequestKeyUsesValidatedInput(bool readBody)
    {
        ConfigureTrackedPublishing();
        var count = 0;
        var ids = new List<Guid>();
        for (var i = 0; i < 2; i++)
        {
            var request = Test.CreateHttpRequest(HttpMethod.Post);
            var value = new Api.Entities.Person { Id = "123", FirstName = "repeatable-submission" };
            using var body = new System.IO.MemoryStream(JsonSerializer.SerializeToUtf8Bytes(value));
            request.Body = body;
            request.ContentLength = body.Length;
            request.ContentType = "application/json";
            request.Headers["X-Submission-Id"] = "request-context";
            var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.Create<Api.Entities.Person>()
                .WithOperationKey((ro, p) => CoreEx.Entities.CompositeKey.Create(p.FirstName, ro.Request.Headers["X-Submission-Id"].ToString()))
                .WithOperationOptions(new())
                .WithPublisher(_ => new RecordingPublisher(_ => count++));
            if (!readBody)
                options.WithValue(value);

            var response = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
            using var status = JsonDocument.Parse(response.GetContent()!);
            ids.Add(status.RootElement.GetProperty("id").GetGuid());
        }

        ids[1].Should().Be(ids[0]);
        count.Should().Be(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TrackedPublishing_MappedRequestKeyUsesValidatedInputBeforeMapping(bool readBody)
    {
        ConfigureTrackedPublishing();
        var stages = new List<string>();
        var publishedNames = new List<string>();
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var request = Test.CreateHttpRequest(HttpMethod.Post);
            var value = new MappedSubmission { Submission = "  repeatable-submission  ", Name = i == 0 ? "First" : i == 1 ? "Changed" : "Other" };
            using var body = new System.IO.MemoryStream(JsonSerializer.SerializeToUtf8Bytes(value));
            request.Body = body;
            request.ContentLength = body.Length;
            request.ContentType = "application/json";
            request.Headers["X-Submission-Id"] = i < 2 ? "context-a" : "context-b";
            var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateMapped(
                    CoreEx.Mapping.Mapper.Create<MappedSubmission, Api.Entities.Person>(input =>
                    {
                        stages.Add("map");
                        return new Api.Entities.Person { Id = $"mapped-{input.Name}", FirstName = input.Name };
                    }))
                .WithValidator(new MappedSubmissionValidator((input, _) =>
                {
                    stages.Add("validate");
                    input.Submission = input.Submission?.Trim();
                }))
                .WithOperationKey((ro, input) =>
                {
                    stages.Add("key");
                    ro.Request.Should().BeSameAs(request);
                    ro.OperationType.Should().Be(OperationType.Create);
                    return CoreEx.Entities.CompositeKey.Create(input.Submission, ro.Request.Headers["X-Submission-Id"].ToString());
                })
                .WithOperationOptions(new())
                .WithPublisher(_ => i == 1
                    ? throw new InvalidOperationException("Duplicates must not resolve the publisher.")
                    : new RecordingPublisher(events =>
                    {
                        using var payload = JsonDocument.Parse(events.Should().ContainSingle().Which.Event.Data.Should().BeOfType<BinaryData>().Which.ToMemory());
                        publishedNames.Add(payload.RootElement.GetProperty("firstName").GetString().ThrowIfNull());
                    }));

            if (!readBody)
                options.WithValue(value);

            var response = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
            using var status = JsonDocument.Parse(response.GetContent()!);
            ids.Add(status.RootElement.GetProperty("id").GetGuid());
        }

        stages.Should().Equal("validate", "key", "map", "validate", "key", "map", "validate", "key", "map");
        ids[1].Should().Be(ids[0]);
        ids[2].Should().NotBe(ids[0]);
        publishedNames.Should().Equal("First", "Other");
        var store = Test.Services.GetRequiredService<IOperationStore>();
        (await store.GetByKeyAsync("person-submit", CoreEx.Entities.CompositeKey.Create("repeatable-submission", "context-a"))).ThrowIfNull().OperationId.Should().Be(ids[0]);
        (await store.GetByKeyAsync("person-submit", CoreEx.Entities.CompositeKey.Create("repeatable-submission", "context-b"))).ThrowIfNull().OperationId.Should().Be(ids[2]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TrackedPublishing_MappedValidationFailureDoesNotSelectKeyOrPublish(bool readBody)
    {
        ConfigureTrackedPublishing();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var value = new MappedSubmission { Submission = "invalid" };
        using var body = new System.IO.MemoryStream(JsonSerializer.SerializeToUtf8Bytes(value));
        request.Body = body;
        request.ContentLength = body.Length;
        request.ContentType = "application/json";
        var keyCalls = 0;
        var mapCalls = 0;
        var publisherResolutions = 0;
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateMapped(
                CoreEx.Mapping.Mapper.Create<MappedSubmission, Api.Entities.Person>(input =>
                {
                    mapCalls++;
                    return new Api.Entities.Person { Id = "mapped-invalid", FirstName = input.Name };
                }))
            .WithValidator(new MappedSubmissionValidator((_, result) => result.Messages.Add(CoreEx.Entities.MessageItem.CreateErrorMessage("submission", "Invalid submission."))))
            .WithOperationKey((_, input) =>
            {
                keyCalls++;
                return CoreEx.Entities.CompositeKey.Create(input.Submission);
            })
            .WithOperationOptions(new())
            .WithPublisher(_ =>
            {
                publisherResolutions++;
                return new RecordingPublisher(_ => { });
            });

        if (!readBody)
            options.WithValue(value);

        Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.BadRequest);
        keyCalls.Should().Be(0);
        mapCalls.Should().Be(0);
        publisherResolutions.Should().Be(0);
        (await Test.Services.GetRequiredService<IOperationStore>().GetByKeyAsync("person-submit", CoreEx.Entities.CompositeKey.Create("invalid"))).Should().BeNull();
    }

    [Test]
    public async Task TrackedPublishing_MappedExplicitOperationKeyTakesPrecedence()
    {
        ConfigureTrackedPublishing();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var explicitKey = CoreEx.Entities.CompositeKey.Create("explicit");
        var count = 0;
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateMapped(
                CoreEx.Mapping.Mapper.Create<MappedSubmission, Api.Entities.Person>(input => new Api.Entities.Person { Id = "mapped-person", FirstName = input.Name }))
            .WithValue(new MappedSubmission { Submission = "derived-first", Name = "First" })
            .WithOperationKey((_, input) => CoreEx.Entities.CompositeKey.Create(input.Submission))
            .WithOperationOptions(new() { Key = explicitKey })
            .WithPublisher(_ => new RecordingPublisher(_ => count++));

        var first = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
        using var firstStatus = JsonDocument.Parse(first.GetContent()!);
        var id = firstStatus.RootElement.GetProperty("id").GetGuid();

        options.WithValue(new MappedSubmission { Submission = "derived-second", Name = "Changed" })
            .WithPublisher(_ => throw new InvalidOperationException("Duplicates must not resolve the publisher."));
        var duplicate = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
        using var duplicateStatus = JsonDocument.Parse(duplicate.GetContent()!);
        duplicateStatus.RootElement.GetProperty("id").GetGuid().Should().Be(id);
        count.Should().Be(1);

        var store = Test.Services.GetRequiredService<IOperationStore>();
        (await store.GetByKeyAsync("person-submit", explicitKey)).ThrowIfNull().OperationId.Should().Be(id);
        (await store.GetByKeyAsync("person-submit", CoreEx.Entities.CompositeKey.Create("derived-first"))).Should().BeNull();
        (await store.GetByKeyAsync("person-submit", CoreEx.Entities.CompositeKey.Create("derived-second"))).Should().BeNull();
    }

    [Test]
    public void TrackedPublishing_NoKeyAcceptsFreshSubmissionsAndDoesNotReadIdempotencyHeader()
    {
        ConfigureTrackedPublishing();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        request.Headers["Idempotency-Key"] = "not-an-operation-key";
        var count = 0;
        var ids = new List<Guid>();
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.Create(new Api.Entities.Person { Id = "123", FirstName = "Same entity identifier" })
            .WithOperationOptions(new())
            .WithPublisher(_ => new RecordingPublisher(_ => count++));
        for (var i = 0; i < 2; i++)
        {
            var response = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
            using var status = JsonDocument.Parse(response.GetContent()!);
            ids.Add(status.RootElement.GetProperty("id").GetGuid());
        }

        ids[1].Should().NotBe(ids[0]);
        count.Should().Be(2);
    }

    [Test]
    public async Task TrackedPublishing_DirectSendFailureReturnsErrorAndDoesNotResendRetainedOperation()
    {
        ConfigureTrackedPublishing();
        var request = Test.CreateHttpRequest(HttpMethod.Post);
        var key = CoreEx.Entities.CompositeKey.Create("failed-direct-send");
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.Create(new Api.Entities.Person { Id = "123" })
            .WithOperationOptions(new() { Key = key })
            .WithPublisher(_ => new RecordingPublisher(_ => throw new InvalidOperationException("Unknown delivery outcome.")));

        Test.Type<TWebApi>().Run(w =>
        {
            w.ConvertUnhandledExceptionsToProblemDetails = true;
            return w.PublishAsync(request, options);
        }).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.InternalServerError);

        var retained = (await Test.Services.GetRequiredService<IOperationStore>().GetByKeyAsync("person-submit", key)).ThrowIfNull();
        retained.Status.Should().Be(CoreEx.Operations.OperationStatus.Accepted);
        options.WithPublisher(_ => throw new InvalidOperationException("A retained operation must not be sent again."));
        var retried = Test.Type<TWebApi>().Run(w => w.PublishAsync(request, options)).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.Accepted);
        using var status = JsonDocument.Parse(retried.GetContent()!);
        status.RootElement.GetProperty("id").GetGuid().Should().Be(retained.OperationId);
    }

    private void ConfigureTrackedPublishing()
    {
        Test.ConfigureServices(services =>
        {
            services.AddSingleton<IOperationStore, InMemoryOperationStore>();
            services.AddExecutionContext((_, ec) => ec.User = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "bob", Id = "not-bob" });
        });
    }

    private sealed class MappedSubmission
    {
        public string? Submission { get; set; }

        public string? Name { get; set; }
    }

    private sealed class MappedSubmissionValidator(Action<MappedSubmission, MappedSubmissionValidationResult> validate) : CoreEx.Validation.IValidator<MappedSubmission>
    {
        public Task<CoreEx.Validation.IValidationResult<MappedSubmission>> ValidateAsync(MappedSubmission value, CancellationToken cancellationToken = default)
        {
            var result = new MappedSubmissionValidationResult(value);
            validate(value, result);
            return Task.FromResult<CoreEx.Validation.IValidationResult<MappedSubmission>>(result);
        }
    }

    private sealed class MappedSubmissionValidationResult(MappedSubmission value) : CoreEx.Validation.MultiValidatorResult, CoreEx.Validation.IValidationResult<MappedSubmission>
    {
        public MappedSubmission Value => value;
    }

    private sealed class RecordingPublisher(Action<CoreEx.Events.Publishing.DestinationEvent[]> publish) : CoreEx.Events.Publishing.NoOpEventPublisher
    {
        protected override Task OnPublishAsync(CoreEx.Events.Publishing.DestinationEvent[] events, CancellationToken cancellationToken = default)
        {
            publish(events);
            return Task.CompletedTask;
        }
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

}
