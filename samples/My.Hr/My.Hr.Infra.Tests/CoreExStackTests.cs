// https://www.pulumi.com/blog/unit-testing-cloud-deployments-with-dotnet/

using CoreEx.Infra;
using Pulumi.AzureNative.Resources;

namespace UnitTesting;

public class CoreExStackTests
{
    [Test]
    public async Task ResourceGroupHasNameTag()
    {
        var resources = await Testing.RunAsync<CoreExStack>();

        var rgs = resources.OfType<ResourceGroup>();
        var rg = rgs.FirstOrDefault();
        var tags = await rg.Tags.GetValueAsync();

        // Assert
        rgs.Should().HaveCount(1);
        rg.Should().NotBeNull();
        tags.Should().ContainKey("App");
    }

    [Test]
    public async Task FunctionIsCreatedWithAUrl()
    {
        var resources = await Testing.RunAsync<CoreExStack>();

        var stack = resources.OfType<CoreExStack>().First();
        var healthUrl = await stack.FunctionHealthUrl.GetValueAsync();
        var appSwaggerUrl = await stack.AppSwaggerUrl.GetValueAsync();

        // Assert
        healthUrl.Should().Be("https://unittest.azurewebsites.net/api/health?code=key", because: "mock values set in Testing class");
        appSwaggerUrl.Should().Be("https://unittest.azurewebsites.net/swagger/index.html", because: "mock values set in Testing class");
    }

    [Test]
    public async Task SqlIsCreatedWithConnectionString()
    {
        var resources = await Testing.RunAsync<CoreExStack>();

        var stack = resources.OfType<CoreExStack>().First();
        var connectionString = await stack.SqlDatabaseConnectionString.GetValueAsync();

        // Assert
        connectionString.Should().Be("Server=sql-server-stack.database.windows.net; Authentication=Active Directory Default; Database=sqldb", because: "mock values set in Testing class");
    }
}

