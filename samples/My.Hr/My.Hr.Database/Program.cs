using DbEx.Console;
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
            => SqlServerMigratorConsole
                .Create<Program>(connectionString)
                .ConsoleArgs(a =>
                {
                    a.ConnectionStringEnvironmentVariableName = "My_HrDb";
                    a.DataParserArgs.RefDataColumnDefaults.TryAdd("IsActive", _ => true);
                    a.DataParserArgs.RefDataColumnDefaults.TryAdd("SortOrder", i => i);
                    if (assembly != null)
                        a.AddAssembly(assembly);
                })
                .RunAsync(args);
    }
}