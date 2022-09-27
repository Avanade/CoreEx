// https://www.pulumi.com/blog/unit-testing-cloud-deployments-with-dotnet/

using Pulumi.AzureNative.Resources;

namespace My.Hr.Infra.Tests;

public class CoreExStackTests
{
    [Test]
    public async Task ResourceGroupHasNameTag()
    {
        var (resources, _, _) = await Testing.RunAsync();

        var rgs = resources.OfType<ResourceGroup>();
        var rg = rgs.First();
        var tags = await rg.Tags.GetValueAsync();

        // Assert
        rgs.Should().HaveCount(1);
        rg.Should().NotBeNull();
        tags.Should().ContainKey("App");
    }

    [Test]
    public async Task AllResourcesHaveNameTag()
    {
        // unfortunately this doesn't test tags created by auto-tagging dove via ResourceTransformation
        var (resources, outputs, dbOperationsMock) = await Testing.RunAsync();

        var rs = resources.Select(async r =>
        {
            var tagsProp = r.GetType().GetProperty("Tags");

            return tagsProp != null
            ? (resource: r, tags: await tagsProp!.GetValue(r)!.GetValueAsync<System.Collections.Immutable.ImmutableDictionary<string, string>?>())
            : (resource: r, tags: null);
        });

        var result = (await Task.WhenAll(rs)).Where(anyResource => anyResource.tags != null);

        // Assert
        result.Should().HaveCountGreaterThan(5);
        result.Should().AllSatisfy(r => r.tags!.ContainsKey("App"), because: "All resources should be tagged");
    }

    [Test]
    public async Task FunctionIsCreatedWithAUrl()
    {
        var (_, outputs, _) = await Testing.RunAsync();

        var healthUrl = await outputs["FunctionHealthUrl"]!.GetValueAsync<string>();
        var appSwaggerUrl = await outputs["AppSwaggerUrl"]!.GetValueAsync<string>();

        // Assert
        healthUrl.Should().Be("https://unittest.azurewebsites.net/api/health?code=mocked-key", because: "mock values set in Testing class");
        appSwaggerUrl.Should().Be("https://unittest.azurewebsites.net/swagger/index.html", because: "mock values set in Testing class");
    }

    [Test]
    public async Task SqlIsCreatedWithConnectionString()
    {
        var (_, outputs, _) = await Testing.RunAsync();

        var connectionString = await outputs["SqlDatabaseConnectionString"]!.GetValueAsync<string>();

        // Assert
        connectionString.Should().Be("Server=sql-server-unit-test-stack.database.windows.net; Authentication=Active Directory Default; Database=sqldb", because: "mock values set in Testing class");
    }

    [Test]
    public async Task DbOperationsShouldExecute()
    {
        var (resources, outputs, dbOperationsMock) = await Testing.RunAsync();

        // Assert
        //   because DB schema deployment is enabled in Testing class
        dbOperationsMock.Verify(op => op.DeployDbSchemaAsync(It.IsAny<string>()));
        dbOperationsMock.Verify(op => op.ProvisionUsers(It.IsAny<Input<string>>(), It.IsAny<string>()));
    }
}

