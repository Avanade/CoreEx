namespace CoreEx.Database.SqlServer.Test.Unit;

[TestFixture]
public class SqlServerOperationScriptTests : OperationScriptTestsBase
{
    protected override Type ConsoleType => typeof(CoreEx.Database.SqlServer.Test.Console.Program);
    protected override string ConnectionString => OperationScriptTestHelper.SqlServerOfflineConnection;
    protected override string Extension => "sql";
    protected override string DefaultTable => SqlServerOperationStore.DefaultTableName;
    protected override string ProviderDescription => "SQL Server migration";
    protected override string QualifiedTable(string schema, string table) => $"[{schema}].[{table}]";

    [Test]
    public void Configuration_Probes_Application_Before_Provider()
    {
        var console = new SqlServerMigrationConsole(ConnectionString)
            .Configure(c => CoreEx.Database.SqlServer.Test.Console.Program.ConfigureMigrationArgs(c.Args));

        console.Args.ProbeAssemblies.Take(2).Select(a => a.Assembly)
            .Should().Equal(ConsoleType.Assembly, typeof(SqlServerDatabase).Assembly);
    }
}
