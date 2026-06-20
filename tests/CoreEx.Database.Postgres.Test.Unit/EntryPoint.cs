using CoreEx.Database.Postgres.Test.Unit.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreEx.Database.Postgres.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder) 
    {
        builder.Services.AddPrecisionTimeProvider();
        builder.Services.AddExecutionContext(_ => new ExecutionContext { TenantId = "A" });

        builder.AddNpgsqlDataSource("PostgreSQL");
        builder.Services.AddPostgresDatabase();
        builder.Services.AddPostgresUnitOfWork();

        builder.Services.AddDbContext<TestDbContext>();
        builder.Services.AddEfDb<TestEfDb>();
    }
}