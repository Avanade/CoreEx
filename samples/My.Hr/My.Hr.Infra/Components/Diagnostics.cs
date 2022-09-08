using Pulumi;
using Pulumi.AzureNative.Insights.V20200202;
using Pulumi.AzureNative.OperationalInsights;

namespace My.Hr.Infra.Components;

public class Diagnostics : ComponentResource
{
    public Output<string> InstrumentationKey { get; } = default!;

    public Diagnostics(string name, DiagnosticsArgs args, ComponentResourceOptions? options = null)
         : base("coreexinfra:web:diagnostics", name, options)
    {
        // Log Analytics Workspace
        var workspace = new Workspace("workspace", new()
        {
            ResourceGroupName = args.ResourceGroupName,
            RetentionInDays = 30,
            Sku = new Pulumi.AzureNative.OperationalInsights.Inputs.WorkspaceSkuArgs
            {
                Name = "PerGB2018",
            },
            Tags = args.Tags,
            WorkspaceName = "lw-workspace",
        }, new CustomResourceOptions { Parent = this });

        // Application insights
        var appInsights = new Component("appInsights", new ComponentArgs
        {
            ApplicationType = ApplicationType.Web,
            Kind = "web",
            ResourceGroupName = args.ResourceGroupName,
            WorkspaceResourceId = workspace.Id,
            Tags = args.Tags
        }, new CustomResourceOptions { Parent = this });

        InstrumentationKey = appInsights.InstrumentationKey;
        RegisterOutputs();
    }

    public class DiagnosticsArgs
    {
        public Input<string> ResourceGroupName { get; set; } = default!;
        public InputMap<string> Tags { get; set; } = default!;
    }
}