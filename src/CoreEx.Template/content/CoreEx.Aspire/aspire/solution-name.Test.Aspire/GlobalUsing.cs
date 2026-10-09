global using Aspire.Hosting;
global using AwesomeAssertions;
global using CoreEx;
// #if has-data-provider
global using DbMigration = solution-name.Database.Program;
// #endif
global using NUnit.Framework;
global using System.Net;
// #if has-data-provider
global using TestData = solution-name.Test.Common.TestData;
// #endif
global using UnitTestEx;
global using UnitTestEx.Expectations;
