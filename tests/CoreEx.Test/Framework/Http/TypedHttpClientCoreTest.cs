using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Http
{
    [TestFixture]
    public class TypedHttpClientCoreTest
    {
        [Test]
        public void Get_ValidationError()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.With("{\"Name\":[\"'Name' must not be empty.\"]}", HttpStatusCode.BadRequest);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.GetAsync("test"))
                .AssertSuccess();

            Assert.IsFalse(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.BadRequest, r.Result.StatusCode);
            Assert.IsNull(r.Result.ErrorCode);
            Assert.IsNull(r.Result.ErrorType);

            try
            {
                r.Result.ThrowOnError();
                Assert.Fail("Should not get here!");
            }
            catch (ValidationException vex)
            {
                Assert.NotNull(vex.Messages);
                Assert.AreEqual(1, vex.Messages.Count);
                Assert.AreEqual("Name", vex.Messages[0].Property);
                Assert.AreEqual("'Name' must not be empty.", vex.Messages[0].Text);
                Assert.AreEqual(MessageType.Error, vex.Messages[0].Type);
            }
            catch
            {
                Assert.Fail("Should not get here!");
            }

            Assert.Throws<HttpRequestException>(() => r.Result.ThrowOnError(false));

            mcf.VerifyAll();
        }

        [Test]
        public void Get_BusinessError()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.With("Serious error occurred.", HttpStatusCode.BadRequest, r =>
            {
                r.Headers.Add(HttpConsts.ErrorTypeHeaderName, ErrorType.BusinessError.ToString());
                r.Headers.Add(HttpConsts.ErrorCodeHeaderName, ((int)ErrorType.BusinessError).ToString());
            });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.GetAsync("test"))
                .AssertSuccess();

            Assert.IsFalse(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.BadRequest, r.Result.StatusCode);
            Assert.AreEqual(2, r.Result.ErrorCode);
            Assert.AreEqual("BusinessError", r.Result.ErrorType);
            Assert.AreEqual("Serious error occurred.", r.Result.Content);

            Assert.Throws<BusinessException>(() => r.Result.ThrowOnError());
            Assert.Throws<HttpRequestException>(() => r.Result.ThrowOnError(false));

            mcf.VerifyAll();
        }

        [Test]
        public void Get_Success()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.With("test-content");

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.GetAsync("test"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.AreEqual("test-content", r.Result.Content);

            mcf.VerifyAll();
        }

        [Test]
        public void Get_SuccessWithResponse()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "product/abc").Respond.WithJson(new Product { Id = "abc", Name = "banana", Price = 0.99m });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.GetAsync<Product>("product/abc"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            ObjectComparer.Assert(new Product { Id = "abc", Name = "banana", Price = 0.99m }, r.Result.Value);

            mcf.VerifyAll();
        }

        [Test]
        public void Get_SuccessWithCollectionResult()
        {
            var pc = new ProductCollection { new Product { Id = "abc", Name = "banana", Price = 0.99m }, new Product { Id = "def", Name = "apple", Price = 0.49m } };

            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "product").Respond.WithJson(pc);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.GetCollectionResultAsync<ProductCollectionResult, ProductCollection, Product>("product"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            ObjectComparer.Assert(new ProductCollectionResult { Collection = pc }, r.Result.Value);

            mcf.VerifyAll();
        }

        [Test]
        public void Get_SuccessWithCollectionResultAndPaging()
        {
            var pc = new ProductCollection { new Product { Id = "abc", Name = "banana", Price = 0.99m }, new Product { Id = "def", Name = "apple", Price = 0.49m } };

            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "product").Respond.WithJson(pc, HttpStatusCode.OK, r =>
            {
                r.Headers.Add(HttpConsts.PagingSkipHeaderName, "100");
                r.Headers.Add(HttpConsts.PagingTakeHeaderName, "25");
                r.Headers.Add(HttpConsts.PagingTotalCountHeaderName, "1000");
            });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.GetCollectionResultAsync<ProductCollectionResult, ProductCollection, Product>("product"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            ObjectComparer.Assert(new ProductCollectionResult { Collection = pc, Paging = new PagingResult(PagingArgs.CreateSkipAndTake(100, 25), 1000) }, r.Result.Value);

            mcf.VerifyAll();
        }

        [Test]
        public void Post_Success()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").Respond.With("test-content");

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.PostAsync("test"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.AreEqual("test-content", r.Result.Content);

            mcf.VerifyAll();
        }

        [Test]
        public void Post_SuccessWithRequest()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.With("test-content");

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.AreEqual("test-content", r.Result.Content);

            mcf.VerifyAll();
        }

        [Test]
        public void Post_SuccessWithResponse()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").Respond.WithJson(new Product { Id = "abc", Name = "banana", Price = 0.99m });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.PostAsync<Product>("test"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            ObjectComparer.Assert(new Product { Id = "abc", Name = "banana", Price = 0.99m }, r.Result.Value);

            mcf.VerifyAll();
        }

        [Test]
        public void Post_SuccessWithRequestResponse()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new BackendProduct {  Code = "def", Description = "apple", RetailPrice = 0.49m }).Respond.WithJson(new Product { Id = "abc", Name = "banana", Price = 0.99m });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.PostAsync<BackendProduct, Product>("test", new BackendProduct { Code = "def", Description = "apple", RetailPrice = 0.49m }))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            ObjectComparer.Assert(new Product { Id = "abc", Name = "banana", Price = 0.99m }, r.Result.Value);

            mcf.VerifyAll();
        }

        [Test]
        public void Put_SuccessWithRequest()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Put, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.With("test-content");

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.PutAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.AreEqual("test-content", r.Result.Content);

            mcf.VerifyAll();
        }

        [Test]
        public void Put_SuccessWithRequestResponse()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Put, "test").WithJsonBody(new BackendProduct { Code = "def", Description = "apple", RetailPrice = 0.49m }).Respond.WithJson(new Product { Id = "abc", Name = "banana", Price = 0.99m });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.PutAsync<BackendProduct, Product>("test", new BackendProduct { Code = "def", Description = "apple", RetailPrice = 0.49m }))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            ObjectComparer.Assert(new Product { Id = "abc", Name = "banana", Price = 0.99m }, r.Result.Value);

            mcf.VerifyAll();
        }

        [Test]
        public void Delete_Success()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Delete, "test/1").Respond.With(HttpStatusCode.OK);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.DeleteAsync("test/1"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.IsNull(r.Result.Content);

            mcf.VerifyAll();
        }

        [Test]
        public void Head_Success()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Head, "test/1").Respond.With(HttpStatusCode.OK);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.HeadAsync("test/1"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.IsNull(r.Result.Content);

            mcf.VerifyAll();
        }

        [Test]
        public void Patch_SuccessMergePatch()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Patch, "test/1").WithBody("{\"name\":\"jenny\"}", HttpConsts.MergePatchMediaTypeName).Respond.With(HttpStatusCode.OK);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.PatchAsync("test/1", HttpPatchOption.MergePatch, "{\"name\":\"jenny\"}"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.IsNull(r.Result.Content);

            mcf.VerifyAll();
        }

        [Test]
        public void Patch_SuccessJsonPatch()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Patch, "test/1").WithBody("{\"name\":\"jenny\"}", HttpConsts.JsonPatchMediaTypeName).Respond.With(HttpStatusCode.OK);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.PatchAsync("test/1", HttpPatchOption.JsonPatch, "{\"name\":\"jenny\"}"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.IsNull(r.Result.Content);

            mcf.VerifyAll();
        }
    }
}