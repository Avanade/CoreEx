namespace CoreEx.Azure.Functions.TestApp.CatalogForwarding;

/// <summary>Shares the worker SDK client registration with the composition test.</summary>
public static class ImportStorageRegistration
{
    /// <summary>Uses the native binding setting as a fallback for the worker client.</summary>
    public static void AddImportStorageClient(this IHostApplicationBuilder builder)
        => builder.AddAzureBlobServiceClient("ImportStorage", settings =>
        {
            if (settings.ConnectionString is null && settings.ServiceUri is null)
                settings.ConnectionString = builder.Configuration["ImportStorage"];
        });
}
