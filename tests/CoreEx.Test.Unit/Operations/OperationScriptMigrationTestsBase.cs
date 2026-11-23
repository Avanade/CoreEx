namespace CoreEx.Test.Unit.Operations;

public abstract class OperationScriptMigrationTestsBase
{
    protected abstract Type ConsoleType { get; }
    protected abstract string Schema { get; }
    protected abstract string Table { get; }
    protected abstract string GetConnectionString(string databaseName);
    protected abstract Task VerifyTableExistsAsync(string connectionString);

    [Test]
    public async Task Exported_Migration_Is_Embedded_Applied_And_Journaled()
    {
        var directory = Directory.CreateTempSubdirectory("coreex-operation-migration-");
        var connectionString = GetConnectionString($"coreex_ops_{Guid.NewGuid():N}");
        var created = false;
        try
        {
            var (exportExitCode, exportOutput) = await OperationScriptTestHelper.RunAsync(ConsoleType, directory.FullName, connectionString, "script", "operations", Schema, Table);
            exportExitCode.Should().Be(0, "DbEx export should succeed before the database exists: {0}", exportOutput);

            var script = Directory.GetFiles(Path.Combine(directory.FullName, "Migrations")).Single();
            var original = await File.ReadAllBytesAsync(script);
            var assembly = await OperationScriptTestHelper.BuildMigrationAssemblyAsync(directory.FullName);

            var (createExitCode, createOutput) = await OperationScriptTestHelper.RunAsync(ConsoleType, directory.FullName, connectionString, "create");
            createExitCode.Should().Be(0, "the isolated test database should be created: {0}", createOutput);
            created = true;

            var (migrateExitCode, migrateOutput) = await OperationScriptTestHelper.RunAsync(ConsoleType, directory.FullName, connectionString, "migrate", "-a", assembly);
            migrateExitCode.Should().Be(0, "the exported migration should apply: {0}", migrateOutput);
            migrateOutput.Should().Contain($"OperationMigrationProof.Migrations.{Path.GetFileName(script)}");
            await VerifyTableExistsAsync(connectionString);

            var (repeatExitCode, repeatOutput) = await OperationScriptTestHelper.RunAsync(ConsoleType, directory.FullName, connectionString, "migrate", "-a", assembly);
            repeatExitCode.Should().Be(0, "the journal should prevent reapplying the migration: {0}", repeatOutput);
            repeatOutput.Should().Contain("No new scripts found to execute");
            (await File.ReadAllBytesAsync(script)).Should().Equal(original);
        }
        finally
        {
            try
            {
                if (created)
                {
                    var (exitCode, output) = await OperationScriptTestHelper.RunAsync(ConsoleType, directory.FullName, connectionString, "drop", "--accept-prompts");
                    if (exitCode != 0)
                        throw new InvalidOperationException($"The isolated test database could not be dropped: {output}");
                }
            }
            finally
            {
                directory.Delete(recursive: true);
            }
        }
    }
}
