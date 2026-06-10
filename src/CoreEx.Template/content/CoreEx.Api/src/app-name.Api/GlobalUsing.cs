global using CoreEx;
global using CoreEx.AspNetCore.Mvc;
// #if (refdata-enabled)
global using solution-name.Application;
// #endif
global using CoreEx.Caching;
global using CoreEx.Database;
// #if (implement-sqlserver)
global using CoreEx.Database.SqlServer;
// #elif (implement-postgres)
global using CoreEx.Database.Postgres;
// #endif
global using CoreEx.Entities;
global using CoreEx.Http;
global using CoreEx.Json;
// #if (refdata-enabled)
global using CoreEx.RefData;
// #endif
global using CoreEx.Validation;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Options;
global using NSwag.Annotations;
global using OpenTelemetry;
global using OpenTelemetry.Trace;
global using StackExchange.Redis;
global using System.Net;
global using System.Text.Json;
global using ZiggyCreatures.Caching.Fusion;
global using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
