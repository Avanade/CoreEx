using CoreEx.AspNetCore.Idempotency;
using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.Caching;
using CoreEx.Entities;
using CoreEx.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using UnitTestEx.Expectations;

namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture]
public class OtherApiTests : WithApiTester<Api.Program>
{
    [Test]
    public void ExecutionContextMiddleware_Messages()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/api/other/messages")
            .AssertNoContent()
            .Response.Headers.Should().ContainKey(HttpNames.WarningMessagesHeaderName)
                .WhoseValue.Should().BeEquivalentTo("Please pay your invoice.");
    }

    [Test]
    public async Task UnhandledException_ProblemHandling()
    {
        var ra = Test.Http()
            .Run(HttpMethod.Post, "/api/other/unhandledexception", hrm =>
            {
                hrm.Headers.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
                hrm.Headers.Add("tracestate", "foo=bar");
            })
            .Assert(HttpStatusCode.InternalServerError)
            .AssertJson("{\"title\":\"Oh no, that was unexpected!\",\"status\":500}", pathsToIgnore: ["type", "traceId", "errorCode", "detail"]);

        var cpd = await ra.Response.ToProblemDetailsAsync();
        cpd.Should().NotBeNull();
        cpd.ProblemDetails.Type.Should().NotBeNull();
        cpd.ProblemDetails.Title.Should().Be("Oh no, that was unexpected!");
        cpd.ProblemDetails.Status.Should().Be(500);
        cpd.ProblemDetails.Extensions.Should().ContainKey("traceId").WhoseValue.Should().BeOfType<JsonElement>();
        cpd.ProblemDetails.Extensions["traceId"].As<JsonElement>().ToString().Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");

        var pd = ra.GetValue<Microsoft.AspNetCore.Mvc.ProblemDetails>(System.Net.Mime.MediaTypeNames.Application.ProblemJson);
        pd.Should().NotBeNull();
        pd.Type.Should().NotBeNull();
        pd.Extensions.Should().ContainKey("traceId").WhoseValue.Should().BeOfType<JsonElement>();
        pd.Extensions["traceId"].As<JsonElement>().ToString().Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");
    }

    [Test]
    public async Task UnhandledExtendedException_ProblemHandling()
    {
        var ra = Test.Http()
            .Run(HttpMethod.Post, "/api/other/unhandledextendedexception", hrm =>
            {
                hrm.Headers.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
                hrm.Headers.Add("tracestate", "foo=bar");
            })
            .Assert(HttpStatusCode.Conflict)
            .AssertJson("{\"title\":\"Oh my, we have one of those already!\",\"status\":409,\"errorType\":\"duplicate\"}", pathsToIgnore: ["type", "traceId", "errorCode", "detail"]);

        var cpd = await ra.Response.ToProblemDetailsAsync();
        cpd.Should().NotBeNull();
        cpd.ProblemDetails.Type.Should().NotBeNull();
        cpd.ProblemDetails.Title.Should().Be("Oh my, we have one of those already!");
        cpd.ProblemDetails.Status.Should().Be(409);
        cpd.ProblemDetails.Extensions.Should().ContainKey("traceId").WhoseValue.Should().BeOfType<JsonElement>();
        cpd.ProblemDetails.Extensions["traceId"].As<JsonElement>().ToString().Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");

        var dex = cpd.ToException<DuplicateException>();
        dex.Should().NotBeNull();
        dex.Message.Should().Be("Oh my, we have one of those already!");
        dex.ErrorType.Should().Be("duplicate");

        var pd = ra.GetValue<Microsoft.AspNetCore.Mvc.ProblemDetails>(System.Net.Mime.MediaTypeNames.Application.ProblemJson);
        pd.Should().NotBeNull();
        pd.Type.Should().NotBeNull();
        pd.Extensions.Should().ContainKey("traceId").WhoseValue.Should().BeOfType<JsonElement>();
        pd.Extensions["traceId"].As<JsonElement>().ToString().Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");
    }

    [Test]
    public void Swagger_UI()
    {
        Test.Http()
            .Run(HttpMethod.Get, "/swagger")
            .Assert(HttpStatusCode.Found)
            .AssertLocationHeader(new Uri("/swagger/index.html", UriKind.Relative));

        var html = Test.Http()
            .Run(HttpMethod.Get, "/swagger/index.html")
            .Assert(HttpStatusCode.OK)
            .GetContent();

        html.Should().Contain("<title>Swagger UI</title>");
    }

    [Test]
    public void Swagger_JSON()
    {
        var json = Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .AssertContentTypeJson()
            .GetContent();

        json.Should().Contain("CoreEx.AspNetCore.Test.Api");
        json.Should().Contain("\"etag\""); // Asserts that JsonSubstituteNamingPolicy is in effect.
    }

    [Test]
    public void Swagger_JSON_AcceptsAttribute_RequestBodyRequired()
    {
        // Regression: AcceptsAttribute.IsOptional previously had zero effect on the generated spec - a required body (the default, IsOptional false) was
        // undocumented (no "required" key at all, matching OpenAPI 3's own default-omission convention for required: false), indistinguishable from an
        // explicitly optional one.
        var json = Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .GetContent();

        using var doc = JsonDocument.Parse(json!);
        var required = doc.RootElement.GetProperty("paths").GetProperty("/api/people").GetProperty("post").GetProperty("requestBody").GetProperty("required");
        required.GetBoolean().Should().BeTrue();

        var optionalBody = doc.RootElement.GetProperty("paths").GetProperty("/api/other/optional-body").GetProperty("post").GetProperty("requestBody");
        optionalBody.TryGetProperty("required", out _).Should().BeFalse();
    }

    [Test]
    public void Swagger_JSON_PublishMetadata_DistinguishesTrackedAndUntrackedEndpoints()
    {
        var json = Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .GetContent();

        using var doc = JsonDocument.Parse(json!);
        var paths = doc.RootElement.GetProperty("paths");
        var tracked = paths.GetProperty("/api/other/publish-tracked").GetProperty("post");
        var trackedResponse = tracked.GetProperty("responses").GetProperty("202");

        trackedResponse.GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()
            .Should().EndWith("/OperationStatus");
        var statusProperties = doc.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("OperationStatus").GetProperty("properties");
        statusProperties.EnumerateObject().Select(x => x.Name).Should().Contain(["createdOn", "updatedOn", "expiryOn"]);
        statusProperties.EnumerateObject().Select(x => x.Name).Should().NotContain(["createdAt", "lastUpdatedAt", "expiresAt"]);
        trackedResponse.GetProperty("headers").EnumerateObject().Select(x => x.Name)
            .Should().Contain(["Location", "Operation-Location", "Retry-After"]);
        trackedResponse.GetProperty("links").GetProperty("getOperation").GetProperty("operationId").GetString()
            .Should().Be("CoreExOperationStatus");
        tracked.GetProperty("x-ms-long-running-operation").GetBoolean().Should().BeTrue();
        tracked.GetProperty("x-ms-long-running-operation-options").GetProperty("final-state-via").GetString()
            .Should().Be("operation-location");
        tracked.GetProperty("responses").TryGetProperty("200", out _).Should().BeFalse();

        var minimalTracked = paths.GetProperty("/api/other/minimal-publish-tracked").GetProperty("post");
        minimalTracked.GetProperty("responses").GetProperty("202").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString().Should().EndWith("/OperationStatus");
        minimalTracked.GetProperty("x-ms-long-running-operation").GetBoolean().Should().BeTrue();

        var untracked = paths.GetProperty("/api/other/publish-untracked").GetProperty("post");
        var untrackedResponse = untracked.GetProperty("responses").GetProperty("202");
        untrackedResponse.TryGetProperty("content", out _).Should().BeFalse();
        untrackedResponse.TryGetProperty("links", out _).Should().BeFalse();
        untracked.TryGetProperty("x-ms-long-running-operation", out _).Should().BeFalse();
        untracked.GetProperty("responses").TryGetProperty("200", out _).Should().BeFalse();

        var minimalUntracked = paths.GetProperty("/api/other/minimal-publish-command").GetProperty("post");
        minimalUntracked.GetProperty("responses").GetProperty("202").TryGetProperty("content", out _).Should().BeFalse();
        minimalUntracked.TryGetProperty("x-ms-long-running-operation", out _).Should().BeFalse();
        minimalUntracked.GetProperty("responses").TryGetProperty("200", out _).Should().BeFalse();

        var statusOperation = paths.GetProperty("/operations/{id}").GetProperty("get");
        statusOperation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()).Should().Contain("Operations");
        var status = statusOperation.GetProperty("responses");
        status.GetProperty("200").GetProperty("headers").TryGetProperty("ETag", out _).Should().BeTrue();
        status.GetProperty("304").GetProperty("headers").TryGetProperty("ETag", out _).Should().BeTrue();
        var cancel = paths.GetProperty("/operations/{id}:cancel").GetProperty("post").GetProperty("responses").GetProperty("202").GetProperty("headers");
        cancel.EnumerateObject().Select(x => x.Name).Should().Contain(["Location", "Operation-Location", "Retry-After"]);
    }

    [Test]
    public void Publish_Default_PublishesSelfAddressedCommandWithPublishAction()
    {
        using var test = CreatePublishTester();
        var response = test.Http()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                actual.Event.Type.Should().Be("coreex.test.person.publish.v1");
                actual.Event["operationid"].Should().BeNull();
            }))
            .Run(HttpMethod.Post, "/api/other/minimal-publish-command", new Person { Id = "1", FirstName = "Bob" })
            .AssertAccepted();

        response.GetContent().Should().BeNullOrEmpty();
        response.Response.Headers.Location.Should().BeNull();
        response.Response.Headers.Contains("Operation-Location").Should().BeFalse();
    }

    [TestCase("/api/other/publish-tracked")]
    [TestCase("/api/other/minimal-publish-tracked")]
    public async Task Publish_TrackedEndpoint_CreatesAndPublishesOperation(string path)
    {
        var provider = new CoreEx.Hosting.Work.InMemoryWorkProvider();
        var owner = new CoreEx.Security.AuthenticationUser { Type = CoreEx.Security.AuthenticationType.AccountUser, UserName = "owner" };
        using var test = CreatePublishTester(services =>
        {
            services.AddSingleton(new CoreEx.Hosting.Work.WorkOrchestrator(provider));
            services.AddExecutionContext((_, ec) => ec.User = owner);
        });

        var domain = test.Services.GetRequiredService<CoreEx.Hosting.IHostSettings>().DomainName;
        string? publishedId = null;
        DateTimeOffset? publishedStartBy = null;
        var response = test.Http()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) =>
            {
                actual.Event.Type.Should().Be("coreex.test.person.publish.v1");
                publishedId = actual.Event["operationid"].Should().BeOfType<string>().Subject;
                actual.Event["operationdomain"].Should().Be(domain);
                actual.Event["operationscope"].Should().Be("domain");
                actual.Event["operationname"].Should().Be("webapi-operations");
                publishedStartBy = DateTimeOffset.Parse(actual.Event["operationstartby"].Should().BeOfType<string>().Subject, System.Globalization.CultureInfo.InvariantCulture);
                using var payload = JsonDocument.Parse(actual.Event.Data.Should().BeOfType<BinaryData>().Which.ToMemory());
                payload.RootElement.GetProperty("id").GetString().Should().Be("1");
                payload.RootElement.GetProperty("firstName").GetString().Should().Be("Bob");
            }))
            .Run(HttpMethod.Post, path, new Person { Id = "1", FirstName = "Bob" })
            .AssertAccepted();

        using var status = JsonDocument.Parse(response.GetContent().ThrowIfNull());
        var id = status.RootElement.GetProperty("id").GetString().ThrowIfNull();
        id.Should().NotBeNullOrWhiteSpace();
        publishedId.Should().Be(id);
        status.RootElement.GetProperty("status").GetString().Should().Be("not-started");
        var location = $"/operations/{id}";
        response.Response.Headers.Location.ThrowIfNull().ToString().Should().Be(location);
        response.Response.Headers.GetValues("Operation-Location").Should().ContainSingle().Which.Should().Be(location);

        var key = OperationKey.ForDomain(domain, "webapi-operations", id);
        var state = (await provider.GetAsync(key, CancellationToken.None)).State.ThrowIfNull();
        state.TypeName.Should().Be(typeof(Person).FullName);
        state.Status.Should().Be(CoreEx.Hosting.Work.WorkStatus.Created);
        state.User.Should().Be(owner);
        state.StartBy.Should().Be(publishedStartBy);

        var poll = test.Http().Run(HttpMethod.Get, location).AssertOK();
        using var polledStatus = JsonDocument.Parse(poll.GetContent().ThrowIfNull());
        polledStatus.RootElement.GetProperty("id").GetString().Should().Be(id);
        polledStatus.RootElement.GetProperty("status").GetString().Should().Be("not-started");
    }

    [Test]
    public void Publish_Event_PublishesToSharedDestinationWithPublishedAction()
    {
        using var test = CreatePublishTester();
        test.Http()
            .ExpectEvents("EventPublisher", cfg => cfg.AssertCustom("default", (_, _, actual) => actual.Event.Type.Should().Be("coreex.sales.person.published.v1")))
            .Run(HttpMethod.Post, "/api/other/minimal-publish-event", new Person { Id = "1", FirstName = "Bob" })
            .AssertAccepted();
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Publish_CoreExMapper_ResolvesPublisherFromSelectedServices(bool readBody, bool useExecutionContextServices)
    {
        using var test = CreatePublishTester();
        using var scope = test.Services.CreateScope();
        using var executionScope = test.Services.CreateScope();
        var expectedServices = useExecutionContextServices ? executionScope.ServiceProvider : scope.ServiceProvider;
        var executionContext = new CoreEx.ExecutionContext
        {
            ServiceProvider = useExecutionContextServices ? executionScope.ServiceProvider : null
        };
        var webApi = new WebApi(executionContext: executionContext);
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var value = new Person { Id = "1", FirstName = "Bob" };
        var request = context.Request;
        request.Method = HttpMethods.Post;
        request.ContentType = "application/json";
        var body = JsonSerializer.SerializeToUtf8Bytes(value);
        using var stream = new System.IO.MemoryStream(body);
        request.Body = stream;
        request.ContentLength = body.Length;
        Person? published = null;
        var options = CoreEx.AspNetCore.Publishing.WebApiPublishOptions.CreateMapped<Person, Person>(
                CoreEx.Mapping.Mapper.Create<Person, Person>(source =>
                {
                    var mapped = new Person { Id = source.Id, FirstName = $"Mapped {source.FirstName}" };
                    published = mapped;
                    return mapped;
                }))
            .WithEventFactory(person => CoreEx.Events.EventData.CreateEventWith(person, CoreEx.Events.EventAction.Published).WithSource(new Uri("urn:coreex:test")))
            .WithPublisher(services =>
            {
                services.Should().BeSameAs(expectedServices);
                return services.GetRequiredKeyedService<CoreEx.Events.Publishing.IEventPublisher>("EventPublisher");
            });

        var result = readBody
            ? await webApi.PublishAsync(request, options)
            : await webApi.PublishAsync(request, options.WithValue(value));

        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(202);
        published.Should().BeOfType<Person>().Which.FirstName.Should().Be("Mapped Bob");
        published.Should().NotBeSameAs(value);
        expectedServices.GetRequiredKeyedService<CoreEx.Events.Publishing.IEventPublisher>("EventPublisher").HasBeenPublished.Should().BeTrue();
    }

    /// <summary>
    /// Creates an isolated tester that captures publications without broker infrastructure.
    /// </summary>
    private static UnitTestEx.AspNetCore.ApiTester<Api.Program> CreatePublishTester(Action<IServiceCollection>? configure = null)
    {
        var test = ApiTester.Create<Api.Program>();
        test.ConfigureServices(services =>
        {
            services
                .AddEventPublisher("EventPublisher", sp => ActivatorUtilities.CreateInstance<CoreEx.Events.Publishing.NoOpEventPublisher>(sp))
                .UseExpectedEventPublisher();

            configure?.Invoke(services);
        });
        return test;
    }

    [Test]
    public void OperationStatus_DatesSerializeWithOnSuffix()
    {
        var timestamp = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var status = new CoreEx.AspNetCore.Operations.OperationStatus
        {
            Id = "123",
            Status = "not-started",
            CreatedOn = timestamp,
            UpdatedOn = timestamp.AddMinutes(1),
            ExpiryOn = timestamp.AddHours(1)
        };
        var json = JsonSerializer.Serialize(status, CoreEx.Json.JsonDefaults.SerializerOptions);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("createdOn").GetDateTimeOffset().Should().Be(status.CreatedOn);
        document.RootElement.GetProperty("updatedOn").GetDateTimeOffset().Should().Be(status.UpdatedOn);
        document.RootElement.GetProperty("expiryOn").GetDateTimeOffset().Should().Be(status.ExpiryOn);
        document.RootElement.TryGetProperty("createdAt", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("lastUpdatedAt", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("expiresAt", out _).Should().BeFalse();
    }

    [Test]
    public void Swagger_JSON_NonNullableAttribute_NotNullable()
    {
        // Properties marked [NonNullable] are non-nullable in the spec even though declared nullable (string? / DateOnly?); unmarked ones remain nullable.
        var json = Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .GetContent();

        using var doc = JsonDocument.Parse(json!);
        var props = doc.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("Person").GetProperty("properties");

        props.GetProperty("firstName").GetProperty("nullable").GetBoolean().Should().BeFalse();
        props.GetProperty("birthday").GetProperty("nullable").GetBoolean().Should().BeFalse();
        props.GetProperty("lastName").GetProperty("nullable").GetBoolean().Should().BeTrue();
    }

    [Test]
    public void Swagger_JSON_NonNullableAttribute_DataMapValue_NotNullable()
    {
        var json = Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .GetContent();

        using var doc = JsonDocument.Parse(json!);
        var props = doc.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("NonNullableMapValue").GetProperty("properties");

        props.GetProperty("requiredCode").GetProperty("nullable").GetBoolean().Should().BeFalse();
        props.GetProperty("optionalCode").GetProperty("nullable").GetBoolean().Should().BeTrue();
    }

    [Test]
    public void Swagger_JSON_OptionalProperties_AreNotRequired_AndKeepInnerNullability()
    {
        var json = Test.Http()
            .Run(HttpMethod.Get, "/swagger/v1/swagger.json")
            .Assert(HttpStatusCode.OK)
            .GetContent();

        using var doc = JsonDocument.Parse(json!);
        var schema = doc.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("OptionalPatchRequest");
        var properties = schema.GetProperty("properties");

        schema.TryGetProperty("required", out _).Should().BeFalse();
        properties.GetProperty("title").GetProperty("type").GetString().Should().Be("string");
        properties.GetProperty("title").GetProperty("nullable").GetBoolean().Should().BeFalse();
        properties.GetProperty("notes").GetProperty("nullable").GetBoolean().Should().BeTrue();
        properties.GetProperty("count").GetProperty("type").GetString().Should().Be("integer");
        properties.GetProperty("count").GetProperty("nullable").GetBoolean().Should().BeFalse();
        properties.GetProperty("nullableCount").GetProperty("type").EnumerateArray().Select(x => x.GetString()).Should().BeEquivalentTo("integer", "null");
        properties.GetProperty("nullableCount").GetProperty("nullable").GetBoolean().Should().BeTrue();
        properties.GetProperty("tags").GetProperty("type").GetString().Should().Be("array");
        properties.GetProperty("tags").GetProperty("items").GetProperty("type").GetString().Should().Be("string");
        properties.GetProperty("gender").GetProperty("type").EnumerateArray().Select(x => x.GetString()).Should().BeEquivalentTo("null", "string");
        properties.GetProperty("gender").GetProperty("nullable").GetBoolean().Should().BeTrue();
        properties.TryGetProperty("genderCode", out _).Should().BeFalse();
    }

    [Test]
    public void OptionalReferenceDataNavigation_SharesCodePresence()
    {
        var request = new OptionalPatchRequest();

        request.Gender.IsSpecified.Should().BeFalse();
        request.Gender = Optional<Gender?>.Specified(null);
        request.GenderCode.IsSpecified.Should().BeTrue();
        request.GenderCode.Value.Should().BeNull();
        request.Gender = default;
        request.GenderCode.IsSpecified.Should().BeFalse();

        request.GenderCode = Optional<string?>.Specified(null);
        request.Gender.IsSpecified.Should().BeTrue();
        request.Gender.Value.Should().BeNull();
    }

    [TestCase("/health/live")]
    [TestCase("/health/startup")]
    [TestCase("/health/ready")]
    public void Health_Lite(string path)
    {
        Test.Http()
            .Run(HttpMethod.Get, path)
            .Assert(HttpStatusCode.OK)
            .AssertContentTypePlainText()
            .AssertContent("Healthy");
    }

    [TestCase("/health/live/detailed")]
    [TestCase("/health/startup/detailed")]
    [TestCase("/health/ready/detailed")]
    public void Health_Detailed(string path)
    {
        var json = Test.Http()
            .Run(HttpMethod.Get, path)
            .Assert(HttpStatusCode.OK)
            .AssertContentTypeJson()
            .GetContent();

        json.Should().Contain("Healthy");
    }

    [Test]
    public void Idempotency_Mvc_Reuse_Key()
    {
        var ik = Guid.NewGuid().ToString();

        // First request executes and commits a replayable response.
        Test.Http()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/1", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Bob\"}");

        // Second request with the same logical key and request replays the committed response.
        Test.Http()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/1", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Bob\"}");

        // Third request, same Idempotency-Key but different request, should conflict.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/2", hrm => hrm.WithIdempotencyKey(ik))
            .Assert(HttpStatusCode.UnprocessableEntity)
            .Value!.Title.Should().Be("The 'Idempotency-Key' header has already been used for a different request.");

    }

    [Test]
    public void Idempotency_Mvc_KeyMissing_Success()
    {
        Test.Http()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/1")
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Bob\"}");
    }

    [Test]
    public void Idempotency_Mvc_KeyInvalid_Failure()
    {
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/1", hrm => hrm.Headers.TryAddWithoutValidation(CoreEx.Http.HttpNames.IdempotencyKeyHeaderName, "inv@lid_key!"))
            .AssertBadRequest()
            .Value!.Title.Should().Be("The 'Idempotency-Key' header is invalid.");
    }

    [Test]
    public void Idempotency_Mvc_NotFound_IsACompletedReplayableResponse()
    {
        var ik = Guid.NewGuid().ToString();

        // A normally rendered application 404 is recorded as a completed outcome.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/88", hrm => hrm.WithIdempotencyKey(ik))
            .AssertNotFound();

        // The duplicate request receives the same terminal status without a new attempt.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/88", hrm => hrm.WithIdempotencyKey(ik))
            .AssertNotFound();
    }

    [Test]
    public void Idempotency_Mvc_Response_Too_Large()
    {
        var ik = Guid.NewGuid().ToString();

        // First request, should cache as too-large..
        Test.Http()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/99", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated();

        // Second request, same Idempotency-Key and same request, should conflict due to response too large.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/99", hrm => hrm.WithIdempotencyKey(ik))
            .AssertConflict()
            .Value!.Title.Should().Be("The response associated with the specified 'Idempotency-Key' is no longer available.");
    }

    [Test]
    public void Idempotency_Http_Reuse_Key()
    {
        var ik = Guid.NewGuid().ToString();

        // First request executes and commits a replayable response.
        Test.Http()
            .Run(HttpMethod.Post, "/api/idempotency-key/test/1", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Jen\"}");

        // Second request with the same logical key and request replays the committed response.
        Test.Http()
            .Run(HttpMethod.Post, "/api/idempotency-key/test/1", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Jen\"}");

        // Third request, same Idempotency-Key but different request, should conflict.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/idempotency-key/test/2", hrm => hrm.WithIdempotencyKey(ik))
            .Assert(HttpStatusCode.UnprocessableEntity)
            .Value!.Title.Should().Be("The 'Idempotency-Key' header has already been used for a different request.");

    }
}
