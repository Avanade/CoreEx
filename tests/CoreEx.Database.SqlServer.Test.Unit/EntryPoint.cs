using CoreEx.Database.SqlServer.Test.Unit.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreEx.Database.SqlServer.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder) 
    {
        builder.Services.AddExecutionContext(_ => new ExecutionContext { TenantId = "A" });

        builder.AddSqlServerClient("SqlServer");
        builder.Services.AddSqlServerDatabase();
        builder.Services.AddSqlServerUnitOfWork();

        builder.Services.AddDbContext<TestDbContext>();
        builder.Services.AddEfDb<TestEfDb>();
    }
}