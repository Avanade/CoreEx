namespace CoreEx.Database.Postgres.Test.Unit;

[TestFixture]
public class PostgresOperationScriptTests : OperationScriptTestsBase
{
    protected override Type ConsoleType => typeof(CoreEx.Database.Postgres.Test.Console.Program);
    protected override string ConnectionString => OperationScriptTestHelper.PostgresOfflineConnection;
    protected override string Extension => "pgsql";
    protected override string DefaultTable => PostgresOperationStore.DefaultTableName;
    protected override string ProviderDescription => "PostgreSQL migration";
    protected override string QualifiedTable(string schema, string table) => $"\"{schema}\".\"{table}\"";

    [Test]
    public void Configuration_Probes_Application_Before_Provider()
    {
        var console = new PostgresMigrationConsole(ConnectionString)
            .Configure(c => CoreEx.Database.Postgres.Test.Console.Program.ConfigureMigrationArgs(c.Args));

        console.Args.ProbeAssemblies.Take(2).Select(a => a.Assembly)
            .Should().Equal(ConsoleType.Assembly, typeof(PostgresDatabase).Assembly);
    }
}
