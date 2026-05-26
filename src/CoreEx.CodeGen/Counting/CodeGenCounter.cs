namespace CoreEx.CodeGen.Counting;

internal class CodeGenCounter(CodeGeneratorArgs args)
{
    private static readonly string[] _countExtensions = [".cs", ".json", ".jsn", ".yaml", ".yml", ".xml", ".sql", ".mysql", ".pgsql"];

    public Task<CodeGenStatistics> CountAsync()
    {
        if (args is null || args.OutputDirectory?.Parent is null || args.Logger is null)
            throw new ArgumentNullException(nameof(args), "Arguments, and its OutputDirectory, and Logger cannot be null.");

        if (!args.Logger.IsEnabled(LogLevel.Information))
            throw new ArgumentException("Logger must be enabled for Information level.", nameof(args));

        args.Logger.LogInformation("{Content}", $"Counting: {args.OutputDirectory.Parent.FullName}");
        args.Logger.LogInformation("{Content}", $"Include: {string.Join(", ", _countExtensions)}");
        args.Logger.LogInformation("{Content}", string.Empty);

        var sw = Stopwatch.StartNew();
        var dcs = new DirectoryCountStatistics(args.OutputDirectory.Parent);
        CountDirectoryAndItsChildren(dcs);

        var columnLength = Math.Max(dcs.TotalLineCount.ToString().Length, 5);
        dcs.Write(args.Logger, columnLength, 0, dcs.Directory.Parent is null ? 0 : dcs.Directory.Parent.FullName.Length + 1);

        args.Logger.LogInformation("{Content}", string.Empty);
        args.Logger.LogInformation("{Content}", "Note: Roslyn source generated files are excluded from the counts.");

        sw.Stop();
        return Task.FromResult(new CodeGenStatistics { ElapsedMilliseconds = sw.ElapsedMilliseconds });
    }

    /// <summary>
    /// Count the directory and its children (recursive).
    /// </summary>
    private static void CountDirectoryAndItsChildren(DirectoryCountStatistics dcs)
    {
        foreach (var di in dcs.Directory.EnumerateDirectories())
        {
            if (di.Name.Equals("obj", StringComparison.InvariantCultureIgnoreCase) || di.Name.Equals("bin", StringComparison.InvariantCultureIgnoreCase) || di.Name.StartsWith('.'))
                continue;

            CountDirectoryAndItsChildren(dcs.AddChildDirectory(di));
        }

        foreach (var fi in dcs.Directory.EnumerateFiles())
        {
            if (!_countExtensions.Contains(fi.Extension, StringComparer.InvariantCultureIgnoreCase))
                continue;

            bool isGenerated = fi.Name.EndsWith($".g{fi.Extension}", StringComparison.OrdinalIgnoreCase);

            using var sr = fi.OpenText();
            while (sr.ReadLine() is not null)
            {
                dcs.IncrementLineCount(isGenerated);
            }

            dcs.IncrementFileCount(isGenerated);
        }
    }
}