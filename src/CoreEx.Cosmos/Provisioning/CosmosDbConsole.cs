namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Represents the <b>Cosmos DB</b> provisioning console; the <b>Cosmos DB</b> equivalent of the <c>DbEx</c> migration console (e.g. <c>PostgresMigrationConsole</c>).
/// </summary>
/// <remarks>Usage: <c>dotnet run -- [options] &lt;command&gt;</c> where <c>command</c> is one (or a comma-separated combination) of the <see cref="CosmosDbProvisionCommand"/> names; use <c>--help</c> to list the options.
/// The destructive <see cref="CosmosDbProvisionCommand.Drop"/> and <see cref="CosmosDbProvisionCommand.Reset"/> commands require confirmation unless <c>--accept-prompts</c> is specified.</remarks>
public sealed class CosmosDbConsole
{
    private CosmosDbConsole(string connectionString, string databaseId, Assembly assembly)
    {
        ConnectionString = connectionString.ThrowIfNullOrEmpty();
        Args = new CosmosDbProvisionArgs { DatabaseId = databaseId.ThrowIfNullOrEmpty() }.AddAssembly(assembly);
    }

    /// <summary>
    /// Creates a new <see cref="CosmosDbConsole"/>.
    /// </summary>
    /// <typeparam name="TProgram">The <see cref="Type"/> used to infer the <see cref="Assembly"/> containing the embedded <c>Data</c> resources.</typeparam>
    /// <param name="connectionString">The default connection string; can be overridden using <c>-cs</c> or <c>-cv</c>.</param>
    /// <param name="databaseId">The default <see cref="Database.Id"/>; can be overridden using <c>-d</c>.</param>
    public static CosmosDbConsole Create<TProgram>(string connectionString, string databaseId) => new(connectionString, databaseId, typeof(TProgram).Assembly);

    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets the <see cref="CosmosDbProvisionArgs"/>.
    /// </summary>
    public CosmosDbProvisionArgs Args { get; }

    /// <summary>
    /// Configures the <see cref="CosmosDbConsole"/>.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The <see cref="CosmosDbConsole"/> to support fluent-style method-chaining.</returns>
    public CosmosDbConsole Configure(Action<CosmosDbConsole> configure)
    {
        configure.ThrowIfNull()(this);
        return this;
    }

