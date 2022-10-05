
using System.Net;
using Company.AppName.Infra.Services;
using UnitTestEx.NUnit;

namespace Company.AppName.Infra.Tests.Services;

public class AzureApiServiceTests
{
    [Test]
    public async Task GetHostKeys_Should_GetHostKeyFromAzure()
    {
        // Arrange
        const string resourceGroupName = "resource-group-name";
        const string functionAppName = "function-app";
        const string mockedKey = "mocked-key";
        var resourceGroup = Output.Create(resourceGroupName);
        var functionApp = Output.Create(functionAppName);
        var mcf = MockHttpClientFactory.Create();
        var mc = mcf.CreateClient("azure");

        mc.Request(HttpMethod.Post, $"https://management.azure.com/subscriptions/{Testing.SubscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{functionAppName}/host/default/listkeys?api-version=2022-03-01")
            .Respond.WithJson(new AzureApiService.HostKeys { FunctionKeys = new AzureApiService.FunctionKeysValue { Key = mockedKey } });

        var target = new AzureApiService(new AzureApiClient(mcf.GetHttpClient("azure")!));

        // Act
        var resultOutput = await Testing.RunAsync(() => target.GetHostKeys(resourceGroup, functionApp));
        var result = await resultOutput.GetValueAsync();

        // Assert
        result.Should().Be(mockedKey);
        mcf.VerifyAll();
    }

    [Test]
    [Category("long")]
    [Description("Test retries for the azure call and because of that takes long time (2 min)")]
    public async Task GetHostKeys_Should_GetHostKeyFromAzure_When_FirstCallFails()
    {
        // Arrange
        const string resourceGroupName = "resource-group-name";
        const string functionAppName = "function-app";
        const string mockedKey = "mocked-key";
        var resourceGroup = Output.Create(resourceGroupName);
        var functionApp = Output.Create(functionAppName);
        var mcf = MockHttpClientFactory.Create();
        var mc = mcf.CreateClient("azure");

        mc.Request(HttpMethod.Post, $"https://management.azure.com/subscriptions/{Testing.SubscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{functionAppName}/host/default/listkeys?api-version=2022-03-01")
            .Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.NotFound);
                s.Respond().With(HttpStatusCode.BadRequest);
                s.Respond().With(HttpStatusCode.BadRequest);
                s.Respond().WithJson(new AzureApiService.HostKeys { FunctionKeys = new AzureApiService.FunctionKeysValue { Key = mockedKey } });
            });

        var target = new AzureApiService(new AzureApiClient(mcf.GetHttpClient("azure")!));

        // Act
        var resultOutput = await Testing.RunAsync(() => target.GetHostKeys(resourceGroup, functionApp));
        var result = await resultOutput.GetValueAsync();

        // Assert
        result.Should().Be(mockedKey);
        mcf.VerifyAll();
    }

    [Test]
    public async Task GetRoleIdByName_Should_ReturnRoleId()
    {
        // Arrange
        const string mockedId = "mocked-id";
        const string roleName = "StorageBlobContributor";
        var mcf = MockHttpClientFactory.Create();
        var mc = mcf.CreateClient("azure");

        mc.Request(HttpMethod.Get, $"https://management.azure.com/subscriptions/{Testing.SubscriptionId}/providers/Microsoft.Authorization/roleDefinitions?api-version=2018-01-01-preview&$filter=roleName eq '{roleName}'")
            .Respond.WithJson(new AzureApiService.RoleDefinition { Value = new() { new AzureApiService.RoleDefinitionValue { Id = mockedId } } });

        var target = new AzureApiService(new AzureApiClient(mcf.GetHttpClient("azure")!));

        // Act
        var resultTask = await Testing.RunAsync(() => target.GetRoleIdByName(roleName));
        var result = await resultTask;

        // Assert
        result.Should().Be(mockedId);
        mcf.VerifyAll();
    }

    [Test]
    public async Task SyncTriggers_Should_SyncTriggersOnTheFunctionApp()
    {
        // Arrange
        const string resourceGroupName = "resource-group-name";
        const string functionAppName = "function-app";
        var resourceGroup = Output.Create(resourceGroupName);
        var functionApp = Output.Create(functionAppName);
        var mcf = MockHttpClientFactory.Create();
        var mc = mcf.CreateClient("azure");

        mc.Request(HttpMethod.Post, $"https://management.azure.com/subscriptions/{Testing.SubscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{functionAppName}/syncfunctiontriggers?api-version=2016-08-01")
            .Respond.With(statusCode: HttpStatusCode.NoContent);

        var target = new AzureApiService(new AzureApiClient(mcf.GetHttpClient("azure")!));

        // Act
        var resultOutput = await Testing.RunAsync(() => target.SyncFunctionAppTriggers(resourceGroup, functionApp));
        var result = await resultOutput.GetValueAsync();

        // Assert
        result.Should().BeTrue();
        mcf.VerifyAll();
    }
}