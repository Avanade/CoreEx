using CoreEx.Events;
using CoreEx.Functions;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Functions
{
    [TestFixture]
    public class HttpTriggerExecutorTest
    {
        [Test]
        public void RunAsync_Success()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => Task.FromResult((IActionResult)new OkResult())))
                .ToActionResultAssertor()
                .AssertOK();
        }

        [Test]
        public void RunAsync_CorrelationId()
        {
            var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Post, "https://unittest");
            hr.Headers.Add("x-correlation-id", "corr-id");

            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(hr, () => { Assert.AreEqual("corr-id", ExecutionContext.Current.CorrelationId); return Task.FromResult((IActionResult)new OkResult()); }))
                .ToActionResultAssertor()
                .AssertOK();
        }

        [Test]
        public void RunAsync_ValidationException1()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => throw new ValidationException()))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert("A data validation error occurred.");
        }

        [Test]
        public void RunAsync_ValidationException2()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () =>
                {
                    var msd = new ModelStateDictionary();
                    msd.AddModelError("Test", "Invalid.");
                    throw new ValidationException(msd);
                }))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertErrors(new ApiError("Test", "Invalid."));
        }

        [Test]
        public void RunAsync_TransientException()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => throw new TransientException()))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public void RunAsync_EventPublisherException()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => throw new EventPublisherException(new EventPublisherDataError[] { new EventPublisherDataError { Index = 1, Message = "Not sent." } })))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.InternalServerError)
                .Assert(new EventPublisherDataError[] { new EventPublisherDataError { Index = 1, Message = "Not sent." } });
        }

        [Test]
        public void RunAsync_NoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }), (p) => Task.CompletedTask))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void RunWithResultAsync_NoRequest()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunWithResultAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => Task.FromResult(new Product { Id = "A", Name = "B", Price = 1.99m })))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new Product { Id = "A", Name = "B", Price = 1.99m });
        }

        [Test]
        public void RunWithResultAsync_WithRequest()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunWithResultAsync<Product, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }), (p) => Task.FromResult(p)))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new Product { Id = "A", Name = "B", Price = 1.99m });
        }

        [Test]
        public void RunPublishAsync_Error()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishAsync<Product>(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), imp, "test"))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");

            var qn = imp.GetNames();
            Assert.AreEqual(0, qn.Length);
        }

        [Test]
        public void RunPublishAsync_Success()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "A", Name = "B", Price = 1.99m }), imp, "test"))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.AreEqual(1, qn.Length);
            Assert.AreEqual("test", qn[0]);

            var ed = imp.GetEvents("test");
            Assert.AreEqual(1, ed.Length);
            ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, ed[0].Value);
        }

        [Test]
        public void RunPublishCollAsync_Error()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishCollAsync<List<Product>, Product>(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), imp, "test"))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");

            var qn = imp.GetNames();
            Assert.AreEqual(0, qn.Length);
        }

        [Test]
        public void RunPublishCollAsync_SizeError()
        {
            var products = new List<Product>
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };
            
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishCollAsync<List<Product>, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), imp, "test", maxListSize: 2))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert("The publish collection contains 3 items where only a maximum size of 2 is supported.");

            var qn = imp.GetNames();
            Assert.AreEqual(0, qn.Length);
        }

        [Test]
        public void RunPublishCollAsync_Success()
        {
            var products = new List<Product>
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishCollAsync<List<Product>, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), imp, "test"))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.AreEqual(1, qn.Length);
            Assert.AreEqual("test", qn[0]);

            var ed = imp.GetEvents("test");
            Assert.AreEqual(3, ed.Length);
            ObjectComparer.Assert(products[0], ed[0].Value);
            ObjectComparer.Assert(products[1], ed[1].Value);
            ObjectComparer.Assert(products[2], ed[2].Value);
        }

        [Test]
        public void RunPublishCollAsync_Success_WithCorrelationId()
        {
            var products = new List<Product>
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            using var test = FunctionTester.Create<Startup>();
            var imp = new InMemoryPublisher();
            var hr = test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products);
            hr.Headers.Add("x-correlation-id", "corr-id"); // Send through a known correlation id.

            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishCollAsync<List<Product>, Product>(hr, imp, "test"))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.AreEqual(1, qn.Length);
            Assert.AreEqual("test", qn[0]);

            var ed = imp.GetEvents("test");
            Assert.AreEqual(3, ed.Length);
            ObjectComparer.Assert(products[0], ed[0].Value);
            ObjectComparer.Assert(products[1], ed[1].Value);
            ObjectComparer.Assert(products[2], ed[2].Value);

            // Assert the known correlation id.
            Assert.AreEqual("corr-id", ed[0].CorrelationId);
            Assert.AreEqual("corr-id", ed[1].CorrelationId);
            Assert.AreEqual("corr-id", ed[2].CorrelationId);
        }
    }
}