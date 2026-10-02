using Microsoft.Extensions.Hosting;
using System.Net;

var builder = DistributedApplication.CreateBuilder(args);

// dotnet dev-certs https --trust is not fully supported on Linux, so the ASP.NET Core dev cert used by each
// project resource's https endpoint is not OS-trusted on Linux CI runners. Both the health check probes (via
// AddEndpoints) and AspireTesterBase's CreateHttpClient() resolve their HttpClient via this same DI container's
// IHttpClientFactory, so disabling certificate validation here (dev/test-only AppHost, never shipped) covers both.
builder.DisableHttpCertificateValidation();

// IsRunMode: never surface this test-only resource in a published manifest (per the self-hosted WireMock.Net housekeeping note - see AppHost.cs's header comment).
var mockhost = builder.AddMockHostProject<Projects.Contoso_Aspire_MockHost>("mock-host");

// External infrastructure (SQL Server, Postgres, Redis, Service Bus emulator) runs via docker-compose.yml, not
// Aspire orchestration. These are modelled as connection-string resources - matching the connection name each host
// passes to its own Aspire client-integration package (e.g. AddAzureNpgsqlDataSource("Postgres")) - purely so the
// dashboard graph reflects the real dependencies. Aspire does not start/stop/health-check these resources; each
// host's client-integration package already wires up its own OTLP telemetry and health checks regardless.
// Icon names match what Aspire's own AddPostgres/AddSqlServer/AddRedis/AddAzureServiceBus hosting integrations
// assign to the equivalent managed resource, so these look identical to the "real" ones in the dashboard.
var postgres = builder.AddConnectionString("Postgres").WithIconName("DatabaseMultiple");
var sqlServer = builder.AddConnectionString("SqlServer").WithIconName("DatabaseMultiple");
var redis = builder.AddConnectionString("redis").WithIconName("Database");
var serviceBus = builder.AddConnectionString("ServiceBus").WithIconName("MailMultiple");

// Products domain.
var productsApi = builder.AddProject<Projects.Contoso_Products_Api>("products-api").WithReference(postgres).WithReference(redis).AddEndpoints("/health/ready/detailed");
builder.AddProject<Projects.Contoso_Products_Relay>("products-relay").WithReference(postgres).WithReference(serviceBus).AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();
builder.AddProject<Projects.Contoso_Products_Subscribe>("products-subscribe").WithReference(postgres).WithReference(redis).WithReference(serviceBus).AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();

// Shopping domain.
// Note: shopping-api and shopping-subscribe call Products synchronously (see ProductsHttpClient) - WithReference populates the
// "Services:products-api:*" configuration used by CoreEx's service-discovery-aware AddTypedHttpClient, so the resolved address
// always matches whichever endpoint/port products-api actually binds to (regardless of launch profile), rather than a static guess.
builder.AddProject<Projects.Contoso_Shopping_Api>("shopping-api").WithReference(sqlServer).WithReference(redis).WithReference(serviceBus).WithReference(productsApi).AddEndpoints("/health/ready/detailed");
builder.AddProject<Projects.Contoso_Shopping_Relay>("shopping-relay").WithReference(sqlServer).WithReference(serviceBus).AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();

builder.AddProject<Projects.Contoso_Shopping_Subscribe>("shopping-subscribe")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(serviceBus)
    .WithReference(productsApi)
    .AddEndpoints("/health/ready/detailed")
    .AddHostedServiceSupport()
    .WithMockHostEnvironment("SendGrid__BaseAddress", mockhost, "http");

// Orders domain.
var orderWorkflowWorker = builder.AddProject<Projects.Contoso_Order_Workflow_Worker>("order-workflow-worker").AddEndpoints("/health").WithUrlForEndpoint("https", ep => { ep.Url = "http://localhost:8082"; ep.DisplayText = "DTS Dashboard"; });
builder.AddProject<Projects.Contoso_Orders_Api>("orders-api").WithReference(sqlServer).WithReference(redis).WaitFor(orderWorkflowWorker).AddEndpoints("/health/ready/detailed");

var app = builder.Build();
await app.StartAsync();

await app.HttpMock("mock-host", "http")
    .Request(HttpMethod.Post, "v3/mail/send").WithAnyBody().Respond.WithAsync(HttpStatusCode.Accepted);

await app.WaitForShutdownAsync();
