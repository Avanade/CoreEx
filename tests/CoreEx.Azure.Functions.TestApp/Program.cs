var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults().WithTracing(tracing => tracing.SetSampler(new AlwaysOnSampler()));
builder.AddHostSettings(solutionName: "CoreEx", domainName: "Functions");
builder.Services.AddExecutionContext();
builder.Services.AddMvc();
builder.Services.AddMvcWebApi((_, webApi) => webApi.ConvertUnhandledExceptionsToProblemDetails = true);
builder.Services.AddDynamicServicesUsing<InvocationService>();
builder.Services.AddSingleton(sp => new BlobServiceClient(sp.GetRequiredService<IConfiguration>()["ImportStorage"]
    ?? throw new InvalidOperationException("ImportStorage must be configured for Blob processing.")));
builder.Services.AddEventFormatter();
builder.Services.AddSubscribedManager((_, manager) => manager.AddSubscribersUsing<MessageSubscriber>());
builder.Services.AddScoped<ServiceBusSubscribedSubscriber>();
builder.UseCoreExExecutionContext((context, ec, _) =>
{
    ec.Attributes["invocation-id"] = context.InvocationId;
    return Task.CompletedTask;
});
builder.Build().Run();
