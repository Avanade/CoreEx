global using CoreEx;
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
global using solution-name.Contracts;
// #if (implement-sqlserver || implement-postgres)
global using DbMigration = solution-name.Database.Program;
global using TestData = solution-name.Test.Common.TestData;
// #endif