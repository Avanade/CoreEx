using CoreEx.Configuration;
using CoreEx.DependencyInjection;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.WebApis;
using System.Reflection;

using My.Hr.Business;
using My.Hr.Business.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using CoreEx.Text.Json;

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
    .AddScoped<IEventPublisher, NullEventPublisher>()
    .AddScoped<WebApi, WebApi>();

// Register the business services.
builder.Services
    .AddScoped<ReferenceDataService>()
    .AddScoped<EmployeeService>();

// Database
builder.Services.AddDbContext<HrDbContext>(
    options => options.UseSqlServer("name=ConnectionStrings:Database"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
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
