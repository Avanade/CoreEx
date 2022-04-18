using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        public void RunAsync_UnhandledException()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), _ => throw new DivideByZeroException()))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.InternalServerError);
        }

        [Test]
        public void RunAsync_WithValue()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.RunAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.FromResult((IActionResult)new StatusCodeResult(201)); }))
                .ToActionResultAssertor()
                .AssertCreated();
        }

        [Test]
        public void GetAsync_NoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest"), r => Task.FromResult<string?>(null)))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.NotFound);
        }

        [Test]
        public void GetAsync_WithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult("it-worked")))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert("it-worked");
        }

        [Test]
        public void GetAsync_WithETagValue()
        {
            using var test = FunctionTester.Create<Startup>();
            var vcr = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new Person { Id = 1, Name = "Angela", ETag = "my-etag" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.NotNull(vcr);
            Assert.AreEqual("my-etag", vcr!.ETag);
        }

        [Test]
        public void GetAsync_WithETagValueNotModified()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples");
            hr.Headers.Add(HeaderNames.IfMatch, "my-etag");

            test.Type<WebApi>()
                .Run(f => f.GetAsync(hr, r => Task.FromResult(new Person { Id = 1, Name = "Angela", ETag = "my-etag" })))
                .ToActionResultAssertor()
                .AssertNotModified();
        }

        [Test]
        public void GetAsync_WithGenETagValue()
        {
            using var test = FunctionTester.Create<Startup>();
            var vcr = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new Person { Id = 1, Name = "Angela" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.NotNull(vcr);
            Assert.AreEqual("iVsGVb/ELj5dvXpe3ImuOy/vxLIJnUtU2b8nIfpX5PM=", vcr!.ETag);
        }

        [Test]
        public void GetAsync_WithGenETagValueNotModified()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples");
            hr.Headers.Add(HeaderNames.IfMatch, "iVsGVb/ELj5dvXpe3ImuOy/vxLIJnUtU2b8nIfpX5PM=");

            test.Type<WebApi>()
                .Run(f => f.GetAsync(hr, r => Task.FromResult(new Person { Id = 1, Name = "Angela" })))
                .ToActionResultAssertor()
                .AssertNotModified();
        }

        [Test]
        public void GetAsync_WithCollectionNull()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult<PersonCollection>(null!), alternateStatusCode: HttpStatusCode.NoContent)) 
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void GetAsync_WithCollectionEmpty()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new PersonCollection()), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new PersonCollection());
        }

        [Test]
        public void GetAsync_WithCollectionResultNull()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples");

            test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult<PersonCollectionResult>(null!), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void GetAsync_WithCollectionResultNullCollection()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new PersonCollectionResult()), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(Array.Empty<object?>());
        }

        [Test]
        public void GetAsync_WithCollectionResultItems()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new PersonCollectionResult { Collection = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new PersonCollection { new Person { Id = 1, Name = "Simon" } });
        }

        [Test]
        public void GetAsync_WithCollectionResultItemsAndPaging()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new PersonCollectionResult { Paging = new PagingResult(PagingArgs.CreateSkipAndTake(2, 3), 20), Collection = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            Assert.AreNotEqual(new PagingResult(PagingArgs.CreateSkipAndTake(2, 3), 20), ((ValueContentResult)r.Result).PagingResult);
        }

        [Test]
        public void GetAsync_WithCollectionResultItems_ETagDiffQueryString()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new PersonCollectionResult { Collection = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            var r2 = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=oranges"), r => Task.FromResult(new PersonCollectionResult { Collection = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            Assert.AreNotEqual(((ValueContentResult)r.Result).ETag, ((ValueContentResult)r2.Result).ETag);
        }

        [Test]
        public void GetAsync_WithCollection_FieldsInclude()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples&$fields=name"), r => Task.FromResult(new PersonCollectionResult { Collection = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new PersonCollection { new Person { Name = "Simon" } });
        }

        [Test]
        public void GetAsync_WithCollection_FieldsExclude()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples&$exclude=name"), r => Task.FromResult(new PersonCollectionResult { Collection = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new PersonCollection { new Person { Id = 1 } });
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

        private class Person : IIdentifier<int>, IETag
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? ETag { get; set; }
        }

        private class PersonCollection : List<Person> { }

        private class PersonCollectionResult : CollectionResult<PersonCollection, Person> { }
    }
}