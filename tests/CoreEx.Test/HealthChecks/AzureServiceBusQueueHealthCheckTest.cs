using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using CoreEx.Configuration;
using CoreEx.Messaging.Azure.Health;
using CoreEx.TestFunction;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using UnitTestEx.NUnit;

namespace CoreEx.Test.HealthChecks
{

    [TestFixture, NonParallelizable]
    public class AzureServiceBusQueueHealthCheckTest
    {
        [Test]
        public async Task CheckHealthAsync_Should_CheckIfQueueExists()
        {
            // Arrange
            string connectionName = "ServiceBusConnection", queueSettingName = "TestQueueName";
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetService(typeof(SettingsBase));
            var mock = new Mock<ServiceBusAdministrationClient>();
            //   replace connection to service bus with mock
            AzureServiceHealthCheckBase.ManagementClientConnections.AddOrUpdate(settings.GetValue<string>(connectionName)
            , _ => mock.Object
            , (_, __) => mock.Object);
            var context = new HealthCheckContext();

            var check = new AzureServiceBusQueueHealthCheck(settings, connectionName, queueSettingName);

            // Act
            var result = await check.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy, because: "Service bus did not throw exception");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_When_QueueSettingNotFound()
        {
            // Arrange
            string connectionName = "ServiceBusConnection", queueSettingName = "NonExistingQueueName";
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetService(typeof(SettingsBase));
            var mock = new Mock<ServiceBusAdministrationClient>();
            var context = new HealthCheckContext();

            var check = new AzureServiceBusQueueHealthCheck(settings, connectionName, queueSettingName);

            // Act
            var result = await check.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy, because: "Queue setting doesn't exist in configuration");
            result.Description.Should().Be($"Queue name is not configured under '{queueSettingName}' in settings");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_When_ConnectionSettingNotFound()
        {
            // Arrange
            string connectionName = "NonExistingConnectionName", queueSettingName = "TestQueueName";
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetService(typeof(SettingsBase));
            var mock = new Mock<ServiceBusAdministrationClient>();
            var context = new HealthCheckContext();

            var check = new AzureServiceBusQueueHealthCheck(settings, connectionName, queueSettingName);

            // Act
            var result = await check.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy, because: "Queue setting doesn't exist in configuration");
            result.Description.Should().Be($"Service bus connection is not configured under '{connectionName}' in settings");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_When_ServiceBusThrowsException()
        {
            // Arrange
            string connectionName = "ServiceBusConnection", queueSettingName = "TestQueueName";
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetService(typeof(SettingsBase));
            var mock = new Mock<ServiceBusAdministrationClient>();
            var exception = new ArgumentException("Test exception");
            mock.Setup(x => x.GetQueueRuntimePropertiesAsync(settings.GetValue<string>(queueSettingName, null!), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            AzureServiceHealthCheckBase.ManagementClientConnections.AddOrUpdate(settings.GetValue<string>(connectionName)
            , _ => mock.Object
            , (_, __) => mock.Object);

            var check = new AzureServiceBusQueueHealthCheck(settings, connectionName, queueSettingName);
            var context = new HealthCheckContext()
            {
                Registration = new HealthCheckRegistration("Unit Test", check, null, null)
            };

            // Act
            var result = await check.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy, because: "Service bus threw exception");
            result.Exception.Should().Be(exception);
        }
    }
}