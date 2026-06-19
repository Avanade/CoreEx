global using CoreEx;
global using CoreEx.Azure.Messaging.ServiceBus;
global using CoreEx.Events;
global using CoreEx.Events.Subscribing;
global using CoreEx.Events.Subscribing.Exceptions;
global using CoreEx.Results;
global using AwesomeAssertions;
global using Microsoft.Extensions.DependencyInjection;
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