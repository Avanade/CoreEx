namespace CoreEx.CodeGen.Cleaning;

/// <summary>
/// Deletes generated files from the solution directories.
/// </summary>
internal class CodeGenCleaner(CodeGeneratorArgs args)
{
    /// <summary>
    /// Deletes generated files, or reports them when simulation is enabled.
    /// </summary>
    /// <returns>The code-generation statistics.</returns>
    public Task<CodeGenStatistics> CleanAsync()
    {
        if (args is null || args.OutputDirectory is null || args.Logger is null)
            throw new ArgumentNullException(nameof(args), "Arguments, and its OutputDirectory, and Logger cannot be null.");

        if (!args.Logger.IsEnabled(LogLevel.Information))
            throw new ArgumentException("Logger must be enabled for Information level.", nameof(args));

        if (!args.OutputDirectory.Exists)
            throw new DirectoryNotFoundException($"The specified output directory '{args.OutputDirectory.FullName}' does not exist.");

        var cleanDirectory = CodeGenDirectoryWalker.GetSolutionDirectory(args.OutputDirectory);
        args.Logger.LogInformation("{Content}", $"Cleaning: {cleanDirectory.FullName}");
        args.Logger.LogInformation("{Content}", "Include: **.g.**");
        args.Logger.LogInformation("{Content}", string.Empty);

        var stopwatch = Stopwatch.StartNew();
        var generatedFiles = CodeGenDirectoryWalker.EnumerateFiles(cleanDirectory)
            .Where(file => file.Name.Contains(".g.", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var file in generatedFiles)
        {
            if (args.IsSimulation)
                args.Logger.LogInformation("{Content}", $"Would delete: {file.FullName}");
            else
                file.Delete();
        }

        args.Logger.LogInformation("{Content}", args.IsSimulation
            ? $"Simulation complete; {generatedFiles.Length} generated file(s) would be deleted."
            : $"Deleted {generatedFiles.Length} generated file(s).");

        stopwatch.Stop();
        return Task.FromResult(new CodeGenStatistics { ElapsedMilliseconds = stopwatch.ElapsedMilliseconds });
    }
}
