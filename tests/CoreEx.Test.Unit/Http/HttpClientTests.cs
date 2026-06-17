using CoreEx.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using UnitTestEx.Expectations;

namespace CoreEx.Test.Unit.Http;

[TestFixture]
public class HttpClientTests
{
    [Test]
    public void Resiliency_And_No_IdempotencyKey()
    {
        var mcf = MockHttpClientFactory.Create();
        var mapi = mcf.CreateClient("UnitTest").WithConfigurations();

        mapi.Request(HttpMethod.Get, "/test-endpoint")
            .Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.InternalServerError);
                s.Respond().With(HttpStatusCode.ServiceUnavailable);
                s.Respond().With(HttpStatusCode.RequestTimeout);
                s.Respond().With(HttpStatusCode.NoContent);
            });

        mapi.Request(HttpMethod.Get, "/test-endpoint-2")
            .Respond.With(HttpStatusCode.NotFound);

        using var test = GenericTester.Create<EntryPoint>();
        test.ReplaceHttpClientFactory(mcf);

        var client = test.Services.GetRequiredService<IHttpClientFactory>().CreateClient("UnitTest");

        test.ExpectLogContains("Result: '500', Handled: 'True', Attempt: '0'")
            .ExpectLogContains("Result: '503', Handled: 'True', Attempt: '1'")
            .ExpectLogContains("Result: '408', Handled: 'True', Attempt: '2'")
            .ExpectLogContains("Result: '204', Handled: 'False', Attempt: '3'")
            .Run(async () =>
            {
                var response = await client.GetAsync("/test-endpoint");
                var ik = response.RequestMessage!.Headers.GetValues(HttpNames.IdempotencyKeyHeaderName).FirstOrDefault();
                ik.Should().BeNull();
            });

        test.ExpectLogContains("Result: '404', Handled: 'False', Attempt: '0'")
            .Run(async () =>
            {
                var response = await client.GetAsync("/test-endpoint-2");
                var ik = response.RequestMessage!.Headers.GetValues(HttpNames.IdempotencyKeyHeaderName).FirstOrDefault();
                ik.Should().BeNull();
            });

        mapi.Verify();
    }

    [Test]
    public void Resiliency_And_IdempotencyKey()
    {
        var mcf = MockHttpClientFactory.Create();
        var mapi = mcf.CreateClient("UnitTest").WithConfigurations();

        mapi.Request(HttpMethod.Post, "/test-endpoint")
            .Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.InternalServerError);
                s.Respond().With(HttpStatusCode.ServiceUnavailable);
                s.Respond().With(HttpStatusCode.RequestTimeout);
                s.Respond().With(HttpStatusCode.NoContent);
            });

        mapi.Request(HttpMethod.Post, "/test-endpoint-2")
            .Respond.With(HttpStatusCode.NotFound);

        using var test = GenericTester.Create<EntryPoint>();
        test.ReplaceHttpClientFactory(mcf);

        var client = test.Services.GetRequiredService<IHttpClientFactory>().CreateClient("UnitTest");
        string? lastIdempotencyKey = null;

        test.ExpectLogContains("Result: '500', Handled: 'True', Attempt: '0'")
            .ExpectLogContains("Result: '503', Handled: 'True', Attempt: '1'")
            .ExpectLogContains("Result: '408', Handled: 'True', Attempt: '2'")
            .ExpectLogContains("Result: '204', Handled: 'False', Attempt: '3'")
            .Run(async () =>
            {
                var response = await client.PostAsync("/test-endpoint", null);
                var ik = response.RequestMessage!.Headers.GetValues(HttpNames.IdempotencyKeyHeaderName).FirstOrDefault();
                ik.Should().NotBeNullOrEmpty();
                lastIdempotencyKey = ik;
            });

        test.ExpectLogContains("Result: '404', Handled: 'False', Attempt: '0'")
            .Run(async () =>
            {
                var response = await client.PostAsync("/test-endpoint-2", null);
                var ik = response.RequestMessage!.Headers.GetValues(HttpNames.IdempotencyKeyHeaderName).FirstOrDefault();
                ik.Should().NotBeNullOrEmpty();
                ik.Should().NotBe(lastIdempotencyKey);
            });

        mapi.Verify();
    }

    public class EntryPoint
    {
        public static void ConfigureApplication(IHostApplicationBuilder builder)
        {
            builder.Services.AddHttpClient("UnitTest", static client =>
            {
                client.BaseAddress = new Uri("http://unittest");
            })
            .AddIdempotencyKeyHandler()
            .AddStandardResilienceHandler(); // https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience?tabs=dotnet-cli#standard-resilience-handler-defaults
        }
    }
}