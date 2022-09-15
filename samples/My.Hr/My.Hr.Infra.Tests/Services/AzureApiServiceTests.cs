
using My.Hr.Infra.Services;
using My.Hr.Infra.Tests;
using UnitTestEx.NUnit;

namespace My.Hr.Infra.Tests.Services;

public class AzureApiServiceTests
{
    [Test]
    public async Task GetHostKeys_Should_CallAzure()
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
        var (resources, outputs) = await Testing.RunAsync(() =>
        {
            var hostKey = target.GetHostKeys(resourceGroup, functionApp);
            return Task.FromResult<IDictionary<string, object?>>(new Dictionary<string, object?>
            {
                ["Key"] = hostKey
            });
        });
        var result = await outputs["Key"]!.GetValueAsync<string>();

        // Assert
        result.Should().Be(mockedKey);
    }
}