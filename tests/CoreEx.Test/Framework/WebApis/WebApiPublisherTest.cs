using CoreEx.Events;
using CoreEx.Results;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using CoreEx.AspNetCore.WebApis;
using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx.NUnit;
using CoreEx.Mapping;
using Microsoft.Extensions.DependencyInjection;

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
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "A", Name = "B", Price = 1.99m }), new WebApiPublisherArgs<Product>("test")))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.That(qn, Has.Length.EqualTo(1));
            Assert.That(qn[0], Is.EqualTo("test"));

            var ed = imp.GetEvents("test");
            Assert.That(ed, Has.Length.EqualTo(1));
            ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, ed[0].Value);
        }

        [Test]
        public void PublishAsync_Value_Error()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<Product>(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), new WebApiPublisherArgs<Product>("test")))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");

            var qn = imp.GetNames();
            Assert.That(qn, Is.Empty);
        }

        [Test]
        public void PublishAsync_Value_Mapper()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .ConfigureServices(sc => sc.AddMappers<WebApiPublisherTest>())
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<Product, BackendProduct>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "A", Name = "B", Price = 1.99m }), new WebApiPublisherArgs<Product, BackendProduct>("test")))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.That(qn, Has.Length.EqualTo(1));
            Assert.That(qn[0], Is.EqualTo("test"));

            var ed = imp.GetEvents("test");
            Assert.That(ed, Has.Length.EqualTo(1));
            ObjectComparer.Assert(new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m }, ed[0].Value);
        }

        [Test]
        public void PublishCollectionAsync_Success()
        {
            var products = new ProductCollection
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishCollectionAsync<ProductCollection, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), new WebApiPublisherCollectionArgs<ProductCollection, Product>("test")))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.That(qn, Has.Length.EqualTo(1));
            Assert.That(qn[0], Is.EqualTo("test"));

            var ed = imp.GetEvents("test");
            Assert.That(ed, Has.Length.EqualTo(3));
            ObjectComparer.Assert(products[0], ed[0].Value);
            ObjectComparer.Assert(products[1], ed[1].Value);
            ObjectComparer.Assert(products[2], ed[2].Value);
        }

        [Test]
        public void PublishCollectionAsync_Success_Mapper()
        {
            var products = new ProductCollection
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .ConfigureServices(sc => sc.AddMappers<WebApiPublisherTest>())
                .Type<WebApiPublisher>()
                .Run(f => f.PublishCollectionAsync<ProductCollection, Product, BackendProduct>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), new WebApiPublisherCollectionArgs<ProductCollection, Product, BackendProduct>("test")))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.That(qn, Has.Length.EqualTo(1));
            Assert.That(qn[0], Is.EqualTo("test"));

            var ed = imp.GetEvents("test");
            Assert.That(ed, Has.Length.EqualTo(3));
            ObjectComparer.Assert(new BackendProduct { Code = "Xyz", Description = "Widget", RetailPrice = 9.95m }, ed[0].Value);
            ObjectComparer.Assert(new BackendProduct { Code = "Xyz2", Description = "Widget2", RetailPrice = 9.95m }, ed[1].Value);
            ObjectComparer.Assert(new BackendProduct { Code = "Xyz3", Description = "Widget3", RetailPrice = 9.95m }, ed[2].Value);
        }

        [Test]
        public void PublishCollectionAsync_Success_WithCorrelationId()
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

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishCollectionAsync<ProductCollection, Product>(hr, new WebApiPublisherCollectionArgs<ProductCollection, Product>("test")))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.That(qn, Has.Length.EqualTo(1));
            Assert.That(qn[0], Is.EqualTo("test"));

            var ed = imp.GetEvents("test");
            Assert.That(ed, Has.Length.EqualTo(3));
            ObjectComparer.Assert(products[0], ed[0].Value);
            ObjectComparer.Assert(products[1], ed[1].Value);
            ObjectComparer.Assert(products[2], ed[2].Value);

            Assert.Multiple(() =>
            {
                // Assert the known correlation id.
                Assert.That(ed[0].CorrelationId, Is.EqualTo("corr-id"));
                Assert.That(ed[1].CorrelationId, Is.EqualTo("corr-id"));
                Assert.That(ed[2].CorrelationId, Is.EqualTo("corr-id"));
            });
        }

        [Test]
        public void PublishCollectionAsync_SizeError()
        {
            var products = new ProductCollection
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishCollectionAsync<ProductCollection, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), new WebApiPublisherCollectionArgs<ProductCollection, Product>("test") { MaxCollectionSize = 2 }))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("The publish collection contains 3 items where only a maximum size of 2 is supported.");

            var qn = imp.GetNames();
            Assert.That(qn, Is.Empty);
        }

        [Test]
        public void PublishAsync_BeforeError()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "Xyz", Name = "Widget", Price = 9.95m }),
                    new WebApiPublisherArgs<Product> { EventName = "test", OnBeforeEventAsync = (_, __) => throw new BusinessException("Nope, nope!") }))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("Nope, nope!");

            var qn = imp.GetNames();
            Assert.That(qn, Is.Empty);
        }

        [Test]
        public void PublishAsync_BeforeErrorResult()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "Xyz", Name = "Widget", Price = 9.95m }),
                    new WebApiPublisherArgs<Product> { EventName = "test", OnBeforeEventAsync = (_, __) => Task.FromResult(Result.Fail("Nope, nope!")) }))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("Nope, nope!");

            var qn = imp.GetNames();
            Assert.That(qn, Is.Empty);
        }

        [Test]
        public void PublishCollectionAsync_BeforeError()
        {
            var products = new ProductCollection
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishCollectionAsync<ProductCollection, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), new WebApiPublisherCollectionArgs<ProductCollection, Product>("test") { OnBeforeEventAsync = (_, __) => throw new BusinessException("Nope, nope!") }))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("Nope, nope!");

            var qn = imp.GetNames();
            Assert.That(qn, Is.Empty);
        }

        [Test]
        public void PublishCollectionAsync_BeforeErrorResult()
        {
            var products = new ProductCollection
            {
                new Product { Id = "Xyz", Name = "Widget", Price = 9.95m },
                new Product { Id = "Xyz2", Name = "Widget2", Price = 9.95m },
                new Product { Id = "Xyz3", Name = "Widget3", Price = 9.95m }
            };

            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishCollectionAsync<ProductCollection, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", products), new WebApiPublisherCollectionArgs<ProductCollection, Product>("test") { OnBeforeEventAsync = (_, __) => Task.FromResult(Result.Fail("Nope, nope!")) }))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("Nope, nope!");

            var qn = imp.GetNames();
            Assert.That(qn, Is.Empty);
        }
    }

    // Demonnstrates a hard-coded mapper.
    public class ProductMapper : Mapper<Product, BackendProduct>
    {
        protected override BackendProduct? OnMap(Product? s, BackendProduct? d, OperationTypes operationType)
        {
            if (s is null || d is null)
                return d;

            d.Code = s.Id!;
            d.Description = s.Name;
            d.RetailPrice = s.Price;
            return d;
        }
    }
}