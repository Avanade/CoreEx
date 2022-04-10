using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.Text.Json;
using CoreEx.WebApis;

using My.Hr.Business;
using My.Hr.Business.Services;

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using CoreEx.Healthchecks;
using CoreEx.Messaging.Azure.ServiceBus;
using CoreEx.Messaging.Azure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Register the core services.
builder.Services
    .AddSingleton<HrSettings>()
    .AddExecutionContext()
    .AddScoped<SettingsBase, HrSettings>()
    .AddScoped<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>(_ => new CoreEx.Text.Json.JsonSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(), new ExceptionConverterFactory() }
    }))
    .AddScoped<IEventSerializer, CoreEx.Text.Json.EventDataSerializer>()
    .AddScoped<EventDataFormatter>()
    .AddScoped<IEventPublisher, EventPublisher>()
    .AddScoped<IEventSender, ServiceBusSender>()
    .AddAzureServiceBusClient(connectionName: nameof(HrSettings.ServiceBusConnection))
    .AddScoped<WebApi>();

// Register the business services.
builder.Services
    .AddScoped<ReferenceDataService>()
    .AddScoped<EmployeeService>();

// Database
builder.Services.AddDbContext<HrDbContext>(
    options => options.UseSqlServer("name=ConnectionStrings:Database"));

// Register the health checks.
builder.Services
    .AddScoped<HealthService>()
    .AddHealthChecks()
    .AddTypeActivatedCheck<AzureServiceBusQueueHealthCheck>("Health check for service bus verification queue", HealthStatus.Unhealthy, nameof(HrSettings.ServiceBusConnection), nameof(HrSettings.VerificationQueueName));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // using System.Reflection;
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
