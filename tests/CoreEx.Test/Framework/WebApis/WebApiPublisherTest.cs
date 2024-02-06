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
using CoreEx.Hosting.Work;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using UnitTestEx;

namespace CoreEx.Test.Framework.WebApis
{
    [TestFixture]
    public class WebApiPublisherTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => UnitTestEx.Abstractions.CoreExOneOffTestSetUp.ForceSetUp();

        [Test]
        public void PublishAsync_NoValue()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f => f.PublishAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), new WebApiPublisherArgs("test")))
                .ToActionResultAssertor()
                .AssertAccepted();

            var qn = imp.GetNames();
            Assert.That(qn, Has.Length.EqualTo(1));
            Assert.That(qn[0], Is.EqualTo("test"));

            var ed = imp.GetEvents("test");
            Assert.That(ed, Has.Length.EqualTo(1));
        }

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

        [Test]
        public void Publish_With_Work_State()
        {
            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();

            var ws = test.ReplaceScoped<IEventPublisher>(_ => imp)
                .Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.PublishAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new Product { Id = "A", Name = "B", Price = 1.99m }),
                        new WebApiPublisherArgs<Product>("test") { CreateLocation = (_, @event) => new Uri($"status/{@event.Id}", UriKind.Relative) }.WithWorkState());
                })
                .ToActionResultAssertor()
                .AssertAccepted()
                .AssertLocationHeaderContains("status/")
                .GetValue<WorkState>();

            var qn = imp.GetNames();
            Assert.That(qn, Has.Length.EqualTo(1));
            Assert.That(qn[0], Is.EqualTo("test"));

            var ed = imp.GetEvents("test");
            Assert.That(ed, Has.Length.EqualTo(1));
            ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, ed[0].Value);

            var ws2 = wso.GetAsync<Product>(ed[0].Id!).Result;
            Assert.That(ws2, Is.Not.Null);
            Assert.That(ws2.Status, Is.EqualTo(WorkStatus.Created));
            
            ObjectComparer.Assert(ws, ws2);
        }

        [Test]
        public void GetWorkStatus_NotFound()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
                    return f.GetWorkStatusAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherStatusArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertNotFound();
        }

        [Test]
        public void GetWorkStatus_InProgress_OK()
        {
            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
            var ws = wso.CreateAsync(new WorkStateArgs("test", "abc")).Result;

            using var test = FunctionTester.Create<Startup>();

            // Status of created.
            var ara = test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.GetWorkStatusAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherStatusArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(ws)
                .AssertResultType<ValueContentResult>();

            var vcr = ara.Result as ValueContentResult;
            Assert.That(vcr?.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(30)));

            // Status of started.
            ws = wso.StartAsync(ws.Id!).Result;
            test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.GetWorkStatusAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherStatusArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(ws);

            // Status of indeterminate.
            ws = wso.IndeterminateAsync(ws.Id!, "Oh no!").Result;
            test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.GetWorkStatusAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherStatusArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(ws);
        }

        [Test]
        public void GetWorkStatus_Fail_BadRequest()
        {
            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
            var ws = wso.CreateAsync(new WorkStateArgs("test", "abc")).Result;
            ws = wso.StartAsync("abc").Result;
            ws = wso.FailAsync("abc", "bad-request").Result;

            using var test = FunctionTester.Create<Startup>();

            test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.GetWorkStatusAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherStatusArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertValue(ws);
        }

        [Test]
        public void GetWorkStatus_Completed()
        {
            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
            var ws = wso.CreateAsync(new WorkStateArgs("test", "abc")).Result;
            ws = wso.StartAsync("abc").Result;
            ws = wso.CompleteAsync("abc").Result;

            using var test = FunctionTester.Create<Startup>();

            var ara = test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.GetWorkStatusAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherStatusArgs("test", "abc") { CreateResultLocation = ws => new Uri($"/result/{ws.Id}", UriKind.Relative) });
                })
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.Redirect)
                .AssertLocationHeader(new Uri($"/result/{ws.Id}", UriKind.Relative));
        }

        [Test]
        public void GetWorkResult_NotFound()
        {
            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());

            using var test = FunctionTester.Create<Startup>();

            test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.GetWorkResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherResultArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertNotFound();
        }

        [Test]
        public void GetWorkResult_NoValue()
        {
            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
            var ws = wso.CreateAsync(new WorkStateArgs("test", "abc")).Result;
            ws = wso.StartAsync("abc").Result;
            ws = wso.CompleteAsync("abc").Result;

            using var test = FunctionTester.Create<Startup>();

            test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.GetWorkResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherResultArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void GetWorkResult_WithValue()
        {
            var p = new Product { Id = "A", Name = "B", Price = 1.99m };

            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
            var ws = wso.CreateAsync(new WorkStateArgs("test", "abc")).Result;
            ws = wso.StartAsync("abc").Result;
            ws = wso.CompleteAsync("abc").Result;
            wso.SetDataAsync(ws.Id!, p).Wait();

            using var test = FunctionTester.Create<Startup>();

            test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.GetWorkResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherResultArgs<Product>("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(p);
        }

        [Test]
        public void CancelWorkStatus_AlreadyFailed()
        {
            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
            var ws = wso.CreateAsync(new WorkStateArgs("test", "abc")).Result;
            ws = wso.StartAsync("abc").Result;
            ws = wso.FailAsync("abc", "bad-request").Result;

            using var test = FunctionTester.Create<Startup>();

            // Status of created.
            test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.CancelWorkStatusAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherCancelArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("A cancellation can not be performed when the current status is Failed.");
        }

        [Test]
        public void CancelWorkStatus_Success()
        {
            var wso = new WorkStateOrchestrator(new InMemoryWorkStatePersistence());
            var ws = wso.CreateAsync(new WorkStateArgs("test", "abc")).Result;
            ws = wso.StartAsync("abc").Result;

            using var test = FunctionTester.Create<Startup>();

            ws = test.Type<WebApiPublisher>()
                .Run(f =>
                {
                    f.WorkStateOrchestrator = wso;
                    return f.CancelWorkStatusAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/status/abc"), new WebApiPublisherCancelArgs("test", "abc"));
                })
                .ToActionResultAssertor()
                .AssertOK()
                .GetValue<WorkState>();

            Assert.That(ws, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(ws.Status, Is.EqualTo(WorkStatus.Cancelled));
                Assert.That(ws.Reason, Is.EqualTo("No reason was specified."));
            });
        }
    }

    // Demonstrates a hard-coded mapper.
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