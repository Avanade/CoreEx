global using CoreEx;
global using CoreEx.AspNetCore.Mvc;
global using CoreEx.Caching;
// #if implement-cosmos
global using CoreEx.Cosmos;
// #endif
// #if implement-relational
global using CoreEx.Database;
// #endif
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
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
// #if implement-cosmos
global using Microsoft.Azure.Cosmos;
// #endif
global using NSwag.Annotations;
global using System.Net;
global using System.Text.Json;
