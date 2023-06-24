using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using CoreEx.Azure.HealthChecks;
using CoreEx.Configuration;
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
    public class AzureServiceBusTopicHealthCheckTest
    {
        [Test]
        public async Task CheckHealthAsync_Should_CheckIfTopicExists()
        {
            // Arrange
            string connectionName = "ServiceBusConnection", topicSettingName = "TestTopicName", testSubscriptionName = "TestSubscriptionName";
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetRequiredService(typeof(SettingsBase));
            var mock = new Mock<ServiceBusAdministrationClient>();
            //   replace connection to service bus with mock
            AzureServiceHealthCheckBase.ManagementClientConnections.AddOrUpdate(settings.GetValue<string>(connectionName)
                , _ => mock.Object
                , (_, __) => mock.Object);

            var context = new HealthCheckContext();

            var check = new AzureServiceBusTopicHealthCheck(settings, connectionName, topicSettingName, testSubscriptionName);

            // Act
            var result = await check.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy, because: "Service bus did not throw exception");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_When_TopicSettingNotFound()
        {
            // Arrange
            string connectionName = "ServiceBusConnection", topicSettingName = "NonExistingTestTopicName", testSubscriptionName = "TestSubscriptionName";
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetRequiredService(typeof(SettingsBase));
            var mock = new Mock<ServiceBusAdministrationClient>();
            var context = new HealthCheckContext();

            var check = new AzureServiceBusTopicHealthCheck(settings, connectionName, topicSettingName, testSubscriptionName);

            // Act
            var result = await check.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy, because: "Queue setting doesn't exist in configuration");
            result.Description.Should().Be($"Topic name is not configured under '{topicSettingName}' in settings");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_When_SubscriptionSettingNotFound()
        {
            // Arrange
            string connectionName = "ServiceBusConnection", topicSettingName = "TestTopicName", testSubscriptionName = "NonExistingTestSubscriptionName";
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetRequiredService(typeof(SettingsBase));
            var mock = new Mock<ServiceBusAdministrationClient>();
            var context = new HealthCheckContext();

            var check = new AzureServiceBusTopicHealthCheck(settings, connectionName, topicSettingName, testSubscriptionName);

            // Act
            var result = await check.CheckHealthAsync(context, CancellationToken.None);

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy, because: "Queue setting doesn't exist in configuration");
            result.Description.Should().Be($"Subscription name is not configured under '{testSubscriptionName}' in settings");
        }

        [Test]
        public async Task CheckHealthAsync_Should_Return_Unhealthy_When_ServiceBusThrowsException()
        {
            // Arrange
            string connectionName = "ServiceBusConnection", topicSettingName = "TestTopicName", testSubscriptionName = "TestSubscriptionName";
            using var test = FunctionTester.Create<Startup>();
            var settings = (SettingsBase)test.Services.GetRequiredService(typeof(SettingsBase));
            var mock = new Mock<ServiceBusAdministrationClient>();
            var exception = new ArgumentException("Test exception");
            mock.Setup(x => x.GetSubscriptionRuntimePropertiesAsync(
                    settings.GetValue<string>(topicSettingName, null!),
                    settings.GetValue<string>(testSubscriptionName, null!),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            AzureServiceHealthCheckBase.ManagementClientConnections.AddOrUpdate(settings.GetValue<string>(connectionName)
            , _ => mock.Object
            , (_, __) => mock.Object);

            var check = new AzureServiceBusTopicHealthCheck(settings, connectionName, topicSettingName, testSubscriptionName);
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