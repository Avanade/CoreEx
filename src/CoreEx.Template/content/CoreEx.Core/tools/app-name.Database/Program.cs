using CoreEx.Database;
// #if implement-sqlserver
using DbEx.Migration;
using DbEx.SqlServer.Console;

namespace app-name.Database;

/// <summary>Represents the <b>database utilities</b> program.</summary>
public class Program
{
    /// <summary>Main startup.</summary>
    public static Task<int> Main(string[] args) => SqlServerMigrationConsole
        .Create<Program>("Data Source=127.0.0.1,1433;Initial Catalog=domain-name;User id=sa;Password=yourStrong(!)Password;TrustServerCertificate=true")
        .Configure(c => ConfigureMigrationArgs(c.Args))
        .RunAsync(args);

    /// <summary>Configure the <see cref="MigrationArgs"/>.</summary>
    public static MigrationArgs ConfigureMigrationArgs(MigrationArgs args)
    {
        args.AddAssembly<SqlStatement>().AddAssembly<Program>();   // SqlStatement = CoreEx EF code-gen templates; Program = this project's embedded migrations/data. Both REQUIRED — the API tests call ConfigureMigrationArgs directly (not via Main), so the Database assembly must be added here. Do not remove.
        args.DataResetFilterPredicate = ts => ts.Schema == "domain-name";   // Only reset data for the specified schema.
        return args;
    }
}
// #elif implement-postgres
using DbEx.Migration;
using DbEx.Postgres.Console;

namespace app-name.Database;

/// <summary>Represents the <b>database utilities</b> program.</summary>
public class Program
{
    /// <summary>Main startup.</summary>
    public static Task<int> Main(string[] args) => PostgresMigrationConsole
        .Create<Program>("Server=127.0.0.1;Database=domain-name-lower;Username=postgres;Password=yourStrong#!Password")
        .Configure(c => ConfigureMigrationArgs(c.Args))
        .RunAsync(args);

    /// <summary>Configure the <see cref="MigrationArgs"/>.</summary>
    public static MigrationArgs ConfigureMigrationArgs(MigrationArgs args)
    {
        args.AddAssembly<SqlStatement>().AddAssembly<Program>();   // SqlStatement = CoreEx EF code-gen templates; Program = this project's embedded migrations/data. Both REQUIRED — the API tests call ConfigureMigrationArgs directly (not via Main), so the Database assembly must be added here. Do not remove.
        args.DataResetFilterPredicate = ts => ts.Schema == "db-name";   // Only reset data for the specified schema.
        return args;
    }
}
// #endif