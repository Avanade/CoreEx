namespace Contoso.Order.Workflow.Client;

internal static class DurableTaskConnectionStringFactory
{
    public static string Create(DurableTaskSchedulerOptions options)
    {
        var endpoint = options.Endpoint;
        var hostAddress = endpoint.Contains(';', StringComparison.Ordinal) ? endpoint.Split(';', StringSplitOptions.TrimEntries)[0] : endpoint;
            var taskHubName = string.IsNullOrWhiteSpace(options.TaskHub) ? "order" : options.TaskHub;
        var isLocalEmulator = hostAddress.StartsWith("http://localhost:8080", StringComparison.OrdinalIgnoreCase)
            || hostAddress.StartsWith("http://localhost:8081", StringComparison.OrdinalIgnoreCase);

        var authentication = isLocalEmulator ? "None" : "DefaultAzure";
        return $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication={authentication}";
    }
}