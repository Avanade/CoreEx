using CoreEx.Database;
using DbEx.Migration;
using DbEx.Postgres.Console;

namespace Contoso.Products.Database;

/// <summary>
/// Represents the <b>database utilities</b> program (capability).
/// </summary>
public class Program
{
    /// <summary>
    /// Main startup.
    /// </summary>
    /// <param name="args">The startup arguments.</param>
    /// <returns>The status code whereby zero indicates success.</returns>
    public static Task<int> Main(string[] args) => PostgresMigrationConsole
        .Create<Program>("Server=127.0.0.1;Database=contoso;Username=postgres;Password=yourStrong#!Password")   
        .Configure(c => ConfigureMigrationArgs(c.Args))
        .RunAsync(args);

    /// <summary>
    /// Configure the <see cref="MigrationArgs"/>.
    /// </summary>
    /// <param name="args">The <see cref="MigrationArgs"/>.</param>
    /// <returns>The <see cref="MigrationArgs"/>.</returns>
    public static MigrationArgs ConfigureMigrationArgs(MigrationArgs args)
    {
        args.AddAssembly<SqlStatement>().AddAssembly<Program>()
            .IncludeExtendedSchemaScripts()
            .DataParserArgs
                .RefDataColumnDefault("SortOrder", _ => 0)
                .RefDataColumnDefault("Scale", _ => 0);

        // Only reset data for the 'products' schema.
        args.DataResetFilterPredicate = ts => ts.Schema == "products";

        return args;
    }
}