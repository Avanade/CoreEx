using System;
using System.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using CoreEx.HealthChecks;
using CoreEx.Json;
using CoreEx.TestFunction;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using UnitTestEx.NUnit;

namespace CoreEx.Test.HealthChecks
{
    [TestFixture, NonParallelizable]
    public class HealthServiceTests
    {
        [Test]
        public async Task RunAsync_Should_Return_HealthCheckJSON()
        {
            // Arrange
            using var test = FunctionTester.Create<Startup>();

            // Act
            var result = await test.Type<HealthService>().RunAsync(x => x.RunAsync());
            var json = ((Microsoft.AspNetCore.Mvc.ContentResult)result.Result).Content;
            var jsonSerializer = (IJsonSerializer)test.Services.GetService(typeof(IJsonSerializer));

            JsonElement jsonObj = (JsonElement)jsonSerializer.Deserialize(json)!;

            // Assert
            result.AssertSuccess();
            jsonObj.Should().NotBeNull();
            jsonObj.GetProperty("healthReport").GetProperty("status").GetString().Should().Be("Unhealthy", because: "Registered http backend is not healthy");
            jsonObj.GetProperty("deployment").GetProperty("by").GetString().Should().Be("me");
            jsonObj.GetProperty("deployment").GetProperty("build").GetString().Should().Be("build no");
            jsonObj.GetProperty("deployment").GetProperty("name").GetString().Should().Be("my deployment");
            jsonObj.GetProperty("deployment").GetProperty("version").GetString().Should().Be("1.0.0");
            jsonObj.GetProperty("deployment").GetProperty("dateUtc").GetString().Should().Be("today");
        }

        [Test]
        public async Task RunAsync_Should_Return_HealthCheckJSON_When_ChecksFail()
        {
            // Arrange
            using var test = FunctionTester.Create<Startup>();
            //   setup failing health check
            Mock<IHealthCheck> mock = new Mock<IHealthCheck>();
            mock.Setup(h => h.CheckHealthAsync(It.IsAny<HealthCheckContext>(), default))
                .Returns(Task.FromResult(HealthCheckResult.Unhealthy("Failed during health checks")));
            test.ConfigureServices(sc => sc.AddHealthChecks().AddCheck("Failing", mock.Object));

            var settings = (SettingsBase)test.Services.GetService(typeof(SettingsBase));
            var jsonSerializer = (IJsonSerializer)test.Services.GetService(typeof(IJsonSerializer));

            // Act
            var result = await test.Type<HealthService>().RunAsync(x => x.RunAsync());
            var json = ((Microsoft.AspNetCore.Mvc.ContentResult)result.Result).Content;
            JsonElement jsonObj = (JsonElement)jsonSerializer.Deserialize(json)!;

            // Assert
            jsonObj.Should().NotBeNull();
            json.Should().Contain("Failed during health checks");
            jsonObj.GetProperty("healthReport").GetProperty("status").GetString().Should().Be("Unhealthy");
            jsonObj.GetProperty("deployment").GetProperty("by").GetString().Should().Be("me");
            jsonObj.GetProperty("deployment").GetProperty("build").GetString().Should().Be("build no");
            jsonObj.GetProperty("deployment").GetProperty("name").GetString().Should().Be("my deployment");
            jsonObj.GetProperty("deployment").GetProperty("version").GetString().Should().Be("1.0.0");
            jsonObj.GetProperty("deployment").GetProperty("dateUtc").GetString().Should().Be("today");
        }

        [Test]
        public async Task RunAsync_Should_Return_HealthCheckJSON_When_ChecksThrowException()
        {
            // Arrange
            using var test = FunctionTester.Create<Startup>();
            //   setup failing health check
            Mock<IHealthCheck> mock = new Mock<IHealthCheck>();
            mock.Setup(h => h.CheckHealthAsync(It.IsAny<HealthCheckContext>(), default))
                .Throws(new Exception("Failed during health checks"));
            test.ConfigureServices(sc => sc.AddHealthChecks().AddCheck("Failing-with-exception", mock.Object));

            var settings = (SettingsBase)test.Services.GetService(typeof(SettingsBase));
            var jsonSerializer = (IJsonSerializer)test.Services.GetService(typeof(IJsonSerializer));

            // Act
            var result = await test.Type<HealthService>().RunAsync(x => x.RunAsync());
            var json = ((Microsoft.AspNetCore.Mvc.ContentResult)result.Result).Content;
            JsonElement jsonObj = (JsonElement)jsonSerializer.Deserialize(json)!;

            // Assert
            jsonObj.Should().NotBeNull();
            json.Should().Contain("Failed during health checks");
            jsonObj.GetProperty("healthReport").GetProperty("status").GetString().Should().Be("Unhealthy");
            jsonObj.GetProperty("deployment").GetProperty("by").GetString().Should().Be("me");
            jsonObj.GetProperty("deployment").GetProperty("build").GetString().Should().Be("build no");
            jsonObj.GetProperty("deployment").GetProperty("name").GetString().Should().Be("my deployment");
            jsonObj.GetProperty("deployment").GetProperty("version").GetString().Should().Be("1.0.0");
            jsonObj.GetProperty("deployment").GetProperty("dateUtc").GetString().Should().Be("today");
        }
    }
}