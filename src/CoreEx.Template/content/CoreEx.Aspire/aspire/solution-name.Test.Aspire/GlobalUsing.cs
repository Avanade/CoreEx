global using CoreEx;
global using AwesomeAssertions;
// #if implement-servicebus
global using Azure.Messaging.ServiceBus.Administration;
// #endif
global using NUnit.Framework;
global using System.Net;
global using UnitTestEx;
global using UnitTestEx.Expectations;
// #if (implement-sqlserver || implement-postgres)
global using DbMigration = solution-name.Database.Program;
global using TestData = solution-name.Test.Common.TestData;
// #endif
