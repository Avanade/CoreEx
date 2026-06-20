using McMaster.Extensions.CommandLineUtils;

namespace CoreEx.CodeGen;

/// <summary>
/// <b>CoreEx</b>-specific code-generation console.
/// </summary>
/// <remarks>This is the main entry point for code generation and is responsible for processing the configuration and invoking the appropriate code generators.
/// <para>Supported code generators include: <b>Reference-data</b> as determined by the existence of the <c>ref-data.yaml</c>.</para></remarks>
public class CodeGenConsole : OnRamp.Console.CodeGenConsole
{
    private CommandArgument<CommandType>? _cmdArg;

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
    }

    /// <inheritdoc/>
    protected override async Task<CodeGenStatistics> OnCodeGenerationAsync()
    {
        var cmd = _cmdArg?.ParsedValue ?? CommandType.RefData;
        if (cmd == CommandType.RefData)
            return await base.OnCodeGenerationAsync().ConfigureAwait(false);

        return await new Counting.CodeGenCounter(Args).CountAsync().ConfigureAwait(false);
    }
}