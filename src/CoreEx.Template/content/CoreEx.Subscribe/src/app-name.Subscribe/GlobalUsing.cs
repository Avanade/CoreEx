global using CoreEx;
global using CoreEx.AspNetCore.Mvc;
// #if implement-servicebus
global using CoreEx.Azure.Messaging.ServiceBus;
// #endif
global using CoreEx.Caching;
// #if has-data-provider
global using CoreEx.Database;
// #endif
// #if implement-sqlserver
global using CoreEx.Database.SqlServer;
// #elif implement-postgres
global using CoreEx.Database.Postgres;
// #endif
global using CoreEx.DependencyInjection;
global using CoreEx.Entities;
global using CoreEx.Events;
global using CoreEx.Events.Subscribing;
global using CoreEx.Http;
global using CoreEx.Json;
global using CoreEx.Results;
// #if refdata-enabled
global using CoreEx.RefData;
// #endif
global using CoreEx.Validation;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Options;
global using OpenTelemetry;
global using OpenTelemetry.Trace;
global using StackExchange.Redis;
global using System.Net;
global using System.Text.Json;
global using ZiggyCreatures.Caching.Fusion;
global using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;