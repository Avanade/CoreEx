namespace CoreEx.CodeGen.Files;

/// <summary>Prepares every selected File definition and checks batch ownership before generating any artifact.</summary>
public static class FileGenerationBatch
{
    /// <summary>Discovers or explicitly selects definitions, validates the whole batch, then uses OnRamp generation modes and resources unchanged.</summary>
    /// <param name="args">Caller-owned generation options, including resource assemblies and project/output directory.</param>
    /// <param name="files">Optional explicit filenames, resolved relative to the project/output directory.</param>
    /// <param name="validateOnly">Whether to stop after preparation without writes.</param>
    /// <param name="cancellationToken">The cancellation token checked between definitions and generation operations.</param>
    /// <returns>Combined generation statistics.</returns>
    public static async Task<CodeGenStatistics> GenerateAsync(ICodeGeneratorArgs args, IEnumerable<string>? files = null, bool validateOnly = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        var basis = args.OutputDirectory ?? throw new CodeGenException("File generation requires the CodeGen project/output directory.");
        var selected = files?.ToArray() ?? [];
        if (selected.Length == 0 && args.ConfigFileName is not null && Path.GetFileName(args.ConfigFileName) != "ref-data.yaml")
            selected = [args.ConfigFileName];

        var paths = (selected.Length == 0 ? Directory.GetFiles(basis.FullName, "*-file.yaml", SearchOption.TopDirectoryOnly)
            : selected.Select(x => Path.GetFullPath(x, basis.FullName)))
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();

        if (paths.Length == 0)
            throw new CodeGenException($"No top-level *-file.yaml definitions found in '{basis.FullName}'.");

        var batch = new List<(CodeGenerator Generator, Config.CodeGenConfig Config)>();
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outputFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var folders = new List<string>();

        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!path.EndsWith("-file.yaml", StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path))
                throw new CodeGenException($"File definition must exist and end with '-file.yaml': '{path}'.");

            var itemArgs = new CodeGeneratorArgs("file-script.yaml", path);
            ((ICodeGeneratorArgs)itemArgs).CopyFrom(args);
            itemArgs.ScriptFileName = args.ScriptFileName is null or "ref-data-script.yaml" ? "file-script.yaml" : args.ScriptFileName;
            itemArgs.ConfigFileName = path;
            itemArgs.AddAssembly(typeof(FileGenerationBatch).Assembly);
            var generator = await CodeGenerator.CreateAsync(itemArgs).ConfigureAwait(false);
            var config = await generator.LoadConfigAsync().ConfigureAwait(false) as Config.CodeGenConfig
                ?? throw new CodeGenException($"The File script must prepare {typeof(Config.CodeGenConfig).FullName}.");

            if (!identities.Add(config.Name!))
                throw new CodeGenException(config, nameof(config.Name), "Duplicate integration identity in selected batch.");

            foreach (var type in config.TypeNames)
                if (!types.Add(config.RuntimeNamespace + "." + type))
                    throw new CodeGenException(config, nameof(config.Name), $"Generated CLR type collision: {config.RuntimeNamespace}.{type}.");

            foreach (var file in config.OutputFiles)
                if (!outputFiles.Add(file))
                    throw new CodeGenException(config, nameof(config.RuntimePath), $"Generated path collision: {file}.");

            var folder = config.OutputDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (folders.Any(x => folder.StartsWith(x, StringComparison.OrdinalIgnoreCase) || x.StartsWith(folder, StringComparison.OrdinalIgnoreCase)))
                throw new CodeGenException(config, nameof(config.RuntimePath), "Selected definitions must own non-overlapping output folders.");

            folders.Add(folder);
            batch.Add((generator, config));
        }

        var statistics = new CodeGenStatistics();
        foreach (var (generator, config) in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (validateOnly)
            {
                args.Logger?.LogInformation("Validated File definition {Name} revision {Version}; no artifacts generated.", config.Name, config.Version);
                statistics.RootConfig = config;
            }
            else
                statistics.Add(await generator.GenerateAsync(config).ConfigureAwait(false));
        }

        return statistics;
    }
}
