using System.Threading.Tasks;
using Dapper;
using DbEx;
using DbEx.Migration;
using DbEx.SqlServer.Migration;
using Microsoft.Data.SqlClient;
using Pulumi;

namespace My.Hr.Infra.Services;

public class DbOperations : IDbOperations
{
    public void ProvisionUsers(Input<string> connectionString, string groupName)
    {
        if (Deployment.Instance.IsDryRun)
            // skip in dry run
            return;

        Log.Info($"Provisioning user {groupName} in SQL DB");
        string commandText = @$"
        IF NOT EXISTS (SELECT [name]
                FROM [sys].[database_principals]
                WHERE [type] = N'X' AND [name] = N'{groupName}')
        BEGIN
            CREATE USER {groupName} FROM EXTERNAL PROVIDER; 
        END
       
        ALTER ROLE db_datareader ADD MEMBER {groupName}; 
        ALTER ROLE db_datawriter ADD MEMBER {groupName};
        ";

        connectionString.Apply(async cs =>
        {
            using SqlConnection conn = new(cs);
            await conn.OpenAsync();

            var result = await conn.ExecuteAsync(commandText);
            return true;
        });
    }

    public Task<bool> DeployDbSchemaAsync(string connectionString)
    {
        if (Deployment.Instance.IsDryRun)
            // skip in dry run
            return Task.FromResult(true);

        Log.Info($"Deploying DB schema using {connectionString}");
        var args = Database.Program.ConfigureMigrationArgs(new MigrationArgs(MigrationCommand.DeployWithData, connectionString));
        return new SqlServerMigration(args).MigrateAsync();
    }
}