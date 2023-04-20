using DbEx.Migration;
using DbEx.SqlServer.Console;

namespace My.Hr.Database
{
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
        internal static Task<int> Main(string[] args) => new SqlServerMigrationConsole("Data Source=.;Initial Catalog=My.HrDb;Integrated Security=True;TrustServerCertificate=true")
            .Configure(c => ConfigureMigrationArgs(c.Args))
            .RunAsync(args);

        /// <summary>
        /// Configure the <see cref="MigrationArgs"/>.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgs"/>.</param>
        /// <returns>The <see cref="MigrationArgs"/>.</returns>
        public static MigrationArgs ConfigureMigrationArgs(MigrationArgs args)
        {
            args.ConnectionStringEnvironmentVariableName = "My_HrDb";
            args.DataParserArgs.RefDataColumnDefaults.TryAdd("IsActive", _ => true);
            args.DataParserArgs.RefDataColumnDefaults.TryAdd("SortOrder", i => i);
            args.AddAssembly<Program>();
            return args;
        }
    }
}