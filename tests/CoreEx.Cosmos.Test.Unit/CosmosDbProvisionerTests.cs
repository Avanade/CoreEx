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
        await p.RunAsync(CosmosDbProvisionCommand.Create);
        await Client.GetContainer(DatabaseId, ItemsId).CreateItemAsync(new { id = "keep", partitionKey = "pk" }, new PartitionKey("pk"));

        await p.RunAsync(CosmosDbProvisionCommand.Create);

        (await CountAsync(ItemsId)).Should().Be(1);
    }

    [Test]
    public async Task OutboxLeaseContainer_Create_IsProvisionedAndLogged()
    {
        var output = new StringWriter();
        var args = CreateArgs(output).OutboxLeaseContainer();
        args.Containers.Single(x => x.IsOutboxLease).Id.Should().Be(CosmosDbOutboxRelayOptions.DefaultLeaseContainerId).And.Be("$outbox-leases");

        var p = CreateProvisioner(args);
        await p.RunAsync(CosmosDbProvisionCommand.Create);

        var lease = await Client.GetContainer(DatabaseId, "$outbox-leases").ReadContainerAsync();
        lease.Resource.PartitionKeyPath.Should().Be("/id");
        output.ToString().Should().Contain($"Outbox lease container '$outbox-leases' created.");

        output.GetStringBuilder().Clear();
        await p.RunAsync(CosmosDbProvisionCommand.Create);
        output.ToString().Should().Contain("already exists and therefore not created.");
    }

    [Test]
    public async Task OutboxLeaseContainer_Reset_IsReplacedAndLogged()
    {
        var output = new StringWriter();
        var p = CreateProvisioner(CreateArgs(output).OutboxLeaseContainer());
        await p.RunAsync(CosmosDbProvisionCommand.Create);
        var lease = Client.GetContainer(DatabaseId, "$outbox-leases");
        await lease.CreateItemAsync(new { id = "lease1" }, new PartitionKey("lease1"));

        await p.RunAsync(CosmosDbProvisionCommand.Reset);

        using var iterator = lease.GetItemQueryIterator<int>("SELECT VALUE COUNT(1) FROM c");
        (await iterator.ReadNextAsync()).Single().Should().Be(0);
        output.ToString().Should().Contain($"Outbox lease container '$outbox-leases' replaced (empty).");
    }

    [Test]
    public void OutboxLeaseContainer_CustomId_AndDuplicate()
    {
        new CosmosDbProvisionArgs().OutboxLeaseContainer("custom").Containers.Single().Id.Should().Be("custom");
        FluentActions.Invoking(() => new CosmosDbProvisionArgs().OutboxLeaseContainer().Container("$outbox-leases")).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task RunAndLogAsync_Success_ReturnsCapturedOutput_AndRestoresOutput()
    {
        var original = new StringWriter();
        var args = CreateArgs(original);

        var (success, output) = await CreateProvisioner(args).RunAndLogAsync(CosmosDbProvisionCommand.Create);

        success.Should().BeTrue();
        output.Should().Contain("DATABASE CREATE");
        args.Output.Should().BeSameAs(original);
        original.ToString().Should().Be(output);
        (await Client.GetContainer(DatabaseId, ItemsId).ReadContainerAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task RunAndLogAsync_Failure_ReturnsFalse_WithMessage_AndDoesNotThrow()
    {
        var args = CreateArgs();
        var original = args.Output;

        var (success, output) = await CreateProvisioner(args).RunAndLogAsync(CosmosDbProvisionCommand.ResetAndData);

        success.Should().BeFalse();
        output.Should().Contain("Failed:").And.Contain("does not exist");
        args.Output.Should().BeSameAs(original);
    }

    [Test]
    public async Task Reset_EmptiesContainers()
    {
        var p = CreateProvisioner(CreateArgs());
        await p.RunAsync(CosmosDbProvisionCommand.Create);
        await Client.GetContainer(DatabaseId, ItemsId).CreateItemAsync(new { id = "gone", partitionKey = "pk" }, new PartitionKey("pk"));
        (await CountAsync(ItemsId)).Should().Be(1);

        await p.RunAsync(CosmosDbProvisionCommand.Reset);

        (await CountAsync(ItemsId)).Should().Be(0);
    }

    [Test]
    public async Task Reset_DatabaseNotExisting_FailsAndDoesNotCreate()
    {
        await FluentActions.Awaiting(() => CreateProvisioner(CreateArgs()).RunAsync(CosmosDbProvisionCommand.ResetAndData)).Should().ThrowAsync<InvalidOperationException>().WithMessage("*does not exist*");

        await FluentActions.Awaiting(() => Client.GetDatabase(DatabaseId).ReadAsync()).Should().ThrowAsync<CosmosException>().Where(x => x.StatusCode == HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ResetAndData_ImportsPlainAndDiscriminatedData()
    {
        var args = CreateArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml");
        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData);

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
        await CreateProvisioner(CreateArgs()).RunAsync(CosmosDbProvisionCommand.Create);
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
        await p.RunAsync(CosmosDbProvisionCommand.Create);

        await FluentActions.Awaiting(() => p.RunAsync(CosmosDbProvisionCommand.Data)).Should().ThrowAsync<InvalidOperationException>().WithMessage("*unknown-container*");
    }

    [Test]
    public async Task Drop_RemovesDatabase_AndIsSafeWhenMissing()
    {
        var p = CreateProvisioner(CreateArgs());
        await p.RunAsync(CosmosDbProvisionCommand.Create);

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
        (await console.RunAsync(["Create", "-p", "NoEquals"])).Should().Be(1);
        (await console.RunAsync(["Create", "-cv", "COREEX_COSMOS_TEST_UNSET_VARNAME"])).Should().Be(1);
        (await console.RunAsync(["Create", "--not-an-option"])).Should().Be(1);
    }

    [Test]
    public async Task Console_DestructiveCommands_RequireConfirmation()
    {
        var originalIn = Console.In;
        var originalError = Console.Error;
        try
        {
            // Not confirmed: stops before any connection is made, so nothing is executed.
            var error = new StringWriter();
            Console.SetError(error);
            var console = CosmosDbConsole.Create<CosmosDbProvisionerTests>("AccountEndpoint=https://localhost:1/;AccountKey=abc", DatabaseId);

            Console.SetIn(new StringReader("n"));
            (await console.RunAsync(["Drop"])).Should().Be(1);
            error.ToString().Should().Contain("Database drop was not confirmed");

            error.GetStringBuilder().Clear();
            Console.SetIn(new StringReader(string.Empty));
            (await console.RunAsync(["ResetAndData"])).Should().Be(1);
            error.ToString().Should().Contain("Container reset was not confirmed");
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetError(originalError);
        }
    }

    [Test]
    public async Task Console_ConnectionVarName_AndParams_AreApplied()
    {
        var varName = $"COREEX_COSMOS_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(varName, "AccountEndpoint=https://localhost:1/;AccountKey=abc");
        try
        {
            var console = CosmosDbConsole.Create<CosmosDbProvisionerTests>("AccountEndpoint=https://localhost:8081/;AccountKey=abc", DatabaseId);
            await console.RunAsync(["Create", "-cv", varName, "-p", "A=1", "--param", "B=x=y", "--accept-prompts"]);

            console.ConnectionString.Should().Contain("localhost:1");
            console.Args.AcceptPrompts.Should().BeTrue();
            console.Args.Parameters.Should().BeEquivalentTo(new Dictionary<string, string?> { ["A"] = "1", ["B"] = "x=y" });
        }
        finally
        {
            Environment.SetEnvironmentVariable(varName, null);
        }
    }

    [Test]
    public async Task Data_Parameters_AreSubstituted()
    {
        var args = CreateArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning-param.seed.yaml");
        args.Parameters["Who"] = "Eric";
        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData);

        (await QueryAsync<string>(ItemsId, "SELECT VALUE c.name FROM c")).Should().Equal("Eric");
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

        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData);

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

        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData);

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

        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData);

        (await QueryAsync<string>(RefId, "SELECT VALUE c.id FROM c ORDER BY c.id")).Should().Equal("gen-1", "gen-2", "gen-3");
    }

    [Test]
    public async Task Data_DollarPrefix_Merges_AndIsRerunnableWithStableIds()
    {
        var args = CreateArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning-merge.seed.yaml");
        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData);

        var ids = await RefIdsAsync();
        ids.Should().HaveCount(2);

        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.All);

        (await CountAsync(ItemsId)).Should().Be(1);
        (await CountAsync(RefId)).Should().Be(2);
        (await RefIdsAsync()).Should().Equal(ids);
    }

    [Test]
    public async Task Data_NoDollarPrefix_Inserts_SoRerunConflicts()
    {
        var args = CreateArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning-insert.seed.yaml");
        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData);
        (await CountAsync(RefId)).Should().Be(1);

        var act = () => CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Data);
        (await act.Should().ThrowAsync<CosmosException>()).Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);

        var plain = CreateArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning-insert-plain.seed.yaml");
        await CreateProvisioner(plain).RunAsync(CosmosDbProvisionCommand.Data);
        var act2 = () => CreateProvisioner(plain).RunAsync(CosmosDbProvisionCommand.Data);
        (await act2.Should().ThrowAsync<CosmosException>()).Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    private async Task<List<string>> RefIdsAsync()
    {
        using var iterator = Client.GetContainer(DatabaseId, RefId).GetItemQueryIterator<string>("SELECT VALUE c.id FROM c ORDER BY c.code", requestOptions: new QueryRequestOptions { PartitionKey = PartitionKey.None });
        var ids = new List<string>();
        while (iterator.HasMoreResults)
        {
            ids.AddRange(await iterator.ReadNextAsync());
        }

        return ids;
    }

    [Test]
    public async Task Output_WritesSections_AndMergeCounts()
    {
        var first = new StringWriter();
        var args = CreateArgs(first).AddDataResource<CosmosDbProvisionerTests>("provisioning-merge.seed.yaml");
        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData);

        var text = first.ToString();
        text.Should().Contain("DATABASE CREATE:").And.Contain("CONTAINER RESET:").And.Contain("DATABASE DATA:");
        text.Should().Contain("** Parsing and executing: ").And.Contain("provisioning-merge.seed.yaml");
        text.Should().Contain("Result: 2 item(s) (2 created, 0 replaced).").And.Contain("Complete. [");
        text.Should().Contain(new string('-', 80));

        var second = new StringWriter();
        args.Output = second;
        await CreateProvisioner(args).RunAsync(CosmosDbProvisionCommand.All);

        text = second.ToString();
        text.Should().Contain("DATABASE CREATE:").And.Contain("already exists and therefore not created.");
        text.Should().Contain("Result: 2 item(s) (0 created, 2 replaced).");
    }

    [Test]
    public void AddDataResource_Duplicate_Throws()
        => FluentActions.Invoking(() => new CosmosDbProvisionArgs().AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml").AddDataResource<CosmosDbProvisionerTests>("provisioning.seed.yaml", _ => null))
            .Should().Throw<InvalidOperationException>();
}
