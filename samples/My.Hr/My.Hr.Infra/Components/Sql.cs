using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Pulumi;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Sql.Inputs;
using AD = Pulumi.AzureAD;
using Deployment = Pulumi.Deployment;

namespace CoreEx.Infra.Components;

public class Sql : ComponentResource
{
    private const string SqlSchemaUsername = "dbEx";
    private readonly SqlArgs args;
    private readonly HashSet<string> firewallAllowedIps = new();

    public Output<string> SqlDatabaseConnectionString { get; }
    public Output<string> SqlServerName { get; }
    public Output<string> SqlDatabaseAuthorizedGroupId { get; }

    public Sql(string name, SqlArgs args, ComponentResourceOptions? options = null)
         : base("coreexinfra:web:sql", name, options)
    {
        this.args = args;

        Pulumi.Log.Info("password =" + args.SqlAdAdminPassword.GetValue().Result);
        var sqlAdAdmin = new AD.User("sqlAdmin", new AD.UserArgs
        {
            UserPrincipalName = args.SqlAdAdminLogin,
            Password = args.SqlAdAdminPassword,
            DisplayName = "Global SQL Admin"
        }, new CustomResourceOptions { Parent = this });

        var sqlServer = new Server($"sql-server-{Deployment.Instance.StackName}", new ServerArgs
        {
            ResourceGroupName = args.ResourceGroupName,
            Administrators = new ServerExternalAdministratorArgs
            {
                Login = sqlAdAdmin.UserPrincipalName,
                Sid = sqlAdAdmin.Id,
                // disabling OnlyADAuthentication, because MyHr.Database -> DbEx -> DBUp doesn't support Active Directory Authentication
                AzureADOnlyAuthentication = true,
                AdministratorType = AdministratorType.ActiveDirectory,
                PrincipalType = PrincipalType.User,
            },
            MinimalTlsVersion = "1.2",
            Tags = args.Tags
        }, new CustomResourceOptions { Parent = this });

        var publicIp = Output.Create(new HttpClient().GetStringAsync("https://api.ipify.org"));

        var enableLocalMachine = new FirewallRule("AllowLocalMachine", new FirewallRuleArgs
        {
            ResourceGroupName = args.ResourceGroupName,
            ServerName = sqlServer.Name,
            StartIpAddress = publicIp,
            EndIpAddress = publicIp
        }, new CustomResourceOptions { Parent = this });

        var database = new Pulumi.AzureNative.Sql.Database("sqldb", new DatabaseArgs
        {
            ResourceGroupName = args.ResourceGroupName,
            ServerName = sqlServer.Name,
            DatabaseName = "CoreExDB",
            Sku = new SkuArgs
            {
                Name = "Basic"
            },
            Tags = args.Tags
        }, new CustomResourceOptions { Parent = this });

        const string sqlDatabaseAuthorizedGroupName = "SqlDbUsersGroup";
        var sqlDatabaseAuthorizedGroup = new AD.Group(sqlDatabaseAuthorizedGroupName, new AD.GroupArgs
        {
            DisplayName = sqlDatabaseAuthorizedGroupName,
            SecurityEnabled = true,
            Owners = new InputList<string> { sqlAdAdmin.Id }
        }, new CustomResourceOptions { Parent = this });

        var sqlSchemaUserPassword = new Pulumi.Random.RandomPassword("sqlUserPassword", new()
        {
            Length = 16,
            Special = true,
            OverrideSpecial = "@",
        }, new CustomResourceOptions { Parent = this });

        var sqlADConnectionString = Output.Format($"Server={sqlServer.Name}.database.windows.net; Authentication=Active Directory Password; User={args.SqlAdAdminLogin}; Password={args.SqlAdAdminPassword}; Database={database.Name}");
        var sqlPasswordConnectionString = Output.Format($"Server={sqlServer.Name}.database.windows.net; User Id={SqlSchemaUsername}; Password={sqlSchemaUserPassword}; Database={database.Name}");

        // login with AD admin credentials to give access to AD group that contains App and Function managed identity users
        ProvisionUsers(sqlADConnectionString!, sqlDatabaseAuthorizedGroupName);

        // create SQL user and setup schema
        Output.Tuple(args.IsAppsDeploymentEnabled.ToOutput(), sqlSchemaUserPassword.Result, sqlADConnectionString, sqlPasswordConnectionString).Apply(t =>
        {
            var (isAppsDeploymentEnabled, sqlSchemaUserPassword, sqlADConnectionString, sqlPasswordConnectionString) = t;

            if (isAppsDeploymentEnabled)
            {
                // first provision SQL user so DbEx can execute
                ProvisionSqlUser(sqlADConnectionString, SqlSchemaUsername, sqlSchemaUserPassword!);
                return DeployDbSchemaAsync(sqlPasswordConnectionString);
            }

            return Task.FromResult(0);
        });

        // https://docs.microsoft.com/en-us/sql/connect/ado-net/sql/azure-active-directory-authentication?view=sql-server-ver15#using-active-directory-default-authentication
        SqlDatabaseConnectionString = Output.Format($"Server={sqlServer.Name}.database.windows.net; Authentication=Active Directory Default; Database={database.Name}");
        SqlServerName = sqlServer.Name;
        SqlDatabaseAuthorizedGroupId = sqlDatabaseAuthorizedGroup.Id;

        RegisterOutputs();
    }

