using CoreEx.Database.SqlServer.Test.Console;
using DbEx;
using DbEx.Migration;
using DbEx.SqlServer.Migration;
using Microsoft.Extensions.Configuration;

namespace CoreEx.Database.SqlServer.Test.Unit;

public abstract class DatabaseTestBase : WithGenericTester<EntryPoint>
{
    [OneTimeSetUp()]
    public async Task OneTimeSetUp()
    {
        var cs = Test.Configuration.GetValue<string>("Aspire:Microsoft:Data:SqlClient:ConnectionString");
        var ma = Program.ConfigureMigrationArgs(new MigrationArgs(MigrationCommand.DropAndAll, cs)).AddAssembly<DatabaseTestBase>();

        using var m = new SqlServerMigration(ma);
        var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);

        TestContext.Progress.WriteLine(Output);

        if (!Success)
            Assert.Fail("SqlServerMigration failed.");
    }
}