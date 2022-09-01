using System.Collections.Generic;
using System.Net.Http;
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
    private readonly SqlArgs args;
    private readonly HashSet<string> firewallAllowedIps = new();

    public Output<string> SqlDatabaseConnectionString { get; }
    public Output<string> SqlServerName { get; }
    public Output<string> SqlDatabaseAuthorizedGroupId { get; }

    public Sql(string name, SqlArgs args, ComponentResourceOptions? options = null)
         : base("coreexinfra:web:sql", name, options)
    {
        this.args = args;

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

        var database = new Database("sqldb", new DatabaseArgs
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

        // login with admin credentials to provision users
        Output.Format($"Server={sqlServer.Name}.database.windows.net; User={args.SqlAdAdminLogin}; Authentication=Active Directory Password; Password={args.SqlAdAdminPassword}; Database={database.Name}").Apply(cs =>
        {
            ProvisionUsers(cs, sqlDatabaseAuthorizedGroupName);
            return true;
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

    private static void ProvisionUsers(string connectionString, string groupName)
    {
        if (Deployment.Instance.IsDryRun || Deployment.Instance.ProjectName == "unittest")
            // skip in dry run
            return;

        System.Console.WriteLine("Provisioning user {0} in SQL DB", groupName);
        string commandText = @$"
        IF NOT EXISTS (SELECT [name]
                FROM [sys].[database_principals]
                WHERE [type] = N'X' AND [name] = N'{groupName}')
        BEGIN
            CREATE USER {groupName} FROM EXTERNAL PROVIDER; 
        END
       
        ALTER ROLE db_datareader ADD MEMBER {groupName}; 
        ALTER ROLE db_datawriter ADD MEMBER {groupName};";

        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        var result = conn.Execute(commandText);
    }

    public class SqlArgs
    {
        public Input<string> ResourceGroupName { get; set; } = default!;
        public InputMap<string> Tags { get; set; } = default!;
        public Input<string> SqlAdAdminLogin { get; internal set; } = default!;
        public Input<string> SqlAdAdminPassword { get; internal set; } = default!;
    }
}