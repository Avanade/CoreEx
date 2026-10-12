var builder = DistributedApplication.CreateBuilder(args);

// dotnet dev-certs https --trust is not fully supported on Linux, so the ASP.NET Core dev cert used by each
// project resource's https endpoint is not OS-trusted on Linux CI runners. Both the health check probes (via
// AddEndpoints) and AspireTesterBase's CreateHttpClient() resolve their HttpClient via this same DI container's
// IHttpClientFactory, so disabling certificate validation here (dev/test-only AppHost, never shipped) covers both.
builder.DisableHttpCertificateValidation();

// IsRunMode: never surface this test-only resource in a published manifest (per the self-hosted WireMock.Net housekeeping note - see AppHost.cs's header comment).
//var mockhost = builder.AddMockHostProject<Projects.solution-name-underscore_Aspire_MockHost>("mock-host");

// Compose owns the infrastructure lifecycle; visible connection-string resources expose graph dependencies.
// Secret parameters retain the existing ConnectionStrings configuration and are not graph nodes.
// #if implement-postgres
var db = builder.AddExternalConnectionString("Postgres").WithIconName("DatabaseMultiple");
// #elif implement-cosmos
var db = builder.AddExternalConnectionString("Cosmos", endpointKey: "AccountEndpoint").WithIconName("DatabaseMultiple");
// #elif implement-sqlserver
var db = builder.AddExternalConnectionString("SqlServer").WithIconName("DatabaseMultiple");
// #endif
var redis = builder.AddExternalConnectionString("redis").WithIconName("Database");
// #if implement-servicebus
var serviceBus = builder.AddExternalConnectionString("ServiceBus").WithIconName("MailMultiple");
// #endif
// domain-name domain.
// #if has-api

builder.AddProject<Projects.solution-name-underscore_Api>("domain-name-lower-api")
// #if has-data-provider
    .WithReference(db)
// #endif
    .WithReference(redis)
    .AddEndpoints("/health/ready/detailed");
// #endif
// #if has-relay

builder.AddProject<Projects.solution-name-underscore_Relay>("domain-name-lower-relay")
// #if has-data-provider
    .WithReference(db)
// #endif
// #if implement-servicebus
    .WithReference(serviceBus)
// #endif
    .AddEndpoints("/health/ready/detailed")
    .AddHostedServiceSupport();
// #endif
// #if has-subscribe

builder.AddProject<Projects.solution-name-underscore_Subscribe>("domain-name-lower-subscribe")
// #if has-data-provider
    .WithReference(db)
// #endif
    .WithReference(redis)
// #if implement-servicebus
    .WithReference(serviceBus)
// #endif
    .AddEndpoints("/health/ready/detailed")
    .AddHostedServiceSupport();
// #endif

builder.Build().Run();
