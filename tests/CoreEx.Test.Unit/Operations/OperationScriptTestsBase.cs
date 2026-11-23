namespace CoreEx.Test.Unit.Operations;

public abstract class OperationScriptTestsBase
{
    private DirectoryInfo? _outputDirectory;

    protected abstract Type ConsoleType { get; }
    protected abstract string ConnectionString { get; }
    protected abstract string Extension { get; }
    protected abstract string DefaultTable { get; }
    protected abstract string ProviderDescription { get; }
    protected abstract string QualifiedTable(string schema, string table);

    private string OutputDirectory => _outputDirectory?.FullName ?? throw new InvalidOperationException("The test output directory has not been created.");

    [SetUp]
    public void SetUp() => _outputDirectory = Directory.CreateTempSubdirectory("coreex-operation-script-");

    [TearDown]
    public void TearDown() => _outputDirectory?.Delete(recursive: true);

    private Task<(int ExitCode, string Output)> RunAsync(params string[] arguments)
        => OperationScriptTestHelper.RunAsync(ConsoleType, OutputDirectory, ConnectionString, arguments);

    private async Task AssertExportAsync(string schema, string table)
    {
        var files = Directory.GetFiles(Path.Combine(OutputDirectory, "Migrations"), $"*.{Extension}");
        files.Should().ContainSingle();
        var file = files[0];
        Path.GetFileName(file).Should().MatchRegex(@"^\d{8}-\d{6}-create-")
            .And.EndWith($"-create-{schema.ToLowerInvariant()}-{table.ToLowerInvariant()}-table.{Extension}");

        var sql = await File.ReadAllTextAsync(file);
        sql.Should().Contain($"CREATE TABLE {QualifiedTable(schema, table)} (");
        sql.Should().NotContain("{{");
    }

    [Test]
    public async Task ScriptHelp_Discovers_Operations_Without_A_Database()
    {
        var (exitCode, output) = await RunAsync("--help");

        exitCode.Should().Be(0, "DbEx help should succeed: {0}", output);
        output.Should().Contain("script operations <Schema> <Table>");
        output.Should().Contain(ProviderDescription);
        output.Should().Contain("script outbox");
        Directory.GetFiles(OutputDirectory, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Test]
    public async Task Script_Uses_Configured_Schema_And_Default_Table()
    {
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "dbex.yaml"), "schema: ops_config\n");
        var (exitCode, output) = await RunAsync("script", "operations");

        exitCode.Should().Be(0, "DbEx export should succeed: {0}", output);
        await AssertExportAsync("ops_config", DefaultTable);
        (await File.ReadAllTextAsync(Path.Combine(OutputDirectory, "dbex.yaml"))).Should().Be("schema: ops_config\n");
    }

    [Test]
    public async Task Script_Positional_Arguments_Override_Configured_Schema()
    {
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "dbex.yaml"), "schema: ignored\n");
        var (exitCode, output) = await RunAsync("script", "operations", "Sales", "Orders");

        exitCode.Should().Be(0, "DbEx export should succeed: {0}", output);
        await AssertExportAsync("Sales", "Orders");
    }

    [Test]
    public async Task Script_Named_Parameters_Override_Configured_Schema()
    {
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "dbex.yaml"), "schema: ignored\n");
        var (exitCode, output) = await RunAsync("script", "operations", "-p", "Param1=Sales", "-p", "Param2=Orders");

        exitCode.Should().Be(0, "DbEx export should succeed: {0}", output);
        await AssertExportAsync("Sales", "Orders");
    }

    [Test]
    public async Task Script_Named_Parameters_Work_Without_Configuration()
    {
        var (exitCode, output) = await RunAsync("script", "operations", "-p", "Param1=Sales", "-p", "Param2=Orders");

        exitCode.Should().Be(0, "DbEx export should succeed: {0}", output);
        await AssertExportAsync("Sales", "Orders");
    }

    [Test]
    public async Task Script_Uses_Literal_Schema_Fallback_Without_Configuration()
    {
        var (exitCode, output) = await RunAsync("script", "operations");

        exitCode.Should().Be(0, "DbEx export should succeed: {0}", output);
        await AssertExportAsync("Schema", DefaultTable);
    }

    [Test]
    public async Task Script_Rejects_Malformed_Configuration_Without_Exporting()
    {
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "dbex.yaml"), "schema: [unterminated\n");
        var (exitCode, output) = await RunAsync("script", "operations");

        exitCode.Should().NotBe(0);
        output.Should().Contain("dbex.yaml").And.Contain("invalid");
        var migrations = Path.Combine(OutputDirectory, "Migrations");
        if (Directory.Exists(migrations))
            Directory.GetFiles(migrations).Should().BeEmpty();
    }

    [Test]
    public async Task Script_Does_Not_Overwrite_Existing_Migrations()
    {
        var migrations = Directory.CreateDirectory(Path.Combine(OutputDirectory, "Migrations"));
        var now = TimeProvider.System.GetUtcNow();
        for (var offset = -5; offset <= 60; offset++)
        {
            var timestamp = now.AddSeconds(offset).ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            await File.WriteAllTextAsync(Path.Combine(migrations.FullName, $"{timestamp}-create-clash-orders-table.{Extension}"), "original snapshot");
        }

        var (exitCode, output) = await RunAsync("script", "operations", "Clash", "Orders");

        exitCode.Should().NotBe(0);
        output.Should().Contain("already exists").And.Contain("not changed");
        var files = migrations.GetFiles();
        files.Should().HaveCount(66);
        foreach (var file in files)
            (await File.ReadAllTextAsync(file.FullName)).Should().Be("original snapshot");
    }
}
