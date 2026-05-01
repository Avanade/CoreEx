#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    private static bool _initializeDatabase = true;

    /// <summary>
    /// Execute the <see cref="SqlServerMigration"/> using the <typeparamref name="TAssembly"/> to include additional resource.
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Assembly"/>.</typeparam>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="configureMigrationArgs">The function to further configure the <see cref="MigrationArgs"/>.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <remarks>Where the migration is unsuccessful then an <see cref="TestFrameworkImplementor.AssertFail(string?)"/> will be automatically isued.
    /// <para>The <paramref name="connectionString"/> supports the retrieval of the value from <see cref="TesterBase.Configuration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</para></remarks>
    public static Task MigrateSqlServerDataAsync<TAssembly>(this TesterBase tester, Func<MigrationArgs, MigrationArgs>? configureMigrationArgs = null, string connectionString = "^Aspire:Microsoft:Data:SqlClient:ConnectionString")
        => MigrateSqlServerDataAsync(tester, configureMigrationArgs, connectionString, typeof(TAssembly).Assembly);

    /// <summary>
    /// Execute the <see cref="SqlServerMigration"/>.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="configureMigrationArgs">The function to further configure the <see cref="MigrationArgs"/>.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="assemblies">Zero or more assemblies to add to the migration args (see <see cref="MigrationArgsBase{TSelf}.AddAssembly(Assembly[])"/>).</param>
    /// <remarks>Where the migration is unsuccessful then an <see cref="TestFrameworkImplementor.AssertFail(string?)"/> will be automatically isued.
    /// <para>The <paramref name="connectionString"/> supports the retrieval of the value from <see cref="TesterBase.Configuration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</para></remarks>
    public static async Task MigrateSqlServerDataAsync(this TesterBase tester, Func<MigrationArgs, MigrationArgs>? configureMigrationArgs = null, string connectionString = "^Aspire:Microsoft:Data:SqlClient:ConnectionString", params Assembly[] assemblies)
    {
        // Determine the connection string and configure the migration args.
        var cs = CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(connectionString.ThrowIfNullOrEmpty(), tester.ThrowIfNull().Configuration);
        var ma = new MigrationArgs(_initializeDatabase ? MigrationCommand.All | MigrationCommand.ResetAndData : MigrationCommand.ResetAndData, cs);

        if (configureMigrationArgs is not null)
            ma = configureMigrationArgs(ma);

        if (assemblies.Length > 0)
            ma.AddAssembly(assemblies);

       // Execute the sql-server migration.
        using var m = new SqlServerMigration(ma);
        var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);

        if (!Success)
            tester.Implementor.AssertFail("SqlServerMigration failed:" + Environment.NewLine + Output);

        _initializeDatabase = false;
    }
}