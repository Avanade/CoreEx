using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Results;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using CoreEx.AspNetCore.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnitTestEx;
using UnitTestEx.NUnit;
using HttpRequestOptions = CoreEx.Http.HttpRequestOptions;
using UnitTestEx.Functions;
using UnitTestEx.Hosting;
using CoreEx.AspNetCore.Http;

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
                .Run(f => f.RunAsync(hr, r => { Assert.That(ExecutionContext.Current.CorrelationId, Is.EqualTo("corr-id")); return Task.FromResult((IActionResult)new StatusCodeResult(200)); }))
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
                .AssertValue("A data validation error occurred."); // TODO: this is wonky!
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
        public void RunAsync_ValidationException_NoCatchAndHandleExceptions()
        {
            using var test = FunctionTester.Create<Startup>().ReplaceScoped(_ => new WebApiInvoker { CatchAndHandleExceptions = false });
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), _ => throw new ValidationException()))
                .AssertException<ValidationException>();
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
        public void RunAsync_UnhandledException_NoCatchAndHandleExceptions()
        {
            using var test = FunctionTester.Create<Startup>().ReplaceScoped(_ => new WebApiInvoker { CatchAndHandleExceptions = false });
            test.Type<WebApi>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), _ => throw new DivideByZeroException()))
                .AssertException<DivideByZeroException>();
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
                .AssertValue("it-worked");
        }

        [Test]
        public void GetAsync_WithETag()
        {
            using var test = FunctionTester.Create<Startup>();
            var vcr = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget"), r => Task.FromResult(new Person { Id = 1, Name = "Angela", ETag = "my-etag" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr, Is.Not.Null);
            Assert.That(vcr!.ETag, Is.EqualTo("my-etag"));

            // Second time should be the same.
            vcr = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget"), r => Task.FromResult(new Person { Id = 1, Name = "Angela", ETag = "my-etag" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr, Is.Not.Null);
            Assert.That(vcr!.ETag, Is.EqualTo("my-etag"));

            // However, if a query string, then etag will need to be generated, as it possibly can influence result.
            vcr = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new Person { Id = 1, Name = "Angela", ETag = "my-etag" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr, Is.Not.Null);
            Assert.That(vcr!.ETag, Is.Not.EqualTo("my-etag"));

            var vcr2 = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new Person { Id = 1, Name = "Angela", ETag = "my-etag" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr2, Is.Not.Null);
            Assert.That(vcr2!.ETag, Is.EqualTo(vcr.ETag));
        }

        [Test]
        public void GetAsync_WithETagValue()
        {
            using var test = FunctionTester.Create<Startup>();
            var vcr = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget"), r => Task.FromResult(new Person { Id = 1, Name = "Angela", ETag = "my-etag" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr, Is.Not.Null);
            Assert.That(vcr!.ETag, Is.EqualTo("my-etag"));
        }

        [Test]
        public void GetAsync_WithETagValueNotModified()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget");
            hr.Headers.Add(HeaderNames.IfMatch, "\\W\"my-etag\"");

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
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget"), r => Task.FromResult(new Person { Id = 1, Name = "Angela" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr, Is.Not.Null);
            Assert.That(vcr!.ETag, Is.EqualTo("iVsGVb/ELj5dvXpe3ImuOy/vxLIJnUtU2b8nIfpX5PM="));

            var p = test.JsonSerializer.Deserialize<Person>(vcr.Content!);
            Assert.That(p, Is.Not.Null);
            Assert.That(p.ETag, Is.EqualTo("iVsGVb/ELj5dvXpe3ImuOy/vxLIJnUtU2b8nIfpX5PM="));
        }

        [Test]
        public void GetAsync_WithGenETagValue_QueryString()
        {
            using var test = FunctionTester.Create<Startup>();
            var vcr = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new Person { Id = 1, Name = "Angela" })))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr, Is.Not.Null);
            Assert.That(vcr!.ETag, Is.EqualTo("cpDn3xugV1xKSHF9AY4kQRNQ1yC/SU49xC66C92WZbE="));

            var p = test.JsonSerializer.Deserialize<Person>(vcr.Content!);
            Assert.That(p, Is.Not.Null);
            Assert.That(p.ETag, Is.EqualTo("iVsGVb/ELj5dvXpe3ImuOy/vxLIJnUtU2b8nIfpX5PM="));
        }

        [Test]
        public void GetAsync_WithGenETagValueNotModified()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples");
            hr.Headers.Add(HeaderNames.IfMatch, "\\W\"cpDn3xugV1xKSHF9AY4kQRNQ1yC/SU49xC66C92WZbE=\"");

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
                .AssertValue(new PersonCollection());
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
                .AssertValue(Array.Empty<object?>());
        }

        [Test]
        public void GetAsync_WithCollectionResultItems()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1, Name = "Simon" } });
        }

        [Test]
        public void GetAsync_WithCollectionResultItemsAndPaging()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new PersonCollectionResult { Paging = new PagingResult(PagingArgs.CreateSkipAndTake(2, 3), 20), Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            Assert.That(((ValueContentResult)r.Result).PagingResult, Is.EqualTo(new PagingResult(PagingArgs.CreateSkipAndTake(2, 3), 20)));
        }

        [Test]
        public void GetAsync_WithCollectionResultItems_ETagDiffQueryString()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            var r2 = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=oranges"), r => Task.FromResult(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            Assert.That(((ValueContentResult)r2.Result).ETag, Is.Not.EqualTo(((ValueContentResult)r.Result).ETag));
        }

        [Test]
        public void GetAsync_WithCollection_FieldsInclude()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples&$fields=name"), r => Task.FromResult(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Name = "Simon" } });
        }

        [Test]
        public void GetAsync_WithCollection_FieldsExclude()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples&$exclude=name"), r => Task.FromResult(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } }), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1 } });
        }

        [Test]
        public void GetAsync_WithMessages_ErrorStatusCode()
        {
            using var test = FunctionTester.Create<Startup>();
            var result = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest"), r =>
                {
                    ExecutionContext.Current.Messages.Add(MessageType.Warning, "Please renew licence.");
                    return Task.FromResult<string?>(null);
                }))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.NotFound)
                .Result as StatusCodeResult;

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetAsync_WithMessages_SuccessStatusCode()
        {
            using var test = FunctionTester.Create<Startup>();
            var result = test.Type<WebApi>()
                .Run(f => f.GetAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest"), r =>
                {
                    ExecutionContext.Current.Messages.Add(MessageType.Warning, "Please renew licence.");
                    return Task.FromResult<string?>("This is ok.");
                }))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.OK)
                .Result as ValueContentResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Messages, Is.Not.Null);
            Assert.That(result.Messages, Has.Count.EqualTo(1));
            Assert.That(result.Messages[0].Type, Is.EqualTo(MessageType.Warning));
            Assert.That(result.Messages[0].Text, Is.EqualTo("Please renew licence."));
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
                .AssertOK()
                .AssertValue(new Product { Id = "A", Name = "B", Price = 1.99m });
        }

        [Test]
        public void PostAsync_WithValueNoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.CompletedTask; }))
                .ToActionResultAssertor()
                .AssertOK();
        }

        [Test]
        public void PostAsync_WithValueWithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostAsync<Product, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.FromResult(new Product { Id = "Y", Name = "Z", Price = 3.01m }); }))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Product { Id = "Y", Name = "Z", Price = 3.01m });
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
                .AssertValue(new Product { Id = "Y", Name = "Z", Price = 3.01m });
        }

        [Test]
        public void PutAsync_AutoConcurrency_NoIfMatch()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutAsync(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 2.99m }),
                        r => Task.FromResult<Product?>(new Product { Id = "A", Name = "B", Price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 2.99m }, r.Value); return Task.FromResult(new Product { Id = "Y", Name = "Z", Price = 3.99m }); },
                        simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("An 'If-Match' header is required for an HTTP PUT where the underlying entity supports concurrency (ETag).");
        }

        [Test]
        public void PutAsync_AutoConcurrency_NoMatch()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutAsync(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 2.99m }, new HttpRequestOptions { ETag = "bbb" }),
                        r => Task.FromResult<Product?>(new Product { Id = "A", Name = "B", Price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 2.99m }, r.Value); return Task.FromResult(new Product { Id = "Y", Name = "Z", Price = 3.99m }); },
                        simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PutAsync_AutoConcurrency_Match()
        {
            using var test = FunctionTester.Create<Startup>();
            
            test.Type<WebApi>()
                .Run(f => f.PutAsync(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 2.99m }, new HttpRequestOptions { ETag = "98Oe+fRzgTuVae59mLwf0Mj+iKySTlgUxEQt18huJZg=" }),
                        r => Task.FromResult<Product?>(new Product { Id = "A", Name = "B", Price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 2.99m }, r.Value); return Task.FromResult(new Product { Id = "Y", Name = "Z", Price = 3.99m }); },
                        simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Product { Id = "Y", Name = "Z", Price = 3.99m });
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

        [Test]
        public void PatchAsync_WithInvalidContentType()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Patch, "https://unittest");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(null), put: _ => Task.FromResult<Person>(null!)))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.UnsupportedMediaType)
                .AssertContent("Unsupported 'Content-Type' for a PATCH; only JSON Merge Patch is supported using either: 'application/merge-patch+json' or 'application/json'.");
        }

        [Test]
        public void PatchAsync_WithNullJson()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, null);
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<PersonCollection?>(null), put: _ => Task.FromResult<PersonCollection>(null!)))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");
        }

        [Test]
        public void PatchAsync_WithBadJson()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "<xml/>");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<PersonCollection?>(null), put: _ => Task.FromResult<PersonCollection>(null!)))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("'<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.");
        }

        [Test]
        public void PatchAsync_WithNoETag()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ }");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person()), put: _ => Task.FromResult<Person>(null!)))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("An 'If-Match' header is required for an HTTP PATCH where the underlying entity supports concurrency (ETag).");
        }

        [Test]
        public void PatchAsync_WithETagHeader_ThenNotFound()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ }", "aaaa");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(null), put: _ => Task.FromResult<Person>(null!)))
                .ToActionResultAssertor()
                .AssertNotFound();
        }

        [Test]
        public void PatchAsync_WithETagProperty_ThenNotFound()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"etag\": \"aaa\"}");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(null), put: _ => Task.FromResult<Person>(null!)))
                .ToActionResultAssertor()
                .AssertNotFound();
        }

        [Test]
        public void PatchAsync_WithETagHeader_NotMatched()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person { ETag = "bbb" }), put: _ => Task.FromResult<Person>(null!)))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PatchAsync_WithETagProperty_NotMatched()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"etag\": \"aaa\"}");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person { ETag = "bbb" }), put: _ => Task.FromResult<Person>(null!)))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PatchAsync_WithETagHeader_PutConcurrency()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"bob\" }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person { ETag = "aaa" }), put: _ => throw new ConcurrencyException()))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PatchAsync_WithETagHeader_SimulateDuplicate_WasMergedWithChanges()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"bob\" }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person { Name = "bobby", ETag = "aaa" }), put: _ => throw new DuplicateException()))
                .ToActionResultAssertor()
                .AssertConflict();
        }

        [Test]
        public void PatchAsync_WithETagHeader_OK_WithNoMergeChanges()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"bob\" }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person { Name = "bob", ETag = "aaa" }), put: _ => throw new ConcurrencyException()))
                .ToActionResultAssertor()
                .AssertOK();
        }

        [Test]
        public void PatchAsync_WithETagHeader_OK_SimulateChanged()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"bobby\" }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr,
                    get: _ => Task.FromResult<Person?>(new Person { Name = "bob", ETag = "aaa" }),
                    put: p => { ObjectComparer.Assert(new Person { Name = "bobby", ETag = "aaa" }, p.Value); p.Value!.ETag = "bbb"; return Task.FromResult(p.Value!); }))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Person { Name = "bobby", ETag = "bbb" })
                .AssertETagHeader("bbb");
        }

        [Test]
        public void PatchAsync_AutoConcurrency_NoIfMatchHeader()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"Gazza\" }");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person { Id = 13, Name = "Deano" }), put: _ => Task.FromResult<Person>(null!), simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("An 'If-Match' header is required for an HTTP PATCH where the underlying entity supports concurrency (ETag).");
        }

        [Test]
        public void PatchAsync_AutoConcurrency_NotMatched()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"Gazza\" }", etag: "bbb");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person { Id = 13, Name = "Deano" }), put: _ => Task.FromResult<Person>(null!), simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PatchAsync_AutoConcurrency_Matched()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"Gazza\" }", etag: "Q8nNyU0hP+j7+1tDN0JzLGMcfPOX8OsLAh7lma4U0xo=");
            test.Type<WebApi>()
                .Run(f => f.PatchAsync(hr, get: _ => Task.FromResult<Person?>(new Person { Id = 13, Name = "Deano" }), put: _ => Task.FromResult<Person>(new Person { Id = 13, Name = "Gazza" }), simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Person { Id = 13, Name = "Gazza", ETag = "tEEokPXk+4Q5MoiGqyAs1+6A00e2ww59Zm57LJgvBcg=" });
        }

        private static HttpRequest CreatePatchRequest(UnitTestEx.NUnit.Internal.FunctionTester<Startup> test, string? json, string? etag = null)
            => test.CreateHttpRequest(HttpMethod.Patch, "https://unittest", json, HttpConsts.MergePatchMediaTypeName, hr => hr.ApplyRequestOptions(new HttpRequestOptions { ETag = etag }));

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