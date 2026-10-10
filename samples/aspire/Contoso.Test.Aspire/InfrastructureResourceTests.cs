namespace Contoso.Test.Aspire;

public class InfrastructureResourceTests
{
    [TestCase("Postgres")]
    [TestCase("SqlServer")]
    [TestCase("redis")]
    [TestCase("ServiceBus")]
    [TestCase("Cosmos")]
    public async Task ExternalConnectionString_IsVisibleAndPreservesConfiguration(string name)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true });
        const string value = "configured-connection-string";
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [$"ConnectionStrings:{name}"] = value });

        var resource = builder.AddExternalConnectionString(name);

        resource.Resource.Should().BeOfType<ConnectionStringResource>();
        resource.Resource.Name.Should().Be(name);
        (await resource.Resource.ConnectionStringExpression.GetValueAsync(default).ConfigureAwait(false)).Should().Be(value);
        var parameter = builder.Resources.OfType<ParameterResource>().Single();
        parameter.Secret.Should().BeTrue();
        parameter.Name.Should().Be($"{name}-connection");
    }

    [Test]
    public void Cosmos_ExposesConfiguredEndpointWithoutCredentials()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Cosmos"] = "AccountEndpoint=https://cosmos.example.test:18081/;AccountKey=test-key;"
        });

        var resource = builder.AddExternalConnectionString("Cosmos", endpointKey: "AccountEndpoint");

        resource.Resource.Annotations.OfType<ResourceUrlAnnotation>().Single().Url.Should().Be("https://cosmos.example.test:18081");
    }

    [TestCase(null)]
    [TestCase("AccountKey=test-key;")]
    [TestCase("AccountEndpoint=not-a-url;AccountKey=test-key;")]
    [TestCase("AccountEndpoint=ftp://cosmos.example.test/;AccountKey=test-key;")]
    [TestCase("AccountEndpoint=https://user:password@cosmos.example.test/;AccountKey=test-key;")]
    public void Cosmos_RejectsMissingOrInvalidEndpoint(string? connectionString)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { DisableDashboard = true });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Cosmos"] = connectionString });

        var action = () => builder.AddExternalConnectionString("Cosmos", endpointKey: "AccountEndpoint");

        action.Should().Throw<InvalidOperationException>();
    }
}
