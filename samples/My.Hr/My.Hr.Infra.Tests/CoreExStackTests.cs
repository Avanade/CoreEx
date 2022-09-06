// https://www.pulumi.com/blog/unit-testing-cloud-deployments-with-dotnet/

using Pulumi.AzureNative.Resources;

namespace UnitTesting;

public class CoreExStackTests
{
    [Test]
    public async Task ResourceGroupHasNameTag()
    {
        var (resources, outputs, dbOperationsMock) = await Testing.RunAsync();

        var rgs = resources.OfType<ResourceGroup>();
        var rg = rgs.First();
        var tags = await rg.Tags.GetValueAsync();

        // Assert
        rgs.Should().HaveCount(1);
        rg.Should().NotBeNull();
        tags.Should().ContainKey("App");
    }

    [Test]
    public async Task FunctionIsCreatedWithAUrl()
    {
        var (resources, outputs, dbOperationsMock) = await Testing.RunAsync();

        var healthUrl = await outputs["FunctionHealthUrl"]!.GetValueAsync<string>();
        var appSwaggerUrl = await outputs["AppSwaggerUrl"]!.GetValueAsync<string>();

        // Assert
        healthUrl.Should().Be("https://unittest.azurewebsites.net/api/health?code=key", because: "mock values set in Testing class");
        appSwaggerUrl.Should().Be("https://unittest.azurewebsites.net/swagger/index.html", because: "mock values set in Testing class");
    }

    [Test]
    public async Task SqlIsCreatedWithConnectionString()
    {
        var (resources, outputs, dbOperationsMock) = await Testing.RunAsync();

        var connectionString = await outputs["SqlDatabaseConnectionString"]!.GetValueAsync<string>();

        // Assert
        connectionString.Should().Be("Server=sql-server-stack.database.windows.net; Authentication=Active Directory Default; Database=sqldb", because: "mock values set in Testing class");
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

