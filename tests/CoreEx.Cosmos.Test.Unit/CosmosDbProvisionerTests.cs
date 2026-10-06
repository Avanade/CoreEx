namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Proves the <see cref="CosmosDbProvisioner"/> (and its console/client factory) end-to-end against the local emulator; each test uses its own client and uniquely named database (dropped afterwards) so replacing
/// and dropping resources cannot leave stale metadata caches behind for other tests.
/// </summary>
[TestFixture]
public class CosmosDbProvisionerTests : CosmosTestBase
{
    private CosmosClient _client = null!;
    private string _databaseId = null!;
    private const string ItemsId = "prov-items";
    private const string RefId = "prov-ref";

    private new CosmosClient Client => _client;

    private string DatabaseId => _databaseId;

    private CosmosDbProvisionArgs CreateArgs(StringWriter? output = null) => new CosmosDbProvisionArgs { DatabaseId = DatabaseId, Output = output ?? new StringWriter() }
        .Container(ItemsId)
        .ReferenceDataContainer(RefId);

    private CosmosDbProvisioner CreateProvisioner(CosmosDbProvisionArgs args) => new(Client, args);

    private static PartitionKey PartitionOf(string containerId) => containerId == ItemsId ? new PartitionKey("pk") : PartitionKey.None;

    private async Task<int> CountAsync(string containerId)
    {
        using var iterator = Client.GetContainer(DatabaseId, containerId).GetItemQueryIterator<int>("SELECT VALUE COUNT(1) FROM c", requestOptions: new QueryRequestOptions { PartitionKey = PartitionOf(containerId) });
        var total = 0;
        while (iterator.HasMoreResults)
        {
            foreach (var count in await iterator.ReadNextAsync().ConfigureAwait(false))
            {
                total += count;
            }
        }

        return total;
    }