    /// <summary>
    /// Runs the console using the command-line <paramref name="args"/>.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The status code whereby zero indicates success.</returns>
    public async Task<int> RunAsync(string[] args)
    {
        var name = Assembly.GetEntryAssembly()?.GetName().Name ?? "CosmosDb";
        var command = CosmosDbProvisionCommand.None;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "-?" or "-h" or "--help":
                    WriteHelp(name);
                    return 0;

                case "-cs" or "--connection-string" when i + 1 < args.Length:
                    ConnectionString = args[++i];
                    break;

                case "-cv" or "--connection-varname" when i + 1 < args.Length:
                    var varName = args[++i];
                    var value = Environment.GetEnvironmentVariable(varName);
                    if (string.IsNullOrEmpty(value))
                        return Fail($"Environment variable '{varName}' (--connection-varname) does not exist or has no value.");

                    ConnectionString = value;
                    break;

                case "-d" or "--database" when i + 1 < args.Length:
                    Args.DatabaseId = args[++i];
                    break;

                case "-p" or "--param" when i + 1 < args.Length:
                    var param = args[++i];
                    var index = param.IndexOf('=');
                    if (index <= 0)
                        return Fail($"Parameter '{param}' is invalid; must be expressed as a 'Name=Value' pair.");

                    Args.Parameters[param[..index].Trim()] = param[(index + 1)..];
                    break;

                case "--accept-prompts":
                    Args.AcceptPrompts = true;
                    break;

                default:
                    if (arg.Length > 0 && !arg.StartsWith('-') && !char.IsDigit(arg[0]) && Enum.TryParse<CosmosDbProvisionCommand>(arg, true, out var cmd))
                        command |= cmd;
                    else
                        return Fail($"Unrecognized or incomplete argument: {arg}", name);

                    break;
            }
        }

        if (command == CosmosDbProvisionCommand.None)
        {
            WriteHelp(name);
            return 1;
        }

        Args.Output = Console.Out;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            WriteBanner(name, command);

            if (!Confirm(command))
                return 1;

            using var client = CosmosDbClientFactory.Create(ConnectionString);
            await new CosmosDbProvisioner(client, Args).RunAsync(command).ConfigureAwait(false);
            Console.Out.WriteLine($"{name} Complete. [{sw.Elapsed.TotalMilliseconds:0.0}ms]");
            Console.Out.WriteLine();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Writes the error and (optionally) the help; returns the failure status code.
    /// </summary>
    private static int Fail(string message, string? helpName = null)
    {
        Console.Error.WriteLine(message);
        if (helpName is not null)
            WriteHelp(helpName);

        return 1;
    }

    /// <summary>
    /// Confirms (unless <see cref="CosmosDbProvisionArgs.AcceptPrompts"/>) the destructive commands.
    /// </summary>
    private bool Confirm(CosmosDbProvisionCommand command)
    {
        if (Args.AcceptPrompts)
            return true;

        if (command.HasFlag(CosmosDbProvisionCommand.Drop))
        {
            if (!GetYesNo("DROP: Confirm that where the specified database already exists it should be dropped?"))
            {
                Console.Error.WriteLine("Database drop was not confirmed; no execution occurred.");
                return false;
            }
        }
        else if (command.HasFlag(CosmosDbProvisionCommand.Reset))
        {
            if (!GetYesNo("RESET: Confirm that where the declared containers already exist they should be deleted and recreated (all existing data and container settings will be lost)?"))
            {
                Console.Error.WriteLine("Container reset was not confirmed; no execution occurred.");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Prompts for a yes/no response; defaults to no.
    /// </summary>
    private static bool GetYesNo(string prompt)
    {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Out.Write($"{prompt} [y/N] ");
        Console.ForegroundColor = color;

        var response = Console.In.ReadLine()?.Trim();
        Console.Out.WriteLine();
        return response is not null && (response.Equals("y", StringComparison.OrdinalIgnoreCase) || response.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Writes the logo.
    /// </summary>
    private static void WriteLogo()
    {
        Console.Out.WriteLine("╔═╗┌─┐┬─┐┌─┐╔═╗─┐ ┬  ╔═╗┌─┐┌─┐┌┬┐┌─┐┌─┐  ╔╦╗╔╗ ");
        Console.Out.WriteLine("║  │ │├┬┘├┤ ║╣ ┌┴┬┘  ║  │ │└─┐││││ │└─┐   ║║╠╩╗");
        Console.Out.WriteLine("╚═╝└─┘┴└─└─┘╚═╝┴ └─  ╚═╝└─┘└─┘┴ ┴└─┘└─┘  ═╩╝╚═╝");
        Console.Out.WriteLine();
    }

    /// <summary>
    /// Writes the banner and settings; the connection string is never written as it contains the account key.
    /// </summary>
    private void WriteBanner(string name, CosmosDbProvisionCommand command)
    {
        var o = Console.Out;
        WriteLogo();
        o.WriteLine($"{name} Database Tool. [Azure Cosmos DB]");
        o.WriteLine();
        o.WriteLine($"Command = {command}");
        o.WriteLine($"Endpoint = {GetEndpoint()}");
        o.WriteLine($"Database = {Args.DatabaseId}");

        if (Args.Parameters.Count > 0)
        {
            o.WriteLine("Parameters:");
            foreach (var p in Args.Parameters)
            {
                o.WriteLine($"  {p.Key} = {p.Value}");
            }
        }

        if (Args.Containers.Count > 0)
        {
            o.WriteLine("Containers:");
            foreach (var c in Args.Containers)
            {
                o.WriteLine($"  {c.Id}  (partition key: {c.PartitionKeyPath}{(c.IsReferenceData ? ", reference data" : string.Empty)}{(c.IsOutboxLease ? ", outbox lease" : string.Empty)})");
            }
        }

        if (Args.Assemblies.Count > 0)
        {
            o.WriteLine("Assemblies:");
            foreach (var a in Args.Assemblies)
            {
                o.WriteLine($"  {a.FullName}");
            }
        }

        o.WriteLine();
    }

    /// <summary>
    /// Gets the account endpoint from the connection string (without the key).
    /// </summary>
    private string GetEndpoint()
    {
        foreach (var part in ConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("AccountEndpoint=", StringComparison.OrdinalIgnoreCase))
                return part["AccountEndpoint=".Length..];
        }

        return "(unknown)";
    }

    /// <summary>
    /// Writes the help.
    /// </summary>
    private static void WriteHelp(string name)
    {
        var o = Console.Out;
        WriteLogo();
        o.WriteLine($"{name} Database Tool. [Azure Cosmos DB]");
        o.WriteLine();
        o.WriteLine($"Usage: {name} [options] <command>");
        o.WriteLine();
        o.WriteLine("Arguments:");
        WriteItem("command", $"Database provisioning command (a comma-separated combination is allowed). Allowed values are: {string.Join(", ", Enum.GetNames<CosmosDbProvisionCommand>())}.");
        o.WriteLine();
        o.WriteLine("Options:");
        WriteItem("-?|-h|--help", "Show help information.");
        WriteItem("-cs|--connection-string", "Cosmos DB connection string.");
        WriteItem("-cv|--connection-varname", "Cosmos DB connection string environment variable name.");
        WriteItem("-d|--database", "Cosmos DB database identifier.");
        WriteItem("-p|--param", "Parameter expressed as a 'Name=Value' pair (multiple can be specified); referenced in the seed data as '^Name' (or '(^Name)' where embedded within a value).");
        WriteItem("--accept-prompts", "Accept prompts; command should _not_ stop and wait for user confirmation (DROP or RESET commands).");
        o.WriteLine();
    }

    /// <summary>
    /// Writes a help item, wrapping the description within a fixed-width column.
    /// </summary>
    private static void WriteItem(string item, string description)
    {
        const int column = 30;
        const int width = 130 - column;

        var lines = new List<string>();
        var line = string.Empty;
        foreach (var word in description.Split(' '))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line);
                line = word;
            }
            else
                line = line.Length == 0 ? word : $"{line} {word}";
        }

        lines.Add(line);
        Console.Out.WriteLine($"  {item.PadRight(column - 2)}{lines[0]}");
        foreach (var l in lines.Skip(1))
        {
            Console.Out.WriteLine($"{new string(' ', column)}{l}");
        }
    }
}