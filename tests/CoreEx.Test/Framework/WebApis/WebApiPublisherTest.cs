using CoreEx.Events;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using CoreEx.WebApis;
using NUnit.Framework;
using System.Net.Http;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.WebApis
{
    [TestFixture]
    public class WebApiPublisherTest
    {
        [Test]
        public void PublishAsync_Value_Success()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ConfigureServices(sc => sc.ReplaceScoped<IEventPublisher>(_ => imp))
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "A", Name = "B", Price = 1.99m }), "test"))
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
        public void PublishAsync_Value_Error()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ConfigureServices(sc => sc.ReplaceScoped<IEventPublisher>(_ => imp))
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<Product>(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), "test"))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");

            var qn = imp.GetNames();
            Assert.AreEqual(0, qn.Length);
        }

        [Test]
        public void PublishAsync_Coll_Success()
        {
            var products = new ProductCollection
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ConfigureServices(sc => sc.ReplaceScoped<IEventPublisher>(_ => imp))
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<ProductCollection, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), "test"))
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
        public void PublishAsync_Coll_Success_WithCorrelationId()
        {
            var products = new ProductCollection
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            using var test = FunctionTester.Create<Startup>();
            var imp = new InMemoryPublisher();
            var hr = test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products);
            hr.Headers.Add("x-correlation-id", "corr-id"); // Send through a known correlation id.

            test.ConfigureServices(sc => sc.ReplaceScoped<IEventPublisher>(_ => imp))
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<ProductCollection, Product>(hr, "test"))
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

        [Test]
        public void PublishAsync_Coll_SizeError()
        {
            var products = new ProductCollection
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ConfigureServices(sc => sc.ReplaceScoped<IEventPublisher>(_ => imp))
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<ProductCollection, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), "test", maxCollSize: 2))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert("The publish collection contains 3 items where only a maximum size of 2 is supported.");

            var qn = imp.GetNames();
            Assert.AreEqual(0, qn.Length);
        }
    }
}