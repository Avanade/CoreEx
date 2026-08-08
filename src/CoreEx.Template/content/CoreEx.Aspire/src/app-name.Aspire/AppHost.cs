var builder = DistributedApplication.CreateBuilder(args);

// domain-name domain.
// #if has-api
builder.AddProject<Projects.solution-name-underscore_Api>("domain-name-lower-api").AddEndpoints("/health/ready/detailed");
// #endif
// #if has-relay
builder.AddProject<Projects.solution-name-underscore_Relay>("domain-name-lower-relay").AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();
// #endif
// #if has-subscribe
builder.AddProject<Projects.solution-name-underscore_Subscribe>("domain-name-lower-subscribe").AddEndpoints("/health/ready/detailed").AddHostedServiceSupport();
// #endif

builder.Build().Run();
