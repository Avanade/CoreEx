// #if implement-servicebus
global using CoreEx.Azure.Messaging.ServiceBus;
// #endif
global using CoreEx.Events;
global using CoreEx.UnitTesting;
global using AwesomeAssertions;
global using Microsoft.Extensions.DependencyInjection;
global using NUnit.Framework;
global using System.Net;
global using UnitTestEx;
global using UnitTestEx.Expectations;
global using DbMigration = solution-name.Database.Program;
global using ExecutionContext = CoreEx.ExecutionContext;
global using TestData = solution-name.Test.Common.TestData;