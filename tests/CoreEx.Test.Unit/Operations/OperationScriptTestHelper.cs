namespace CoreEx.Test.Unit.Operations;

internal static class OperationScriptTestHelper
{
    public const string SqlServerOfflineConnection = "Data Source=127.0.0.1,1;Initial Catalog=Offline;User ID=offline;Password=offline;TrustServerCertificate=true;Connect Timeout=1";
    public const string PostgresOfflineConnection = "Host=127.0.0.1;Port=1;Database=offline;Username=offline;Password=offline;Timeout=1;Pooling=false";

    public static async Task<(int ExitCode, string Output)> RunAsync(Type consoleType, string outputDirectory, string connectionString, params string[] arguments)
    {
        var startInfo = CreateStartInfo(outputDirectory);
        // The test output resolves some console dependencies from its shared framework.
        var testAssembly = typeof(OperationScriptTestHelper).Assembly.Location;
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--runtimeconfig");
        startInfo.ArgumentList.Add(Path.ChangeExtension(testAssembly, ".runtimeconfig.json"));
        startInfo.ArgumentList.Add("--depsfile");
        startInfo.ArgumentList.Add(Path.ChangeExtension(testAssembly, ".deps.json"));
        startInfo.ArgumentList.Add(consoleType.Assembly.Location);
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add("-cs");
        startInfo.ArgumentList.Add(connectionString);

        return await ExecuteAsync(startInfo).ConfigureAwait(false);
    }

    public static async Task<string> BuildMigrationAssemblyAsync(string outputDirectory)
    {
        var target = $"net{Environment.Version.Major}.0";
        var project = Path.Combine(outputDirectory, "OperationMigrationProof.csproj");
        await File.WriteAllTextAsync(project, $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>{target}</TargetFramework></PropertyGroup><ItemGroup><EmbeddedResource Include=\"Migrations\\**\\*\" /></ItemGroup></Project>\n").ConfigureAwait(false);

        var startInfo = CreateStartInfo(outputDirectory);
        foreach (var argument in new[] { "build", project, "--nologo", "-v", "quiet" })
            startInfo.ArgumentList.Add(argument);

        var (exitCode, output) = await ExecuteAsync(startInfo).ConfigureAwait(false);
        if (exitCode != 0)
            throw new InvalidOperationException($"Building the migration resource assembly failed ({exitCode}): {output}");

        return Path.Combine(outputDirectory, "bin", "Debug", target, "OperationMigrationProof.dll");
    }

    private static System.Diagnostics.ProcessStartInfo CreateStartInfo(string outputDirectory) => new("dotnet")
    {
        WorkingDirectory = outputDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    private static async Task<(int ExitCode, string Output)> ExecuteAsync(System.Diagnostics.ProcessStartInfo startInfo)
    {
        using var process = System.Diagnostics.Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start dotnet.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }

            throw;
        }

        return (process.ExitCode, (await stdout.ConfigureAwait(false)) + (await stderr.ConfigureAwait(false)));
    }

    public static async Task<string> ExportAsync(Type consoleType, string connectionString, string schema, string table)
    {
        var outputDirectory = Directory.CreateTempSubdirectory("coreex-operation-export-");
        try
        {
            var (exitCode, output) = await RunAsync(consoleType, outputDirectory.FullName, connectionString, "script", "operations", schema, table).ConfigureAwait(false);
            if (exitCode != 0)
                throw new InvalidOperationException($"DbEx operation script export failed ({exitCode}): {output}");

            var script = Directory.EnumerateFiles(Path.Combine(outputDirectory.FullName, "Migrations")).Single();
            return await File.ReadAllTextAsync(script).ConfigureAwait(false);
        }
        finally
        {
            outputDirectory.Delete(recursive: true);
        }
    }
}
