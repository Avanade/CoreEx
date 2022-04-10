using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Configuration;
using CoreEx.Healthchecks.Checks;
using CoreEx.Http;
using CoreEx.Json;
using CoreEx.TestFunction;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using UnitTestEx.NUnit;

namespace CoreEx.Test.HealthChecks
{

    [TestFixture, NonParallelizable]
    public class TypedHttpClientHealthCheckTest
    {
        public class TestHttpClient : TypedHttpClientBase<TestHttpClient>
        {
            public TestHttpClient(HttpClient client, IJsonSerializer jsonSerializer, CoreEx.ExecutionContext executionContext, SettingsBase settings, ILogger<TypedHttpClientBase<TestHttpClient>> logger) : base(client, jsonSerializer, executionContext, settings, logger)
            {
            }

            public override Task<HttpResult> HealthCheckAsync()
            {
                return base.HeadAsync("/health", null, null, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            }
        }

        [Test]
        public async Task CheckHealthAsync_Should_Succeed_When_TestBackendOK()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Test", "https://testing/").Request(HttpMethod.Head, "health").Respond.With(HttpStatusCode.OK);

            using var test = FunctionTester.Create<Startup>()
                .ReplaceHttpClientFactory(mcf)
                .ConfigureServices(sc => sc.AddHttpClient<TestHttpClient>("Test", c => c.BaseAddress = new Uri("https://testing/")));

            var mock = new Mock<IHealthCheck>();
            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("test", mock.Object, null, null)
            };

            // Act
            var result = await test.Type<TypedHttpClientHealthCheck<TestHttpClient>>()
             .RunAsync(x => x.CheckHealthAsync(context, CancellationToken.None));

            // Assert
            result.Result.Status.Should().Be(HealthStatus.Healthy, because: "TestBackend is OK");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Fail_When_TestBackendDown()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Test", "https://testing/").Request(HttpMethod.Head, "health").Respond.With(HttpStatusCode.ServiceUnavailable);

            using var test = FunctionTester.Create<Startup>()
                .ReplaceHttpClientFactory(mcf)
                .ConfigureServices(sc => sc.AddHttpClient<TestHttpClient>("Test", c => c.BaseAddress = new Uri("https://testing/")));

            var mock = new Mock<IHealthCheck>();
            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("test", mock.Object, null, null)
            };

            // Act
            var result = await test.Type<TypedHttpClientHealthCheck<TestHttpClient>>()
             .RunAsync(x => x.CheckHealthAsync(context, CancellationToken.None));

            // Assert
            result.Result.Status.Should().Be(HealthStatus.Unhealthy, because: "Testing service returned 502");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Fail_When_TestBackendThrowsException()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Test", "https://testing/").Request(HttpMethod.Head, "health")
                .Respond.With(string.Empty, response: x => throw new Exception("Testing service is down"));

            using var test = FunctionTester.Create<Startup>()
                .ReplaceHttpClientFactory(mcf)
                .ConfigureServices(sc => sc.AddHttpClient<TestHttpClient>("Test", c => c.BaseAddress = new Uri("https://testing/")));

            var mock = new Mock<IHealthCheck>();
            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("test", mock.Object, null, null)
            };

            // Act
            var result = await test.Type<TypedHttpClientHealthCheck<TestHttpClient>>()
             .RunAsync(x => x.CheckHealthAsync(context, CancellationToken.None));

            // Assert
            result.Result.Status.Should().Be(HealthStatus.Unhealthy, because: "Testing service is Down");
            result.Result.Exception.Should().NotBeNull();
        }

        [Test]
        public async Task CheckHealthAsync_Should_Fail_When_NoHttpClientInjected()
        {
            // Arrange
            var target = new TypedHttpClientHealthCheck<TestHttpClient>(null);
            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("test", new Mock<IHealthCheck>().Object, null, null)
            };

            // Act
            var result = await target.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy, because: "No HttpClient injected");
            result.Description.Should().Be("Typed Http client dependency for 'CoreEx.Test.HealthChecks.TypedHttpClientHealthCheckTest+TestHttpClient' not resolved");
        }
    }
}