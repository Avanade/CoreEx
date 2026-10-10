var builder = DistributedApplication.CreateBuilder(args);

// dotnet dev-certs https --trust is not fully supported on Linux, so the ASP.NET Core dev cert used by each
// project resource's https endpoint is not OS-trusted on Linux CI runners. Both the health check probes (via
// AddEndpoints) and AspireTesterBase's CreateHttpClient() resolve their HttpClient via this same DI container's
// IHttpClientFactory, so disabling certificate validation here (dev/test-only AppHost, never shipped) covers both.
builder.DisableHttpCertificateValidation();

// IsRunMode: never surface this test-only resource in a published manifest (per the self-hosted WireMock.Net housekeeping note - see AppHost.cs's header comment).
var mockhost = builder.AddMockHostProject<Projects.Contoso_Aspire_MockHost>("mock-host");

// Compose owns the infrastructure lifecycle; visible connection-string resources expose graph dependencies.
// The secret parameters retain the existing ConnectionStrings configuration and are not graph nodes.
var postgres = builder.AddExternalConnectionString("Postgres").WithIconName("DatabaseMultiple");
var sqlServer = builder.AddExternalConnectionString("SqlServer").WithIconName("DatabaseMultiple");
var redis = builder.AddExternalConnectionString("redis").WithIconName("Database");
var serviceBus = builder.AddExternalConnectionString("ServiceBus").WithIconName("MailMultiple");
var cosmos = builder.AddExternalConnectionString("Cosmos", endpointKey: "AccountEndpoint").WithIconName("DatabaseMultiple");

// Customers domain.
var customersApi = builder.AddProject<Projects.Contoso_Customers_Api>("customers-api").WithReference(cosmos).WithReference(redis).AddEndpoints("/health/ready/detailed");
builder.AddProject<Projects.Contoso_Customers_Relay>("customers-relay").WithReference(cosmos).WithReference(serviceBus).AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();

// Products domain.
var productsApi = builder.AddProject<Projects.Contoso_Products_Api>("products-api").WithReference(postgres).WithReference(redis).AddEndpoints("/health/ready/detailed");
builder.AddProject<Projects.Contoso_Products_Relay>("products-relay").WithReference(postgres).WithReference(serviceBus).AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();
builder.AddProject<Projects.Contoso_Products_Subscribe>("products-subscribe").WithReference(postgres).WithReference(redis).WithReference(serviceBus).AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();

// Shopping domain.
// Note: shopping-api and shopping-subscribe call Products and Customers synchronously (see ProductsHttpClient and CustomersHttpClient). The appsettings default is the standalone
// "https://localhost:7200" (Products) / "https://localhost:7320" (Customers); here the logical "https+http://products-api" / "https+http://customers-api" is injected instead, and WithReference populates the
// "Services:products-api:*" / "Services:customers-api:*" configuration used by CoreEx's service-discovery-aware AddTypedHttpClient, so the resolved address
// always matches whichever endpoint/port the target actually binds to (regardless of launch profile), rather than a static guess.
builder.AddProject<Projects.Contoso_Shopping_Api>("shopping-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(serviceBus)
    .WithReference(productsApi)
    .WithEnvironment("ProductsApi__BaseAddress", "https+http://products-api")
    .WithReference(customersApi)
    .WithEnvironment("CustomersApi__BaseAddress", "https+http://customers-api")
    .AddEndpoints("/health/ready/detailed");

builder.AddProject<Projects.Contoso_Shopping_Relay>("shopping-relay").WithReference(sqlServer).WithReference(serviceBus).AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();

builder.AddProject<Projects.Contoso_Shopping_Subscribe>("shopping-subscribe")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithReference(serviceBus)
    .WithReference(productsApi)
    .WithEnvironment("ProductsApi__BaseAddress", "https+http://products-api")
    .WithReference(customersApi)
    .WithEnvironment("CustomersApi__BaseAddress", "https+http://customers-api")
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
