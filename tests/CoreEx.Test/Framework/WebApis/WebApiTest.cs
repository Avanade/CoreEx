using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.WebApis
{
    [TestFixture]
    public class WebApiTest
    {
        [Test]
        public void RunAsync_Success()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest"), r => Task.FromResult((IActionResult)new StatusCodeResult(200))))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(HttpStatusCode.OK);
        }

        [Test]
        public void RunAsync_CorrelationId()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Post, "https://unittest");
            hr.Headers.Add("x-correlation-id", "corr-id");

            test.Type<WebApi>()
                .Run(f => f.RunAsync(hr, r => { Assert.AreEqual("corr-id", ExecutionContext.Current.CorrelationId); return Task.FromResult((IActionResult)new StatusCodeResult(200)); }))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(HttpStatusCode.OK);
        }

        [Test]
        public void RunAsync_ValidationException1()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), _ => throw new ValidationException()))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert("A data validation error occurred.");
        }

        [Test]
        public void RunAsync_ValidationException2()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), _ =>
                {
                    var mic = new MessageItemCollection();
                    mic.AddPropertyError("Test", "Invalid.");
                    throw new ValidationException(mic);
                }))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertErrors(new ApiError("Test", "Invalid."));
        }

        [Test]
        public void RunAsync_TransientException()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), _ => throw new TransientException()))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public void RunAsync_EventPublisherException()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), _ => throw new EventPublisherException(new EventPublisherDataError[] { new EventPublisherDataError { Index = 1, Message = "Not sent." } })))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.InternalServerError)
                .Assert(new EventPublisherDataError[] { new EventPublisherDataError { Index = 1, Message = "Not sent." } });
        }

        [Test]
        public void RunAsync_UnhandledException()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), _ => throw new DivideByZeroException()))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.InternalServerError); ;
        }

        [Test]
        public void GetAsync_NoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest"), r => Task.FromResult<string>(null)))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.NotFound);
        }

        [Test]
        public void GetAsync_WithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest"), r => Task.FromResult("it-worked")))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert("it-worked");
        }

        [Test]
        public void PostAsync_NoValueNoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), r => Task.CompletedTask))
                .ToActionResultAssertor()
                .AssertOK();
        }


        [Test]
        public void PostAsync_NoValueWithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), r => Task.FromResult(new Product { Id = "A", Name = "B", Price = 1.99m })))
                .ToActionResultAssertor()
                .AssertCreated()
                .Assert(new Product { Id = "A", Name = "B", Price = 1.99m });
        }

        [Test]
        public void PostAsync_WithValueNoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.CompletedTask; }))
                .ToActionResultAssertor()
                .AssertCreated();
        }

        [Test]
        public void PostAsync_WithValueWithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostAsync<Product, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.FromResult(new Product { Id = "Y", Name = "Z", Price = 3.01m }); }))
                .ToActionResultAssertor()
                .AssertCreated()
                .Assert(new Product { Id = "Y", Name = "Z", Price = 3.01m });
        }

        [Test]
        public void PutAsync_WithValueNoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.CompletedTask; }))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void PutAsync_WithValueWithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutAsync<Product, Product>(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.FromResult(new Product { Id = "Y", Name = "Z", Price = 3.01m }); }))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new Product { Id = "Y", Name = "Z", Price = 3.01m });
        }

        [Test]
        public void DeleteAsync()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.DeleteAsync(test.CreateHttpRequest(HttpMethod.Delete, "https://unittest"), _ => Task.CompletedTask))
                .ToActionResultAssertor()
                .AssertNoContent();
        }
    }
}