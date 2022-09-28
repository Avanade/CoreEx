using System;
using Pulumi;
using AD = Pulumi.AzureAD;

namespace Company.AppName.Infra.Components;

public class DevSetup : ComponentResource
{
    public string DevelopersGroupName { get; } = $"Developers-Company-AppName-{Deployment.Instance.StackName}";

    public Output<string> DevelopersGroupId { get; private set; } = default!;

    public DevSetup(string name, string emailsCommaSeparated, ComponentResourceOptions? options = null)
      : base("Company:AppName:developer:setup", name, options)
    {
        // get current user
        var current = Output.Create(AD.GetClientConfig.InvokeAsync());

        var developersAuthorizedGroup = new AD.Group(DevelopersGroupName, new AD.GroupArgs
        {
            DisplayName = DevelopersGroupName,
            SecurityEnabled = true,
            Owners = new InputList<string> { current.Apply(current => current.ObjectId) },
            Members = new InputList<string> { current.Apply(current => current.ObjectId) }
        }, new CustomResourceOptions { Parent = this });

        DevelopersGroupId = developersAuthorizedGroup.Id;

        Log.Info("Provisioning access for developers: " + emailsCommaSeparated);
        var emails = emailsCommaSeparated.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var email in emails)
        {
            var user = AD.GetUser.Invoke(new()
            {
                UserPrincipalName = email,
            }, new InvokeOptions { Parent = this });

            var groupMember = new AD.GroupMember($"developerGroupMember{Deployment.Instance.StackName}-{email}", new()
            {
                GroupObjectId = DevelopersGroupId,
                MemberObjectId = user.Apply(usr => usr.Id),
            }, new CustomResourceOptions { Parent = this });
        }

        RegisterOutputs();
    }
}