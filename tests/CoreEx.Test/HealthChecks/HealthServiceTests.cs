using FluentAssertions;
using System.Threading.Tasks;
using System;
using NUnit.Framework;
using UnitTestEx.NUnit;
using CoreEx.TestFunction;
using CoreEx.Healthchecks;
using CoreEx.Json;
using CoreEx.Configuration;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

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

            JsonElement jsonObj = (JsonElement)jsonSerializer.Deserialize(json);

            // Assert
            result.AssertSuccess();
            jsonObj.Should().NotBeNull();
            jsonObj.GetProperty("healthReport").GetProperty("Status").GetString().Should().Be("Unhealthy", because: "Registered http backend is not healthy");
            jsonObj.GetProperty("Deployment").GetProperty("By").GetString().Should().Be("me");
            jsonObj.GetProperty("Deployment").GetProperty("Build").GetString().Should().Be("build no");
            jsonObj.GetProperty("Deployment").GetProperty("Name").GetString().Should().Be("my deployment");
            jsonObj.GetProperty("Deployment").GetProperty("Version").GetString().Should().Be("1.0.0");
            jsonObj.GetProperty("Deployment").GetProperty("DateUtc").GetString().Should().Be("today");
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
            JsonElement jsonObj = (JsonElement)jsonSerializer.Deserialize(json);

            // Assert
            jsonObj.Should().NotBeNull();
            json.Should().Contain("Failed during health checks");
            jsonObj.GetProperty("healthReport").GetProperty("Status").GetString().Should().Be("Unhealthy");
            jsonObj.GetProperty("Deployment").GetProperty("By").GetString().Should().Be("me");
            jsonObj.GetProperty("Deployment").GetProperty("Build").GetString().Should().Be("build no");
            jsonObj.GetProperty("Deployment").GetProperty("Name").GetString().Should().Be("my deployment");
            jsonObj.GetProperty("Deployment").GetProperty("Version").GetString().Should().Be("1.0.0");
            jsonObj.GetProperty("Deployment").GetProperty("DateUtc").GetString().Should().Be("today");
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
            JsonElement jsonObj = (JsonElement)jsonSerializer.Deserialize(json);

            // Assert
            jsonObj.Should().NotBeNull();
            json.Should().Contain("Failed during health checks");
            jsonObj.GetProperty("healthReport").GetProperty("Status").GetString().Should().Be("Unhealthy");
            jsonObj.GetProperty("Deployment").GetProperty("By").GetString().Should().Be("me");
            jsonObj.GetProperty("Deployment").GetProperty("Build").GetString().Should().Be("build no");
            jsonObj.GetProperty("Deployment").GetProperty("Name").GetString().Should().Be("my deployment");
            jsonObj.GetProperty("Deployment").GetProperty("Version").GetString().Should().Be("1.0.0");
            jsonObj.GetProperty("Deployment").GetProperty("DateUtc").GetString().Should().Be("today");
        }
    }
}