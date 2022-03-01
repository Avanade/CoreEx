using CoreEx.Events;
using CoreEx.Functions;
using CoreEx.Functions.FluentValidation;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Validators;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Functions.FluentValidation
{
    [TestFixture]
    public class HttpTriggerExecutorTest
    {
        [Test]
        public void RunAsync_Error()
        {
            var p = new Product { Id = "abc", Price = 1.99m };

            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync<Product, ProductValidator>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", p), pv => throw new InvalidOperationException("Should not get here.")))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertErrors(new ApiError(nameof(Product.Name), "'Name' must not be empty."));
        }

        [Test]
        public void RunAsync_Success()
        {
            var p = new Product { Id = "abc", Name = "test", Price = 1.99m };

            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync<Product, ProductValidator>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", p), pv => { ObjectComparer.Assert(p, pv); return Task.CompletedTask; }))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void RunWithResultAsync_Error()
        {
            var p = new Product { Id = "abc", Price = 1.99m };

            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunWithResultAsync<Product, ProductValidator, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", p), pv => throw new InvalidOperationException("Should not get here.")))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertErrors(new ApiError(nameof(Product.Name), "'Name' must not be empty."));
        }

        [Test]
        public void RunWithResultAsync_Success()
        {
            var p = new Product { Id = "abc", Name = "test", Price = 1.99m };

            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunWithResultAsync<Product, ProductValidator, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", p), pv => { ObjectComparer.Assert(p, pv); return Task.FromResult(new Product { Id = "abc", Name = "test", Price = 1.99m }); }))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new Product { Id = "abc", Name = "test", Price = 1.99m });
        }

        [Test]
        public void RunPublishAsync_Error()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishAsync<Product, ProductValidator>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "abc", Price = 1.99m }), imp, "test"))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertErrors(new ApiError(nameof(Product.Name), "'Name' must not be empty."));

            var qn = imp.GetNames();
            Assert.AreEqual(0, qn.Length);
        }

        [Test]
        public void RunPublishAsync_Success()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishAsync<Product, ProductValidator>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "A", Name = "B", Price = 1.99m }), imp, "test"))
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
            var products = new List<Product>
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunPublishCollAsync<List<Product>, Product, ProductsValidator>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), imp, "test"))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert(new ApiError("[1].Name", "'Name' must not be empty."));

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
                .Run(f => f.RunPublishCollAsync<List<Product>, Product, ProductsValidator>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), imp, "test", maxListSize: 2))
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
                .Run(f => f.RunPublishCollAsync<List<Product>, Product, ProductsValidator>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), imp, "test"))
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
    }
}