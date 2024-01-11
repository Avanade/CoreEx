using CoreEx.AspNetCore.Http;
using CoreEx.AspNetCore.WebApis;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Results;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx;
using UnitTestEx.NUnit;
using HttpRequestOptions = CoreEx.Http.HttpRequestOptions;

namespace CoreEx.Test.Framework.WebApis
{
    [TestFixture]
    public class WebApiWithResultTest
    {
        [Test]
        public void GetWithResultAsync_NoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest"), r => Task.FromResult(Result<string?>.Ok(null))))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.NotFound);
        }

        [Test]
        public void GetWithResultAsync_WithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result.Ok("it-worked"))))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue("it-worked");
        }

        [Test]
        public void GetWithResultAsync_WithETagValue()
        {
            using var test = FunctionTester.Create<Startup>();
            var vcr = test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result.Ok(new Person { Id = 1, Name = "Angela", ETag = "my-etag" }))))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr, Is.Not.Null);
            Assert.That(vcr!.ETag, Is.EqualTo("my-etag"));
        }

        [Test]
        public void GetWithResultAsync_WithETagValueNotModified()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples");
            hr.Headers.Add(HeaderNames.IfMatch, "my-etag");

            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(hr, r => Task.FromResult(Result.Ok(new Person { Id = 1, Name = "Angela", ETag = "my-etag" }))))
                .ToActionResultAssertor()
                .AssertNotModified();
        }

        [Test]
        public void GetWithResultAsync_WithGenETagValue()
        {
            using var test = FunctionTester.Create<Startup>();
            var vcr = test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result.Ok(new Person { Id = 1, Name = "Angela" }))))
                .ToActionResultAssertor()
                .AssertOK()
                .Result as ValueContentResult;

            Assert.That(vcr, Is.Not.Null);
            Assert.That(vcr!.ETag, Is.EqualTo("iVsGVb/ELj5dvXpe3ImuOy/vxLIJnUtU2b8nIfpX5PM="));
        }

        [Test]
        public void GetWithResultAsync_WithGenETagValueNotModified()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples");
            hr.Headers.Add(HeaderNames.IfMatch, "iVsGVb/ELj5dvXpe3ImuOy/vxLIJnUtU2b8nIfpX5PM=");

            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(hr, r => Task.FromResult(Result.Ok(new Person { Id = 1, Name = "Angela" }))))
                .ToActionResultAssertor()
                .AssertNotModified();
        }

        [Test]
        public void GetWithResultAsync_WithCollectionNull()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result<PersonCollection>.Ok(null!)), alternateStatusCode: HttpStatusCode.NoContent)) 
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void GetWithResultAsync_WithCollectionEmpty()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result.Ok(new PersonCollection())), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection());
        }

        [Test]
        public void GetWithResultAsync_WithCollectionResultNull()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples");

            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result<PersonCollectionResult>.Ok(null!)), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void GetWithResultAsync_WithCollectionResultNullCollection()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result.Ok(new PersonCollectionResult())), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(Array.Empty<object?>());
        }

        [Test]
        public void GetWithResultAsync_WithCollectionResultItems()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result.Ok(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } })), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1, Name = "Simon" } });
        }

        [Test]
        public void GetWithResultAsync_WithCollectionResultItemsAndPaging()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result.Ok(new PersonCollectionResult { Paging = new PagingResult(PagingArgs.CreateSkipAndTake(2, 3), 20), Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } })), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            Assert.That(((ValueContentResult)r.Result).PagingResult, Is.Not.EqualTo(new PagingResult(PagingArgs.CreateSkipAndTake(2, 3), 20)));
        }

        [Test]
        public void GetWithResultAsync_WithCollectionResultItems_ETagDiffQueryString()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result.Ok(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } })), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            var r2 = test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=oranges"), r => Task.FromResult(Result.Ok(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } })), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1, Name = "Simon" } });

            Assert.That(((ValueContentResult)r2.Result).ETag, Is.Not.EqualTo(((ValueContentResult)r.Result).ETag));
        }

        [Test]
        public void GetWithResultAsync_WithCollection_FieldsInclude()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples&$fields=name"), r => Task.FromResult(Result.Ok(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } })), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Name = "Simon" } });
        }

        [Test]
        public void GetWithResultAsync_WithCollection_FieldsExclude()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples&$exclude=name"), r => Task.FromResult(Result.Ok(new PersonCollectionResult { Items = new PersonCollection { new Person { Id = 1, Name = "Simon" } } })), alternateStatusCode: HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new PersonCollection { new Person { Id = 1 } });
        }

        [Test]
        public void PostWithResultAsync_NoValueNoResult()
        {
            static Task<Result> Success(WebApiParam p) => Task.FromResult(Result.Success);

            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostWithResultAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), Success))
                .ToActionResultAssertor()
                .AssertOK();
        }

        [Test]
        public void PostWithResultAsync_NoValueWithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostWithResultAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), r => Task.FromResult(Result.Ok(new Product { Id = "A", Name = "B", Price = 1.99m }))))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Product { Id = "A", Name = "B", Price = 1.99m });
        }

        [Test]
        public void PostWithResultAsync_WithValueNoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostWithResultAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.FromResult(Result.Success); }))
                .ToActionResultAssertor()
                .AssertOK();
        }

        [Test]
        public void PostWithResultAsync_WithValueWithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PostWithResultAsync<Product, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.FromResult(Result.Ok(new Product { Id = "Y", Name = "Z", Price = 3.01m })); }))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Product { Id = "Y", Name = "Z", Price = 3.01m });
        }

        [Test]
        public void PutWithResultAsync_WithValueNoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutWithResultAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.FromResult(Result.Success); }))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void PutWithResultAsync_WithValueWithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutWithResultAsync<Product, Product>(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 1.99m }),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, r.Value); return Task.FromResult(Result.Ok(new Product { Id = "Y", Name = "Z", Price = 3.01m })); }))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Product { Id = "Y", Name = "Z", Price = 3.01m });
        }

        [Test]
        public void PutWithResultAsync_AutoConcurrency_NoIfMatch()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutWithResultAsync(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 2.99m }),
                        r => Task.FromResult<Result<Product?>>(Result.Ok<Product?>(new Product { Id = "A", Name = "B", Price = 1.99m })),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 2.99m }, r.Value); return Task.FromResult(Result.Ok(new Product { Id = "Y", Name = "Z", Price = 3.99m })); },
                        simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("An 'If-Match' header is required for an HTTP PUT where the underlying entity supports concurrency (ETag).");
        }

        [Test]
        public void PutWithResultAsync_AutoConcurrency_NoMatch()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutWithResultAsync(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 2.99m }, new HttpRequestOptions { ETag = "bbb" }),
                        r => Task.FromResult<Result<Product?>>(Result.Ok<Product?>(new Product { Id = "A", Name = "B", Price = 1.99m })),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 2.99m }, r.Value); return Task.FromResult(Result.Ok(new Product { Id = "Y", Name = "Z", Price = 3.99m })); },
                        simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PutWithResultAsync_AutoConcurrency_Match()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.PutWithResultAsync(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest", new { id = "A", name = "B", price = 2.99m }, new HttpRequestOptions { ETag = "98Oe+fRzgTuVae59mLwf0Mj+iKySTlgUxEQt18huJZg=" }),
                        r => Task.FromResult<Result<Product?>>(Result.Ok<Product?>(new Product { Id = "A", Name = "B", Price = 1.99m })),
                        r => { ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 2.99m }, r.Value); return Task.FromResult(Result.Ok(new Product { Id = "Y", Name = "Z", Price = 3.99m })); },
                        simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Product { Id = "Y", Name = "Z", Price = 3.99m });
        }

        [Test]
        public void DeleteWithResultAsync()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.DeleteWithResultAsync(test.CreateHttpRequest(HttpMethod.Delete, "https://unittest"), _ => Task.FromResult(Result.Success)))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void DeleteWithResultAsync_NotFoundError()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.DeleteWithResultAsync(test.CreateHttpRequest(HttpMethod.Delete, "https://unittest"), _ => Task.FromResult(Result.NotFoundError())))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void DeleteWithResultAsync_AuthenticationError()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<WebApi>()
                .Run(f => f.DeleteWithResultAsync(test.CreateHttpRequest(HttpMethod.Delete, "https://unittest"), _ => Task.FromResult(Result.AuthenticationError())))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.Unauthorized);
        }

        [Test]
        public void PatchWithResultAsync_WithInvalidContentType()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = test.CreateHttpRequest(HttpMethod.Patch, "https://unittest");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(null!)), put: _ => Task.FromResult(Result<Person>.Ok(null!))))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.UnsupportedMediaType)
                .AssertContent("Unsupported 'Content-Type' for a PATCH; only JSON Merge Patch is supported using either: 'application/merge-patch+json' or 'application/json'.");
        }

        [Test]
        public void PatchWithResultAsync_WithNullJson()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, null);
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<PersonCollection?>.Ok(null!)), put: _ => Task.FromResult(Result<PersonCollection>.Ok(null!))))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");
        }

        [Test]
        public void PatchWithResultAsync_WithBadJson()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "<xml/>");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<PersonCollection?>.Ok(null!)), put: _ => Task.FromResult(Result<PersonCollection>.Ok(null!))))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertContent("'<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.");
        }

        [Test]
        public void PatchWithResultAsync_WithNoETag()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ }");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person())), put: _ => Task.FromResult(Result<Person>.Ok(null!))))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("An 'If-Match' header is required for an HTTP PATCH where the underlying entity supports concurrency (ETag).");
        }

        [Test]
        public void PatchWithResultAsync_WithETagHeader_ThenNotFound()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ }", "aaaa");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(null!)), put: _ => Task.FromResult(Result<Person>.Ok(null!))))
                .ToActionResultAssertor()
                .AssertNotFound();
        }

        [Test]
        public void PatchWithResultAsync_WithETagProperty_ThenNotFound()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"etag\": \"aaa\"}");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(null!)), put: _ => Task.FromResult(Result<Person>.Ok(null!))))
                .ToActionResultAssertor()
                .AssertNotFound();
        }

        [Test]
        public void PatchWithResultAsync_WithETagHeader_NotMatched()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person { ETag = "bbb" })), put: _ => Task.FromResult(Result<Person>.Ok(null!))))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PatchWithResultAsync_WithETagProperty_NotMatched()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"etag\": \"aaa\"}");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person { ETag = "bbb" })), put: _ => Task.FromResult(Result<Person>.Ok(null!))))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PatchWithResultAsync_WithETagHeader_PutConcurrency()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"bob\" }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person { ETag = "aaa" })), put: _ => throw new ConcurrencyException()))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PatchWithResultAsync_WithETagHeader_SimulateDuplicate_WasMergedWithChanges()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"bob\" }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person { Name = "bobby", ETag = "aaa" })), put: _ => throw new DuplicateException()))
                .ToActionResultAssertor()
                .AssertConflict();
        }

        [Test]
        public void PatchWithResultAsync_WithETagHeader_OK_WithNoMergeChanges()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"bob\" }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person { Name = "bob", ETag = "aaa" })), put: _ => throw new ConcurrencyException()))
                .ToActionResultAssertor()
                .AssertOK();
        }

        [Test]
        public void PatchWithResultAsync_WithETagHeader_OK_SimulateChanged()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"bobby\" }", "aaa");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr,
                    get: _ => Task.FromResult(Result<Person?>.Ok(new Person { Name = "bob", ETag = "aaa" })),
                    put: p => { ObjectComparer.Assert(new Person { Name = "bobby", ETag = "aaa" }, p.Value); p.Value!.ETag = "bbb"; return Task.FromResult(Result.Ok(p.Value!)); }))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Person { Name = "bobby", ETag = "bbb" })
                .AssertETagHeader("bbb");
        }

        [Test]
        public void PatchWithResultAsync_AutoConcurrency_NoIfMatchHeader()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"Gazza\" }");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person { Id = 13, Name = "Deano" })), put: _ => Task.FromResult(Result<Person>.Ok(null!)), simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("An 'If-Match' header is required for an HTTP PATCH where the underlying entity supports concurrency (ETag).");
        }

        [Test]
        public void PatchWithResultAsync_AutoConcurrency_NotMatched()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"Gazza\" }", etag: "bbb");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person { Id = 13, Name = "Deano" })), put: _ => Task.FromResult(Result<Person>.Ok(null!)), simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertPreconditionFailed()
                .AssertContent("A concurrency error occurred; please refresh the data and try again.");
        }

        [Test]
        public void PatchWithResultAsync_AutoConcurrency_Matched()
        {
            using var test = FunctionTester.Create<Startup>();
            var hr = CreatePatchRequest(test, "{ \"name\": \"Gazza\" }", etag: "Q8nNyU0hP+j7+1tDN0JzLGMcfPOX8OsLAh7lma4U0xo=");
            test.Type<WebApi>()
                .Run(f => f.PatchWithResultAsync(hr, get: _ => Task.FromResult(Result<Person?>.Ok(new Person { Id = 13, Name = "Deano" })), put: _ => Task.FromResult(Result.Ok(new Person { Id = 13, Name = "Gazza" })), simulatedConcurrency: true))
                .ToActionResultAssertor()
                .AssertOK()
                .AssertValue(new Person { Id = 13, Name = "Gazza" });
        }

        [Test]
        public void RunAsync_ValidationException_NoCatchAndHandleExceptions()
        {
            using var test = FunctionTester.Create<Startup>().ReplaceScoped(_ => new WebApiInvoker { CatchAndHandleExceptions = false });
            test.Type<WebApi>()
                .Run(f => f.GetWithResultAsync(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/testget?fruit=apples"), r => Task.FromResult(Result<string>.Fail("it-failed"))))
                .AssertException<BusinessException>();
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