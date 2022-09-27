using System.Collections.Generic;
using My.Hr.Infra.Services;
using Pulumi;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Sql.Inputs;
using AD = Pulumi.AzureAD;
using Deployment = Pulumi.Deployment;

namespace My.Hr.Infra.Components;

public class Sql : ComponentResource
{
    private readonly SqlArgs args;
    private readonly HashSet<string> firewallAllowedIps = new();

    public Output<string> SqlDatabaseConnectionString { get; }
    public Output<string> SqlServerName { get; }
    public Output<string> SqlDatabaseAuthorizedGroupId { get; }

    public Sql(string name, SqlArgs args, IDbOperations dbOperations, AzureApiClient apiClient, ComponentResourceOptions? options = null)
         : base("coreexinfra:web:sql", name, options)
    {
        this.args = args;
        var sqlAdAdmin = new AD.User("sqlAdmin", new AD.UserArgs
        {
            UserPrincipalName = args.SqlAdAdminLogin,
            Password = args.SqlAdAdminPassword,
            DisplayName = $"Global SQL Admin {Deployment.Instance.StackName}"
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

        var publicIp = Output.Create(apiClient.GetMyIP());

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

        string sqlDatabaseAuthorizedGroupName = $"SqlDbUsersGroup{Deployment.Instance.StackName}";
        var sqlDatabaseAuthorizedGroup = new AD.Group(sqlDatabaseAuthorizedGroupName, new AD.GroupArgs
        {
            DisplayName = sqlDatabaseAuthorizedGroupName,
            SecurityEnabled = true,
            Owners = new InputList<string> { sqlAdAdmin.Id }
        }, new CustomResourceOptions { Parent = this });

        var sqlADConnectionString = Output.Format($"Server={sqlServer.Name}.database.windows.net; Authentication=Active Directory Password; User={args.SqlAdAdminLogin}; Password={args.SqlAdAdminPassword}; Database={database.Name}");

        // login with AD admin credentials to give access to AD group that contains App and Function managed identity users
        dbOperations.ProvisionUsers(sqlADConnectionString!, sqlDatabaseAuthorizedGroupName);

        // create SQL user and setup schema
        if (args.IsDBSchemaDeploymentEnabled)
        {
            sqlADConnectionString.Apply(async cs =>
            {
                return await dbOperations.DeployDbSchemaAsync(cs);
            });
        }

        // https://docs.microsoft.com/en-us/sql/connect/ado-net/sql/azure-active-directory-authentication?view=sql-server-ver15#using-active-directory-default-authentication
        SqlDatabaseConnectionString = Output.Format($"Server={sqlServer.Name}.database.windows.net; Authentication=Active Directory Default; Database={database.Name}");
        SqlServerName = sqlServer.Name;
        SqlDatabaseAuthorizedGroupId = sqlDatabaseAuthorizedGroup.Id;

        RegisterOutputs();
    }

    public void AddFirewallRule(Output<string> ips, string name)
    {
        // this shows warning in Pulumi, but there's no other way to create this resource without doing it in Apply
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

    public class SqlArgs
    {
        public Input<string> ResourceGroupName { get; set; } = default!;
        public InputMap<string> Tags { get; set; } = default!;
        public Input<string> SqlAdAdminLogin { get; set; } = default!;
        public Input<string> SqlAdAdminPassword { get; set; } = default!;
        public bool IsDBSchemaDeploymentEnabled { get; set; }
    }
}