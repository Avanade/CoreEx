using CoreEx.AspNetCore.Idempotency;
using CoreEx.Caching;
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

        // First request, should cache.
        Test.Http()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/1", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Bob\"}");

        // Second request, same Idempotency-Key and same request, should replay.
        Test.Http()
            .ExpectLogContains($"Idempotent request with key '{ik}' has resulted in the response being replayed from the cache.")
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/1", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Bob\"}");

        // Third request, same Idempotency-Key but different request, should conflict.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/2", hrm => hrm.WithIdempotencyKey(ik))
            .Assert(HttpStatusCode.UnprocessableEntity)
            .Value!.Title.Should().Be("The 'Idempotency-Key' header has already been used for a different request.");

        // Verify the cache contents.
        Test.ScopedType<IHybridCache>(async test =>
        {
            var data = await test.Service.GetOrCreateByKeyAsync<IdempotencyKey>("Idempotency:" + ik, _ => throw new InvalidOperationException("Must not be empty!"));
            data.Should().NotBeNull();
            data.Status.Should().Be(IdempotencyStatus.CompletedAndReplayable);
            data.StatusCode.Should().Be((int)HttpStatusCode.Created);
            data.Headers.Should().ContainKey("Content-Type").WhoseValue.Should().Contain("application/json");
            data.Body.Should().NotBeNull();
            data.Body.ToString().Should().NotBeNull().And.Be("{\"id\":1,\"name\":\"Bob\"}");
        });
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
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/1", hrm => hrm.WithIdempotencyKey("inv@lid_key!"))
            .AssertBadRequest()
            .Value!.Title.Should().Be("The 'Idempotency-Key' header is invalid.");
    }

    [Test]
    public void Idempotency_Mvc_NotFound_EmptyCache()
    {
        var ik = Guid.NewGuid().ToString();

        // First request, should cache/remove due to error.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/88", hrm => hrm.WithIdempotencyKey(ik))
            .AssertNotFound();

        // Second request, should cache/remove due to error.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/other/idempotency-mvc/88", hrm => hrm.WithIdempotencyKey(ik))
            .AssertNotFound();

        // Verify that the cache does not contain the idempotency key.
        Test.ScopedType<IHybridCache>(async test =>
        {
            var exists = true;
            var entry = await test.Service.GetOrCreateByKeyAsync(ik, _ =>
            {
                exists = false;
                return Task.FromResult(new object());
            });

            exists.Should().BeFalse();
        });
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

        // First request, should cache.
        Test.Http()
            .Run(HttpMethod.Post, "/api/idempotency-key/test/1", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Jen\"}");

        // Second request, same Idempotency-Key and same request, should replay.
        Test.Http()
            .ExpectLogContains($"Idempotent request with key '{ik}' has resulted in the response being replayed from the cache.")
            .Run(HttpMethod.Post, "/api/idempotency-key/test/1", hrm => hrm.WithIdempotencyKey(ik))
            .AssertCreated()
            .AssertJson("{\"id\":1,\"name\":\"Jen\"}");

        // Third request, same Idempotency-Key but different request, should conflict.
        Test.Http<ProblemDetails>()
            .Run(HttpMethod.Post, "/api/idempotency-key/test/2", hrm => hrm.WithIdempotencyKey(ik))
            .Assert(HttpStatusCode.UnprocessableEntity)
            .Value!.Title.Should().Be("The 'Idempotency-Key' header has already been used for a different request.");

        // Verify the cache contents.
        Test.ScopedType<IHybridCache>(async test =>
        {
            var data = await test.Service.GetOrCreateByKeyAsync<IdempotencyKey>("Idempotency:" + ik, _ => throw new InvalidOperationException("Must not be empty!"));
            data.Should().NotBeNull();
            data.Status.Should().Be(IdempotencyStatus.CompletedAndReplayable);
            data.StatusCode.Should().Be((int)HttpStatusCode.Created);
            data.Headers.Should().ContainKey("Content-Type").WhoseValue.Should().Contain("application/json");
            data.Body.Should().NotBeNull();
            data.Body.ToString().Should().NotBeNull().And.Be("{\"id\":1,\"name\":\"Jen\"}");
        });
    }
}