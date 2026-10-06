global using CoreEx;
// #if (implement-cosmos && outbox-enabled)
global using CoreEx.Cosmos.Outbox;
// #endif
// #if (implement-postgres && outbox-enabled)
global using CoreEx.Database.Postgres.Outbox;
// #endif
// #if (implement-sqlserver && outbox-enabled)
global using CoreEx.Database.SqlServer.Outbox;
// #endif
global using AwesomeAssertions;
global using NUnit.Framework;
global using System.Net;
global using UnitTestEx;
global using UnitTestEx.NUnit;
global using UnitTestEx.Expectations;
// #if has-data-provider
global using DbMigration = solution-name.Database.Program;
global using TestData = solution-name.Test.Common.TestData;
// #endif