    public void AddFirewallRule(Output<string> ips, string name)
    {
        ips.Apply(ips =>
        {
            foreach (var address in ips.Split(","))
            {
                if (!firewallAllowedIps.Contains(address))
                {
                    firewallAllowedIps.Add(address);

                    var enableIp = new FirewallRule("Enable_" + name + "_" + address, new FirewallRuleArgs
                    {
                        ResourceGroupName = args.ResourceGroupName,
                        ServerName = SqlServerName,
                        StartIpAddress = address,
                        EndIpAddress = address
                    }, new CustomResourceOptions { Parent = this });
                }
            }

            return true;
        });
    }

    public void AddToSqlDatabaseAuthorizedGroup(string name, Output<string> principalId)
    {
        var appGroupMember = new AD.GroupMember(name, new()
        {
            GroupObjectId = SqlDatabaseAuthorizedGroupId,
            MemberObjectId = principalId,
        }, new CustomResourceOptions { Parent = this });
    }

    private static void ProvisionUsers(Input<string> connectionString, string groupName)
    {
        if (Deployment.Instance.IsDryRun || Deployment.Instance.ProjectName == "unittest")
            // skip in dry run and in unit tests
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

        connectionString.Apply(cs =>
        {
            using SqlConnection conn = new(cs);
            conn.Open();

            var result = conn.Execute(commandText);
            return true;
        });
    }

    private static void ProvisionSqlUser(string connectionString, string sqlUsername, string sqlUserPassword)
    {
        if (Deployment.Instance.IsDryRun || Deployment.Instance.ProjectName == "unittest")
            // skip in dry run
            return;

        Log.Info($"Provisioning user {sqlUsername} in SQL DB using {connectionString}");
        string commandText = @$"
        IF NOT EXISTS (SELECT [name]
                FROM [sys].[database_principals]
                WHERE [type] = N'S' AND [name] = N'{sqlUsername}')
        BEGIN
            CREATE USER [{sqlUsername}] WITH PASSWORD = N'{sqlUserPassword}';
        END
        
        ALTER ROLE db_datareader ADD MEMBER {sqlUsername}; 
        ALTER ROLE db_datawriter ADD MEMBER {sqlUsername};
        ";

        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        var result = conn.Execute(commandText);
    }

    private static Task<int> DeployDbSchemaAsync(string connectionString)
    {
        if (Deployment.Instance.IsDryRun || Deployment.Instance.ProjectName == "unittest")
            // skip in dry run
            return Task.FromResult(0);

        Log.Info($"Deploying DB schema using {connectionString}");
        return My.Hr.Database.Program.RunMigrator(connectionString, assembly: typeof(My.Hr.Database.Program).Assembly, "DeployWithData");
    }

    public class SqlArgs
    {
        public Input<string> ResourceGroupName { get; set; } = default!;
        public InputMap<string> Tags { get; set; } = default!;
        public Input<string> SqlAdAdminLogin { get; set; } = default!;
        public Input<string> SqlAdAdminPassword { get; set; } = default!;
        public Input<bool> IsAppsDeploymentEnabled { get; set; } = default!;
    }
}