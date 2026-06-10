global using CoreEx;
global using CoreEx.AspNetCore.Mvc;
global using CoreEx.Caching;
global using CoreEx.Database;
// #if implement-sqlserver
global using CoreEx.Database.SqlServer;
// #elif implement-postgres
global using CoreEx.Database.Postgres;
// #endif
// #if implement-servicebus
global using CoreEx.Azure.Messaging.ServiceBus;
// #endif
global using CoreEx.Http;
global using CoreEx.Json;
global using CoreEx.Validation;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Options;
global using OpenTelemetry;
global using OpenTelemetry.Trace;
global using System.Net;
global using System.Text.Json;
