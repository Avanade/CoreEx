var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults().WithTracing(tracing => tracing.SetSampler(new AlwaysOnSampler()));
builder.AddHostSettings(solutionName: "CoreEx", domainName: "Functions");
builder.Services.AddExecutionContext();
builder.Services.AddMvc();
builder.Services.AddMvcWebApi((_, webApi) => webApi.ConvertUnhandledExceptionsToProblemDetails = true);
builder.Services.AddFusionCache();
builder.Services.AddDefaultCacheKeyProvider();
builder.Services.AddFusionHybridCache();
builder.Services.AddHybridCacheIdempotencyProvider();
builder.Services.AddSingleton<IdempotencyOperationService>();
builder.Services.AddInventoryImport(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new InventoryImportOptions(
        configuration["InventoryImportDatabase"] ?? throw new InvalidOperationException("InventoryImportDatabase must be configured."),
        configuration["InventoryImportReports"] ?? throw new InvalidOperationException("InventoryImportReports must be configured."),
        configuration["InventoryImportTenant"] ?? throw new InvalidOperationException("InventoryImportTenant must be configured."));
});
builder.Services.AddDynamicServicesUsing<InvocationService>();
builder.AddImportStorageClient();
builder.Services.AddKeyedSingleton<BlobContainerClient>(CatalogForwardingFunction.ReportContainerKey, (sp, _) =>
    sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(
        sp.GetRequiredService<IConfiguration>()[CatalogForwardingFunction.ReportContainerKey]
        ?? throw new InvalidOperationException("CatalogImportReports must be configured.")));
builder.AddAzureServiceBusClient("ServiceBus", settings =>
{
    if (settings.ConnectionString is null && settings.FullyQualifiedNamespace is null)
        settings.ConnectionString = builder.Configuration["ServiceBus"];
});
builder.Services.AddAzureServiceBusPublisher((_, publisher) => publisher.ContentMode = ContentMode.Binary);
builder.Services.AddEventFormatter();
builder.Services.AddSubscribedManager((_, manager) => manager.AddSubscribersUsing<MessageSubscriber>());
builder.Services.AddScoped<ServiceBusSubscribedSubscriber>();
builder.UseCoreExExecutionContext((context, ec, _) =>
{
    ec.Attributes["invocation-id"] = context.InvocationId;
    return Task.CompletedTask;
});
builder.Build().Run();
