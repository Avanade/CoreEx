global using CoreEx;
global using CoreEx.AspNetCore.Mvc;
global using CoreEx.Caching;
global using CoreEx.Database;
// #if implement-sqlserver
global using CoreEx.Database.SqlServer;
// #elif implement-postgres
global using CoreEx.Database.Postgres;
// #endif
global using CoreEx.Entities;
global using CoreEx.Http;
global using CoreEx.Json;
// #if refdata-enabled
global using CoreEx.RefData;
// #endif
global using CoreEx.Validation;
global using Microsoft.AspNetCore.Mvc;
global using solution-name.Application;
global using solution-name.Contracts;
global using NSwag.Annotations;
global using System.Net;
global using System.Text.Json;

// NOTE: Application layer using statements will be added after CodeGen runs.
// See: BOOTSTRAP_PHASE_2.md in your project root.
// Add the following after generating application services:
// // #if refdata-enabled
// global using solution-name.Application;
// // #endif
