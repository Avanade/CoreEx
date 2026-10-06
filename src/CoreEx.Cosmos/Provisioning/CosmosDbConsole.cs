namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Represents the <b>Cosmos DB</b> provisioning console; the <b>Cosmos DB</b> equivalent of the <c>DbEx</c> migration console (e.g. <c>PostgresMigrationConsole</c>).
/// </summary>
/// <remarks>Usage: <c>dotnet run -- &lt;command&gt; [-cs &lt;connection-string&gt;] [-d &lt;database-id&gt;]</c> where <c>command</c> is one (or a comma-separated combination) of the <see cref="CosmosDbProvisionCommand"/> names.</remarks>
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
    /// <param name="connectionString">The default connection string; can be overridden using <c>-cs</c>.</param>
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
        var command = CosmosDbProvisionCommand.None;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "-h" or "--help" or "-?":
                    WriteHelp();
                    return 0;

                case "-cs" or "--connection-string" when i + 1 < args.Length:
                    ConnectionString = args[++i];
                    break;

                case "-d" or "--database" when i + 1 < args.Length:
                    Args.DatabaseId = args[++i];
                    break;

                default:
                    if (arg.Length > 0 && !char.IsDigit(arg[0]) && Enum.TryParse<CosmosDbProvisionCommand>(arg, true, out var cmd))
                        command |= cmd;
                    else
                    {
                        Console.Error.WriteLine($"Unrecognized or incomplete argument: {arg}");
                        WriteHelp();
                        return 1;
                    }

                    break;
            }
        }

        if (command == CosmosDbProvisionCommand.None)
        {
            WriteHelp();
            return 1;
        }

        Args.Output = Console.Out;

        try
        {
            using var client = CosmosDbClientFactory.Create(ConnectionString);
            await new CosmosDbProvisioner(client, Args).RunAsync(command).ConfigureAwait(false);
            Console.Out.WriteLine("Completed successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Writes the help.
    /// </summary>
    private static void WriteHelp()
    {
        Console.Out.WriteLine("Usage: <program> <command> [-cs <connection-string>] [-d <database-id>]");
        Console.Out.WriteLine();
        Console.Out.WriteLine("Commands (a comma-separated combination is allowed):");
        foreach (var name in Enum.GetNames<CosmosDbProvisionCommand>().Where(x => x != nameof(CosmosDbProvisionCommand.None)))
        {
            Console.Out.WriteLine($"  {name}");
        }
    }
}
