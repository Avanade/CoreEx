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
            jsonObj.GetProperty("healthReport").GetProperty("Status").GetString().Should().Be("Healthy");
            jsonObj.GetProperty("Deployment").GetProperty("By").GetString().Should().Be("me");
            jsonObj.GetProperty("Deployment").GetProperty("Build").GetString().Should().Be("build no");
            jsonObj.GetProperty("Deployment").GetProperty("Name").GetString().Should().Be("my deployment");
            jsonObj.GetProperty("Deployment").GetProperty("Version").GetString().Should().Be("1.0.0");
            jsonObj.GetProperty("Deployment").GetProperty("DateUtc").GetString().Should().Be("today");
        }

   //     [Test]
        public async Task RunAsync_Should_Return_HealthCheckJSON_When_ChecksFail()
        {
            // Arrange
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetService(typeof(SettingsBase));
            var jsonSerializer = (IJsonSerializer)test.Services.GetService(typeof(IJsonSerializer));

            // Act
            var result = await test.Type<HealthService>().RunAsync(x => x.RunAsync());
            var json = ((Microsoft.AspNetCore.Mvc.ContentResult)result.Result).Content;
            JsonElement jsonObj = (JsonElement)jsonSerializer.Deserialize(json);

            // Assert
            jsonObj.Should().NotBeNull();
            json.Should().Contain(nameof(NullReferenceException));
            jsonObj.GetProperty("healthReport").GetProperty("Status").GetString().Should().Be("Unhealthy");
            jsonObj.GetProperty("Deployment").GetProperty("By").GetString().Should().Be("me");
            jsonObj.GetProperty("Deployment").GetProperty("Build").GetString().Should().Be("build no");
            jsonObj.GetProperty("Deployment").GetProperty("Name").GetString().Should().Be("my deployment");
            jsonObj.GetProperty("Deployment").GetProperty("Version").GetString().Should().Be("1.0.0");
            jsonObj.GetProperty("Deployment").GetProperty("DateUtc").GetString().Should().Be("today");
        }
    }
}