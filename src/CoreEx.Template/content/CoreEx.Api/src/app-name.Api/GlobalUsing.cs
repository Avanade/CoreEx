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
global using NSwag.Annotations;
global using System.Net;
global using System.Text.Json;
