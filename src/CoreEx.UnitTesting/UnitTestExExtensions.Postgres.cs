#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method that defaults the <paramref name="serviceKey"/> to <see cref="PostgresOutboxPublisher.DefaultServiceKey"/> where invoking the underlying <see cref="UseExpectedEventPublisher(IServiceCollection, string, bool)"/>.
    /// <para>The <paramref name="bypassPassThrough"/> when set to <see langword="true"/> will bypass the pass-through to the original event publisher and leverage the <see cref="NoOpEventPublisher"/> instead.</para></remarks>
    public static IServiceCollection UseExpectedPostgresOutboxPublisher(this IServiceCollection services, string serviceKey = PostgresOutboxPublisher.DefaultServiceKey, bool bypassPassThrough = false)
        => UseExpectedEventPublisher(services, serviceKey, bypassPassThrough);

    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions; whilst also adding post-run expectations for the captured events.
    /// </summary>
    /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="AspNetCore.ApiTester{TEntryPoint}"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <param name="expectNoEvents">Indicates whether to expect no events to be published.</param>
    /// <returns>The <see cref="AspNetCore.ApiTester{TEntryPoint}"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="expectNoEvents"/> parameter is only actioned when no explicit event expectations are defined for the underlying test; acts as a catch all.</remarks>
    public static AspNetCore.ApiTester<TEntryPoint> UseExpectedPostgresOutboxPublisher<TEntryPoint>(this AspNetCore.ApiTester<TEntryPoint> tester, string serviceKey = PostgresOutboxPublisher.DefaultServiceKey, bool bypassPassThrough = false, bool expectNoEvents = true) where TEntryPoint : class
        => tester.ConfigureServices(services => services.UseExpectedPostgresOutboxPublisher(serviceKey, bypassPassThrough))
                 .AddEventExpectationsPostRun(serviceKey, expectNoEvents);

    /// <summary>
    /// Execute the <see cref="PostgresMigration"/> using the <typeparamref name="TAssembly"/> to include additional resource.
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Assembly"/>.</typeparam>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="configureMigrationArgs">The function to further configure the <see cref="MigrationArgs"/>.</param>
    /// <param name="connectionString">The database connection string configuration key.</param>
    /// <remarks>Where the migration is unsuccessful then an <see cref="TestFrameworkImplementor.AssertFail(string?)"/> will be automatically issued.
    /// <para>The <paramref name="connectionString"/> supports the retrieval of the value from <see cref="TesterBase.Configuration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</para></remarks>
    public static Task MigratePostgresDataAsync<TAssembly>(this TesterBase tester, Func<MigrationArgs, MigrationArgs>? configureMigrationArgs = null, string connectionString = "^Aspire:Npgsql:ConnectionString")
        => MigratePostgresDataAsync(tester, configureMigrationArgs, connectionString, typeof(TAssembly).Assembly);

    /// <summary>
    /// Execute the <see cref="PostgresMigration"/> using the <typeparamref name="TAssembly"/> to include additional resource.
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Assembly"/>.</typeparam>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="resourceFileNames">The resource file names to include in the data loading; see <see cref="DataParserArgs.AddNamed{TAssembly}(string[])"/>.</param>
    /// <param name="configureMigrationArgs">The function to further configure the <see cref="MigrationArgs"/>.</param>
    /// <param name="connectionString">The database connection string configuration key.</param>
    /// <remarks>Where the migration is unsuccessful then an <see cref="TestFrameworkImplementor.AssertFail(string?)"/> will be automatically issued.
    /// <para>The <paramref name="connectionString"/> supports the retrieval of the value from <see cref="TesterBase.Configuration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</para></remarks>
    public static Task MigratePostgresDataAsync<TAssembly>(this TesterBase tester, string[] resourceFileNames, Func<MigrationArgs, MigrationArgs>? configureMigrationArgs = null, string connectionString = "^Aspire:Npgsql:ConnectionString")
    {
        if (configureMigrationArgs is null)
        {
            return MigratePostgresDataAsync<TAssembly>(tester, ma =>
            {
                ma.DataParserArgs.AddNamed<TAssembly>(resourceFileNames);
                return ma;
            }, connectionString);
        }
        else
        {
            return MigratePostgresDataAsync<TAssembly>(tester, ma =>
            {
                var cma = configureMigrationArgs(ma);
                cma.DataParserArgs.AddNamed<TAssembly>(resourceFileNames);
                return cma;
            }, connectionString);
        }
    }

    /// <summary>
    /// Execute the <see cref="PostgresMigration"/>.
    /// </summary>
    /// <param name="tester">The <see cref="TesterBase"/>.</param>
    /// <param name="configureMigrationArgs">The function to further configure the <see cref="MigrationArgs"/>.</param>
    /// <param name="connectionString">The database connection string configuration key.</param>
    /// <param name="assemblies">Zero or more assemblies to add to the migration args (see <see cref="MigrationArgsBase{TSelf}.AddAssembly(Assembly[])"/>).</param>
    /// <remarks>Where the migration is unsuccessful then an <see cref="TestFrameworkImplementor.AssertFail(string?)"/> will be automatically issued.
    /// <para>The <paramref name="connectionString"/> supports the retrieval of the value from <see cref="TesterBase.Configuration"/> where prefixed with '<c>config:</c>' or '<c>^</c>', or is wrapped with '<c>%</c>'.</para></remarks>
    public static async Task MigratePostgresDataAsync(this TesterBase tester, Func<MigrationArgs, MigrationArgs>? configureMigrationArgs = null, string connectionString = "^Aspire:Npgsql:ConnectionString", params Assembly[] assemblies)
    {
        // Determine the connection string and configure the migration args.
        var cs = CoreEx.Abstractions.Internal.GetValueFromConfigurationWhereApplicable(connectionString.ThrowIfNullOrEmpty(), tester.ThrowIfNull().Configuration);
        var ma = new MigrationArgs(_connectionStrings.TryAdd(cs, 0) ? MigrationCommand.All | MigrationCommand.ResetAndData : MigrationCommand.ResetAndData, cs);

        if (configureMigrationArgs is not null)
            ma = configureMigrationArgs(ma);

        if (assemblies.Length > 0)
            ma.AddAssembly(assemblies);

        // Execute the PostgreSQL migration.
        using var m = new PostgresMigration(ma);
        var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);

        if (!Success)
            tester.Implementor.AssertFail("PostgresMigration failed:" + Environment.NewLine + Output);
    }
}