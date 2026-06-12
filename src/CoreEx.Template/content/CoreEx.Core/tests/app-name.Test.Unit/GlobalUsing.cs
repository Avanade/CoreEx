global using AwesomeAssertions;
global using CoreEx;
// #if refdata-enabled
global using CoreEx.RefData;
global using CoreEx.RefData.Abstractions;
// #endif
// #if rop-enabled
global using CoreEx.Results;
// #endif
global using CoreEx.UnitTesting;
global using CoreEx.UnitTesting.Data;
global using CoreEx.Validation;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Moq;
global using NUnit.Framework;
global using UnitTestEx;
global using UnitTestEx.NUnit;
global using app-name.Contracts;
global using app-name.Application;
global using app-name.Application.Validators;
// #if refdata-enabled
global using app-name.Application.Repositories;
// #endif
global using ExecutionContext = CoreEx.ExecutionContext;
