using CoreEx.Database.Postgres.Test.Console;
using DbEx;
using DbEx.Migration;
using DbEx.Postgres.Migration;
using Microsoft.Extensions.Configuration;

namespace CoreEx.Database.Postgres.Test.Unit;

public abstract class DatabaseTestBase : WithGenericTester<EntryPoint>
{
    [OneTimeSetUp()]
    public async Task OneTimeSetUp()
    {
        var cs = Test.Configuration.GetValue<string>("Aspire:Npgsql:ConnectionString");
        var ma = Program.ConfigureMigrationArgs(new MigrationArgs(MigrationCommand.DropAndAll, cs)).AddAssembly<DatabaseTestBase>();

        using var m = new PostgresMigration(ma);
        var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);

        TestContext.Progress.WriteLine(Output);

        if (!Success)
            Assert.Fail("PostgresMigration failed.");
    }
}