namespace CoreEx.CodeGen;

/// <summary>
/// <b>CoreEx</b>-specific code-generation console.
/// </summary>
/// <remarks>This is the main entry point for code generation and is responsible for processing the configuration and invoking the appropriate code generators.
/// <para>Commands are <b>RefData</b> (default, <c>ref-data.yaml</c>), <b>File</b> (selected <c>*-file.yaml</c> definitions), and <b>Count</b>.</para></remarks>
public class CodeGenConsole : OnRamp.Console.CodeGenConsole
{
    private CommandArgument<CommandType>? _cmdArg;
    private CommandArgument? _files;
    private CommandOption? _validate;

    /// <summary>
    /// Gets the default masthead text.
    /// </summary>
    /// <remarks>Defaults to 'CoreEx Code-Gen Tool' formatted using <see href="https://www.patorjk.com/software/taag/#p=display&amp;f=Calvin+S&amp;t=CoreEx+Code-Gen+Tool%0A"/>.</remarks>
    public const string DefaultMastheadText = @"
╔═╗┌─┐┬─┐┌─┐╔═╗─┐ ┬  ╔═╗┌─┐┌┬┐┌─┐  ╔═╗┌─┐┌┐┌  ╔╦╗┌─┐┌─┐┬  
║  │ │├┬┘├┤ ║╣ ┌┴┬┘  ║  │ │ ││├┤───║ ╦├┤ │││   ║ │ ││ ││  
╚═╝└─┘┴└─└─┘╚═╝┴ └─  ╚═╝└─┘─┴┘└─┘  ╚═╝└─┘┘└┘   ╩ └─┘└─┘┴─┘
";

    /// <summary>
    /// Creates a new instance of the <see cref="CodeGenConsole"/> class using the calling assembly for code generation.
    /// </summary>
    /// <returns>The <see cref="CodeGenConsole"/> instance.</returns>
    public static CodeGenConsole Create() => Create([Assembly.GetCallingAssembly()]);

    /// <summary>
    /// Creates a new instance of the <see cref="CodeGenConsole"/> class using the specified assemblies for code generation.
    /// </summary>
    /// <param name="assemblies">The assemblies to use for code generation.</param>
    /// <returns>The <see cref="CodeGenConsole"/> instance.</returns>
    public static CodeGenConsole Create(Assembly[] assemblies)
    {
        var path = GetBaseExeDirectory();
        var args = CodeGeneratorArgs.Create<CodeGenConsole>("ref-data-script.yaml", Path.Combine(path, "ref-data.yaml")).AddAssembly(typeof(CodeGenConsole).Assembly).AddAssembly(assemblies);
        args.OutputDirectory = new DirectoryInfo(path);
        return new CodeGenConsole(args);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeGenConsole"/> class.
    /// </summary>
    private CodeGenConsole(CodeGeneratorArgs args) : base(args, OnRamp.Console.SupportedOptions.AllExceptDatabase) => MastheadText = DefaultMastheadText;

    /// <inheritdoc/>
    protected override void OnBeforeExecute(CommandLineApplication app)
    {
        // Add the command argument.
        _cmdArg = app.Argument<CommandType>("command", "The command to execute.");
        _cmdArg.DefaultValue = CommandType.RefData;
        _files = app.Argument("files", "File definition filenames ending in -file.yaml; omitted selects all top-level definitions.", multipleValues: true);
        _validate = app.Option("--validate", "Prepare and validate the selected configuration batch without writing.", CommandOptionType.NoValue);
    }

    /// <inheritdoc/>
    protected override async Task<CodeGenStatistics> OnCodeGenerationAsync()
    {
        var cmd = _cmdArg?.ParsedValue ?? CommandType.RefData;
        if (cmd != CommandType.File && _files?.Values.Count > 0)
            throw new CodeGenException("Positional filenames are supported only by the File command.");

        switch (cmd)
        {
            case CommandType.RefData:
                if (_validate?.HasValue() != true)
                    return await base.OnCodeGenerationAsync().ConfigureAwait(false);

                var generator = await CodeGenerator.CreateAsync(Args).ConfigureAwait(false);
                return new CodeGenStatistics { RootConfig = (IRootConfig)await generator.LoadConfigAsync().ConfigureAwait(false) };
            case CommandType.Count:
                if (_validate?.HasValue() == true)
                    throw new CodeGenException("--validate is not supported by Count.");

                return await new Counting.CodeGenCounter(Args).CountAsync().ConfigureAwait(false);
            case CommandType.File:
                return await Files.FileGenerationBatch.GenerateAsync(Args, _files?.Values.OfType<string>(), _validate?.HasValue() == true).ConfigureAwait(false);
            default:
                throw new CodeGenException($"Unsupported code-generation command '{cmd}'.");
        }
    }
}
