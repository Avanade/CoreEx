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
builder.AddAzureBlobServiceClient("ImportStorage", settings =>
{
    if (settings.ConnectionString is null && settings.ServiceUri is null)
        settings.ConnectionString = builder.Configuration["ImportStorage"];
});
builder.Services.AddEventFormatter();
builder.Services.AddSubscribedManager((_, manager) => manager.AddSubscribersUsing<MessageSubscriber>());
builder.Services.AddScoped<ServiceBusSubscribedSubscriber>();
builder.UseCoreExExecutionContext((context, ec, _) =>
{
    ec.Attributes["invocation-id"] = context.InvocationId;
    return Task.CompletedTask;
});
builder.Build().Run();
