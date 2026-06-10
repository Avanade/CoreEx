global using CoreEx;
global using CoreEx.Data;
global using CoreEx.Data.Models;
// #if implement-sqlserver || implement-postgres
global using CoreEx.Database;
// #if implement-sqlserver
global using CoreEx.Database.SqlServer;
// #if outbox-enabled
global using CoreEx.Database.SqlServer.Outbox;
// #endif
// #elif implement-postgres
global using CoreEx.Database.Postgres;
// #if outbox-enabled
global using CoreEx.Database.Postgres.Outbox;
// #endif
// #endif
// #endif
global using CoreEx.DependencyInjection;
// #if domain-driven-enabled
global using CoreEx.DomainDriven;
// #endif
global using CoreEx.Entities;
// #if implement-sqlserver || implement-postgres
global using CoreEx.EntityFrameworkCore;
global using CoreEx.EntityFrameworkCore.Converters;
// #endif
global using CoreEx.Json;
global using CoreEx.Mapping;
// #if refdata-enabled
global using CoreEx.RefData;
// #endif
// #if implement-sqlserver || implement-postgres
global using Microsoft.EntityFrameworkCore;
// #endif
global using Microsoft.Extensions.DependencyInjection;
global using System.Text.Json.Serialization;
global using app-name.Application.Repositories;