    [SetUp]
    public void SetUpClient()
    {
        _client = CreateClient();
        _databaseId = $"CoreEx.Cosmos.Test.Unit.Provisioning.{NewId()}";
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        if (_client is null)
            return;

        try { await _client.GetDatabase(_databaseId).DeleteAsync().ConfigureAwait(false); }
        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound) { }
        finally { _client.Dispose(); _client = null!; }
    }

    [Test]
    public async Task Create_IsIdempotent_AndPreservesExistingData()
    {
        var p = CreateProvisioner(CreateArgs());
        await p.RunAsync(CosmosDbProvisionCommand.Reset);
        await Client.GetContainer(DatabaseId, ItemsId).CreateItemAsync(new { id = "keep", partitionKey = "pk" }, new PartitionKey("pk"));

        await p.RunAsync(CosmosDbProvisionCommand.Create);

        (await CountAsync(ItemsId)).Should().Be(1);
    }

    [Test]
    public async Task Reset_EmptiesContainers()
    {
        var p = CreateProvisioner(CreateArgs());
        await p.RunAsync(CosmosDbProvisionCommand.Reset);
        await Client.GetContainer(DatabaseId, ItemsId).CreateItemAsync(new { id = "gone", partitionKey = "pk" }, new PartitionKey("pk"));
        (await CountAsync(ItemsId)).Should().Be(1);

        await p.RunAsync(CosmosDbProvisionCommand.Reset);

        (await CountAsync(ItemsId)).Should().Be(0);
    }

    [Test]
    public async Task ResetAndData_ImportsPlainAndDiscriminatedData()
    {
        var args = CreateArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml");
        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.ResetAndData);

        (await CountAsync(ItemsId)).Should().Be(2);
        (await CountAsync(RefId)).Should().Be(3);

        using var iterator = Client.GetContainer(DatabaseId, RefId).GetItemQueryIterator<string>("SELECT VALUE c.typeDiscriminator FROM c ORDER BY c.typeDiscriminator", requestOptions: new QueryRequestOptions { PartitionKey = PartitionKey.None });
        var types = new List<string>();
        while (iterator.HasMoreResults)
        {
            types.AddRange(await iterator.ReadNextAsync());
        }

        types.Should().Equal("Colour", "Colour", "Shape");
    }

    [Test]
    public async Task ReferenceDataContainer_RejectsDuplicateTypeDiscriminatorAndCode()
    {
        await CreateProvisioner(CreateArgs()).RunAsync(CosmosDbProvisionCommand.Reset);
        var container = Client.GetContainer(DatabaseId, RefId);

        await container.CreateItemAsync(new { id = "a", partitionKey = "pk", typeDiscriminator = "Colour", code = "R" }, new PartitionKey("pk"));
        var ex = await FluentActions.Awaiting(() => container.CreateItemAsync(new { id = "b", partitionKey = "pk", typeDiscriminator = "Colour", code = "R" }, new PartitionKey("pk"))).Should().ThrowAsync<CosmosException>();

        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Data_UnknownContainerKey_Throws()
    {
        var args = CreateArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning-unknown.seed.yaml");
        var p = CreateProvisioner(args);
        await p.RunAsync(CosmosDbProvisionCommand.Reset);

        await FluentActions.Awaiting(() => p.RunAsync(CosmosDbProvisionCommand.Data)).Should().ThrowAsync<InvalidOperationException>().WithMessage("*unknown-container*");
    }

    [Test]
    public async Task Drop_RemovesDatabase_AndIsSafeWhenMissing()
    {
        var p = CreateProvisioner(CreateArgs());
        await p.RunAsync(CosmosDbProvisionCommand.Reset);

        await p.RunAsync(CosmosDbProvisionCommand.Drop);
        await p.RunAsync(CosmosDbProvisionCommand.Drop);

        await FluentActions.Awaiting(() => Client.GetDatabase(DatabaseId).ReadAsync()).Should().ThrowAsync<CosmosException>().Where(x => x.StatusCode == HttpStatusCode.NotFound);
    }

    [Test]
    public void DuplicateContainerDeclaration_Throws()
        => FluentActions.Invoking(() => new CosmosDbProvisionArgs().Container("a").Container("a")).Should().Throw<InvalidOperationException>();

    [Test]
    public void ReferenceDataContainer_DefinesUniqueKey_AndPlainContainerDoesNot()
    {
        var args = CreateArgs();
        args.Containers.Single(x => x.Id == RefId).CreateProperties().UniqueKeyPolicy.UniqueKeys.Single().Paths.Should().Equal("/typeDiscriminator", "/code");
        args.Containers.Single(x => x.Id == ItemsId).CreateProperties().UniqueKeyPolicy.UniqueKeys.Should().BeEmpty();
    }

    [Test]
    public void ClientFactory_LocalEndpoint_UsesGateway_AndRemoteDoesNot()
    {
        const string key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        using var local = CosmosDbClientFactory.Create($"AccountEndpoint=https://localhost:8081/;AccountKey={key}");
        local.ClientOptions.ConnectionMode.Should().Be(ConnectionMode.Gateway);

        using var remote = CosmosDbClientFactory.Create($"AccountEndpoint=https://example.documents.azure.com:443/;AccountKey={key}");
        remote.ClientOptions.ConnectionMode.Should().Be(ConnectionMode.Direct);
    }

    [Test]
    public async Task Console_ParsesArguments()
    {
        var console = CosmosDbConsole.Create<CosmosDbProvisionerTests>("AccountEndpoint=https://localhost:8081/;AccountKey=abc", DatabaseId);

        (await console.RunAsync(["--help"])).Should().Be(0);
        (await console.RunAsync([])).Should().Be(1);
        (await console.RunAsync(["NotACommand"])).Should().Be(1);
        (await console.RunAsync(["Create", "-d"])).Should().Be(1);
    }

    private async Task<List<T>> QueryAsync<T>(string containerId, string sql)
    {
        using var iterator = Client.GetContainer(DatabaseId, containerId).GetItemQueryIterator<T>(sql, requestOptions: new QueryRequestOptions { PartitionKey = PartitionOf(containerId) });
        var items = new List<T>();
        while (iterator.HasMoreResults)
        {
            items.AddRange(await iterator.ReadNextAsync());
        }

        return items;
    }

    [Test]
    public async Task Data_ContainerDataOptions_AreApplied()
    {
        var args = new CosmosDbProvisionArgs { DatabaseId = DatabaseId, Output = new StringWriter() }
            .Container(ItemsId, dataOptions: ctx => { var o = new JsonDataReaderOptions(ctx.NamingConvention); o.Properties.Add("tag", "container"); return o; })
            .ReferenceDataContainer(RefId)
            .AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml");

        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.ResetAndData);

        (await QueryAsync<string>(ItemsId, "SELECT VALUE c.tag FROM c")).Should().Equal("container", "container");
        (await QueryAsync<string>(RefId, "SELECT VALUE c.tag FROM c")).Should().BeEmpty();
    }

    [Test]
    public async Task Data_ResourceDataOptions_TakePrecedence_AndNullDefersToContainer()
    {
        var args = new CosmosDbProvisionArgs { DatabaseId = DatabaseId, Output = new StringWriter() }
            .Container(ItemsId, dataOptions: ctx => { var o = new JsonDataReaderOptions(ctx.NamingConvention); o.Properties.Add("tag", "container"); return o; })
            .ReferenceDataContainer(RefId, dataOptions: ctx => { var o = JsonDataReaderOptions.CreateForReferenceData(ctx.NamingConvention); o.Properties.Add("tag", "refcontainer"); return o; })
            .AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml", ctx =>
            {
                if (ctx.Container.Id != ItemsId)
                    return null;

                var o = new JsonDataReaderOptions(ctx.NamingConvention);
                o.Properties.Add("tag", "resource");
                return o;
            });

        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.ResetAndData);

        (await QueryAsync<string>(ItemsId, "SELECT VALUE c.tag FROM c")).Should().Equal("resource", "resource");
        (await QueryAsync<string>(RefId, "SELECT VALUE c.tag FROM c")).Should().Equal("refcontainer", "refcontainer", "refcontainer");
    }

    [Test]
    public async Task Data_ReferenceDataContainer_CustomIdGenerator_ViaDataOptions()
    {
        var n = 0;
        var args = new CosmosDbProvisionArgs { DatabaseId = DatabaseId, Output = new StringWriter() }
            .Container(ItemsId)
            .ReferenceDataContainer(RefId, dataOptions: ctx => JsonDataReaderOptions.CreateForReferenceData(ctx.NamingConvention, () => $"gen-{Interlocked.Increment(ref n)}"))
            .AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml");

        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.ResetAndData);

        (await QueryAsync<string>(RefId, "SELECT VALUE c.id FROM c ORDER BY c.id")).Should().Equal("gen-1", "gen-2", "gen-3");
    }

    [Test]
    public void AddDataResource_Duplicate_Throws()
        => FluentActions.Invoking(() => new CosmosDbProvisionArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml").AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml", _ => null))
            .Should().Throw<InvalidOperationException>();
}
