using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Http.HealthChecks;
using CoreEx.TestFunction;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace CoreEx.Test.HealthChecks
{

    [TestFixture, NonParallelizable]
    public class TypedHttpClientCoreHealthCheckTests
    {
        [Test]
        public async Task CheckHealthAsync_Should_Succeed_When_SampleApiOK()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Backend", "https://backend/").Request(HttpMethod.Head, string.Empty).Respond.With(statusCode: HttpStatusCode.OK);
            using var test = FunctionTester.Create<Startup>()
                .ReplaceHttpClientFactory(mcf);
            var mock = new Mock<IHealthCheck>();

            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("test", mock.Object, null, null)
            };

            // Act
            var result = await test.Type<TypedHttpClientCoreHealthCheck<BackendHttpClient>>()
             .RunAsync(x => x.CheckHealthAsync(context, CancellationToken.None));

            // Assert
            result.Result.Status.Should().Be(HealthStatus.Healthy, because: "Sample API is OK");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Fail_When_SampleApiDown()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Backend", "https://backend/").Request(HttpMethod.Head, string.Empty).Respond.With(statusCode: HttpStatusCode.ServiceUnavailable);
            using var test = FunctionTester.Create<Startup>()
                .UseJsonSerializer(new CoreEx.Text.Json.JsonSerializer()) // Required as the Result type needs to be deserialized using CoreEx.
                .ReplaceHttpClientFactory(mcf);
            var mock = new Mock<IHealthCheck>();

            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("test", mock.Object, null, null)
            };

            // Act
            var result = await test.Type<TypedHttpClientCoreHealthCheck<BackendHttpClient>>()
             .RunAsync(x => x.CheckHealthAsync(context, CancellationToken.None));

            // Assert
            result.Result.Status.Should().Be(HealthStatus.Unhealthy, because: "Sample API return 502");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Fail_When_SampleApiThrowsException()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Backend", "https://backend/").Request(HttpMethod.Head, string.Empty).Respond.With(string.Empty, response: x => throw new Exception("Sample API is down"));
            using var test = FunctionTester.Create<Startup>()
                .UseJsonSerializer(new CoreEx.Text.Json.JsonSerializer()) // Required as the Result type needs to be deserialized using CoreEx.
                .ReplaceHttpClientFactory(mcf);
            var mock = new Mock<IHealthCheck>();

            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("test", mock.Object, null, null)
            };

            // Act
            var result = await test.Type<TypedHttpClientCoreHealthCheck<BackendHttpClient>>()
             .RunAsync(x => x.CheckHealthAsync(context, CancellationToken.None));

            // Assert
            result.Result.Status.Should().Be(HealthStatus.Unhealthy, because: "Sample API is Down");
            result.Result.Exception.Should().NotBeNull();
        }

        [Test]
        public async Task CheckHealthAsync_Should_Fail_When_NoHttpClientInjected()
        {
            // Arrange
            var target = new TypedHttpClientCoreHealthCheck<BackendHttpClient>(null!);
            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("test", new Mock<IHealthCheck>().Object, null, null)
            };

            // Act
            var result = await target.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy, because: "No HttpClient injected.");
            result.Description.Should().Be("Typed Http client dependency for 'CoreEx.TestFunction.BackendHttpClient' not resolved.");
        }
    }
}