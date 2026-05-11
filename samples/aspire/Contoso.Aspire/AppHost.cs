var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Contoso_Products_Api>("products-api").AddEndpoints("/health/ready/detailed");
builder.AddProject<Projects.Contoso_Products_Outbox_Relay>("products-outbox-relay").AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();
builder.AddProject<Projects.Contoso_Products_Subscribe>("products-subscribe").AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();

builder.AddProject<Projects.Contoso_Shopping_Api>("shopping-api").AddEndpoints("/health/ready/detailed");
builder.AddProject<Projects.Contoso_Shopping_Outbox_Relay>("shopping-outbox-relay").AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();
builder.AddProject<Projects.Contoso_Shopping_Subscribe>("shopping-subscribe").AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();

var orderWorkflowWorker = builder.AddProject<Projects.Contoso_Order_Workflow_Worker>("order-workflow-worker").AddEndpoints("/health");

builder.AddProject<Projects.Contoso_Orders_Api>("orders-api")
    .WaitFor(orderWorkflowWorker)
    .AddEndpoints("/health/ready/detailed");

builder.Build().Run();


internal static class Extensions
{
    public static IResourceBuilder<ProjectResource> AddEndpoints(this IResourceBuilder<ProjectResource> builder, params string[] urls)
    {
        var httpEndpoint = builder.GetEndpoint("http");
        foreach (var url in urls)
        {
            builder.WithAnnotation(new ResourceUrlAnnotation { Endpoint = httpEndpoint, Url = url });
        }

        return builder;
    }

    // Icons: https://storybooks.fluentui.dev/react/?path=/docs/icons-catalog--docs
    public static IResourceBuilder<ProjectResource> AddCommand(this IResourceBuilder<ProjectResource> builder, HttpMethod method, string path, string displayName, string? iconName)
        => builder.WithHttpCommand(
            path: path,
            displayName: displayName,
            commandOptions: new HttpCommandOptions()
            {
                Method = method,
                IconName = iconName
            });

    public static IResourceBuilder<ProjectResource> AddHostedServiceSupport(this IResourceBuilder<ProjectResource> builder)
        => builder.AddEndpoints("/hosted-services/all/status")
            .AddCommand(HttpMethod.Post, "/hosted-services/all/pause", "Pause all services", "Pause")
            .AddCommand(HttpMethod.Post, "/hosted-services/all/resume", "Resume all services", "PauseOff");
}