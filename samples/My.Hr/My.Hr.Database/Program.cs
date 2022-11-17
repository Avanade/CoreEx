using DbEx.SqlServer.Console;
using System.Reflection;

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
        internal static Task<int> Main(string[] args) => RunMigrator("Data Source=.;Initial Catalog=My.HrDb;Integrated Security=True;TrustServerCertificate=true", null, args);

        public static Task<int> RunMigrator(string connectionString, Assembly? assembly = null, params string[] args)
            => SqlServerMigrationConsole
                .Create<Program>(connectionString)
                .Configure(c =>
                {
                    c.Args.ConnectionStringEnvironmentVariableName = "My_HrDb";
                    c.Args.DataParserArgs.RefDataColumnDefaults.TryAdd("IsActive", _ => true);
                    c.Args.DataParserArgs.RefDataColumnDefaults.TryAdd("SortOrder", i => i);
                    if (assembly != null)
                        c.Args.AddAssembly(assembly);
                })
                .RunAsync(args);
    }
}