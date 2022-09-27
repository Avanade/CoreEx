using System.Threading.Tasks;
using Pulumi;
using AD = Pulumi.AzureAD;

namespace Company.AppName.Infra;

public class StackConfiguration
{
    public Input<string>? SqlAdAdminLogin { get; private set; }
    public Input<string>? SqlAdAdminPassword { get; private set; }
    public bool IsAppsDeploymentEnabled { get; private set; }
    public bool IsDBSchemaDeploymentEnabled { get; private set; }
    public string PendingVerificationsQueue { get; private set; } = default!;
    public string VerificationResultsQueue { get; private set; } = default!;
    public string MassPublishQueue { get; private set; } = default!;

    /// <summary>
    /// Emails for developer team, that will be added to Developers AD group.
    /// </summary>
    public string DeveloperEmails { get; private set; } = default!;

    private StackConfiguration() { }

    public static async Task<StackConfiguration> CreateConfiguration()
    {
        // read stack config
        var config = new Config();

        // get some info from Azure AD
        var domainResult = await AD.GetDomains.InvokeAsync(new AD.GetDomainsArgs { OnlyDefault = true });
        var defaultUsername = $"sqlGlobalAdAdmin{Pulumi.Deployment.Instance.StackName}@{domainResult.Domains[0].DomainName}";
        var defaultPassword = new Pulumi.Random.RandomPassword("sqlAdPassword", new()
        {
            Length = 32,
            Upper = true,
            Number = true,
            Special = true,
            OverrideSpecial = "@",
            MinLower = 2,
            MinUpper = 2,
            MinSpecial = 2,
            MinNumeric = 2
        }).Result;

        Log.Info($"Default username is: {defaultUsername}");

        return new StackConfiguration
        {
            SqlAdAdminLogin = Extensions.GetConfigValue("sqlAdAdmin", defaultUsername),
            SqlAdAdminPassword = Extensions.GetConfigValue("sqlAdPassword", defaultPassword),
            IsAppsDeploymentEnabled = config.GetBoolean("isAppsDeploymentEnabled") ?? false,
            IsDBSchemaDeploymentEnabled = config.GetBoolean("isDBSchemaDeploymentEnabled") ?? false,

            PendingVerificationsQueue = config.Get("pendingVerificationsQueue") ?? "pendingVerifications",
            VerificationResultsQueue = config.Get("verificationResultsQueue") ?? "verificationResults",
            MassPublishQueue = config.Get("massPublishQueue") ?? "massPublish",

            DeveloperEmails = config.Get("developerEmails")
        };
    }
}