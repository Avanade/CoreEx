using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Http.Extended;
using CoreEx.Mapping;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using NUnit.Framework;
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
                Assert.AreEqual(1, vex.Messages!.Count);
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
        public void Get_ValidationError_NoMessages()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.With("Serious error occurred.", HttpStatusCode.BadRequest, r =>
            {
                r.Headers.Add(HttpConsts.ErrorTypeHeaderName, ErrorType.ValidationError.ToString());
                r.Headers.Add(HttpConsts.ErrorCodeHeaderName, ((int)ErrorType.ValidationError).ToString());
            });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.GetAsync("test"))
                .AssertSuccess();

            Assert.IsFalse(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.BadRequest, r.Result.StatusCode);
            Assert.AreEqual(1, r.Result.ErrorCode);
            Assert.AreEqual("ValidationError", r.Result.ErrorType);
            Assert.AreEqual("Serious error occurred.", r.Result.Content);

            var vex = Assert.Throws<ValidationException>(() => r.Result.ThrowOnError());
            Assert.AreEqual("Serious error occurred.", vex!.Message);
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
                .Run(f => f.GetAsync<ProductCollectionResult>("product"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            ObjectComparer.Assert(new ProductCollectionResult { Items = pc }, r.Result.Value);

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
                .Run(f => f.GetAsync<ProductCollectionResult>("product"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            ObjectComparer.Assert(new ProductCollectionResult { Items = pc, Paging = new PagingResult(PagingArgs.CreateSkipAndTake(100, 25), 1000) }, r.Result.Value);

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
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new BackendProduct { Code = "def", Description = "apple", RetailPrice = 0.49m }).Respond.WithJson(new Product { Id = "abc", Name = "banana", Price = 0.99m });

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
        public void Head_Success_QueryStringOnly()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Head, "?name=bob").Respond.With(HttpStatusCode.OK);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.HeadAsync("?name=bob"))
                .AssertSuccess();

            Assert.IsTrue(r.Result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.OK, r.Result.StatusCode);
            Assert.IsNull(r.Result.Content);

            mcf.VerifyAll();
        }

        [Test]
        public void Head_Success_QueryStringAndRequestOptions()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Head, "?name=bob&$text=true").Respond.With(HttpStatusCode.OK);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.HeadAsync("?name=bob", new HttpRequestOptions { IncludeText = true }))
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

        [Test]
        public void RetryServiceUnavailable()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new { ClassName = "Retry" }).Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.InternalServerError);
                s.Respond().With(HttpStatusCode.VariantAlsoNegotiates);
                s.Respond().With(HttpStatusCode.BadGateway);
                s.Respond().With(HttpStatusCode.LoopDetected);
            });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ConfigureServices(sc => mcf.Replace(sc))
                .Type<BackendHttpClient>()
                .Run(f => f.WithRetry().ThrowTransientException().PostAsync("test", new { ClassName = "Retry" } ))
                .AssertException<TransientException>();

            mcf.VerifyAll();
        }

        [Test]
        public async Task TypedHttpClientInstance()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.With("test-content");

            var thc = mcf.GetHttpClient("Backend")!.CreateTypedClient(onBeforeRequest: (req, ct) => { req.Headers.MaxForwards = 88; return Task.CompletedTask; });
            var res = await thc.GetAsync("test");

            Assert.That(res, Is.Not.Null);
            Assert.That(res.IsSuccess, Is.True);
            Assert.That(res.Content, Is.EqualTo("test-content"));
            Assert.That(res.Request!.Headers.MaxForwards, Is.EqualTo(88));

            mcf.VerifyAll();
        }

        [Test]
        public void OnBeforeRequest_Option()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.With("test-content");

            using var test = FunctionTester.Create<Startup>();
            var res = test.ConfigureServices(sc => mcf.Replace(sc))
                .Type<BackendHttpClient>()
                .Run(f => f.OnBeforeRequest((req, ct) => { req.Headers.MaxForwards = 88; return Task.CompletedTask; }).GetAsync("test"))
                .AssertSuccess()
                .Result;

            Assert.That(res, Is.Not.Null);
            Assert.That(res.IsSuccess, Is.True);
            Assert.That(res.Content, Is.EqualTo("test-content"));
            Assert.That(res.Request!.Headers.MaxForwards, Is.EqualTo(88));

            mcf.VerifyAll();
        }

        [Test]
        public async Task TypedHttpClientInstance_OnConfiguration()
        {
            try
            {
                TypedMappedHttpClient.OnDefaultOptionsConfiguration = null;
                TypedHttpClient.OnDefaultOptionsConfiguration = x => x.EnsureSuccess();

                Assert.IsNotNull(TypedHttpClient.OnDefaultOptionsConfiguration);
                Assert.IsNull(TypedMappedHttpClient.OnDefaultOptionsConfiguration);

                var mcf = MockHttpClientFactory.Create();
                var mc = mcf.CreateClient("Backend", "https://backend/");
                mc.Request(HttpMethod.Get, "test").Respond.With("test-content");

                var thc = mcf.GetHttpClient("Backend")!.CreateTypedClient(onBeforeRequest: (req, ct) => { req.Headers.MaxForwards = 88; return Task.CompletedTask; });

                Assert.IsTrue(thc.SendOptions.ShouldEnsureSuccess);

                var res = await thc.GetAsync("test");

                Assert.That(res, Is.Not.Null);
                Assert.That(res.IsSuccess, Is.True);
                Assert.That(res.Content, Is.EqualTo("test-content"));
                Assert.That(res.Request!.Headers.MaxForwards, Is.EqualTo(88));

                mcf.VerifyAll();
            }
            finally
            {
                TypedHttpClient.OnDefaultOptionsConfiguration = null;
            }
        }

        [Test]
        public async Task NullOnNotFound()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.NotFound);
                s.Respond().With("1");
                s.Respond().With("2");
            });

            var thc = mcf.GetHttpClient("Backend")!.CreateTypedClient();
            var res = await thc.GetAsync<int>("test");

            Assert.That(res, Is.Not.Null);
            Assert.That(res.IsSuccess, Is.False);
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(res.ErrorType, Is.Null);
            Assert.That(res.ErrorCode, Is.Null);
            Assert.That(res.WillResultInNullAsNotFound, Is.False);
            Assert.Throws<NotFoundException>(() => _ = res.Value);

            res.NullOnNotFoundResponse = true;
            Assert.That(res.IsSuccess, Is.True);
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(res.ErrorType, Is.Null);
            Assert.That(res.ErrorCode, Is.Null);
            Assert.That(res.WillResultInNullAsNotFound, Is.True);
            Assert.That(res.Value, Is.EqualTo(0));

            res = await thc.GetAsync<int>("test");
            Assert.That(res.NullOnNotFoundResponse, Is.False);
            Assert.That(res.IsSuccess, Is.True);
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(res.ErrorType, Is.Null);
            Assert.That(res.ErrorCode, Is.Null);
            Assert.That(res.WillResultInNullAsNotFound, Is.False);
            Assert.That(res.Value, Is.EqualTo(1));

            res = await thc.NullOnNotFound().GetAsync<int>("test");
            Assert.That(res.NullOnNotFoundResponse, Is.True);
            Assert.That(res.IsSuccess, Is.True);
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(res.ErrorType, Is.Null);
            Assert.That(res.ErrorCode, Is.Null);
            Assert.That(res.WillResultInNullAsNotFound, Is.False);
            Assert.That(res.Value, Is.EqualTo(2));
        }

        [Test]
        public async Task ToResult()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.NotFound);
                s.Respond().With("no-not-happy", HttpStatusCode.NotAcceptable);
                s.Respond().With(HttpStatusCode.OK);
            });

            var thc = mcf.GetHttpClient("Backend")!.CreateTypedClient();

            var res = await thc.GetAsync("test");
            var r = res.ToResult();
            Assert.That(r.Error, Is.InstanceOf<NotFoundException>());

            res = await thc.GetAsync("test");
            r = res.ToResult();
            Assert.That(r.Error, Is.InstanceOf<HttpRequestException>().And.Message.EqualTo("no-not-happy"));

            res = await thc.GetAsync("test");
            r = res.ToResult();
            Assert.AreEqual(CoreEx.Results.Result.Success, r);
        }

        [Test]
        public async Task ToResultT()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.NotFound);
                s.Respond().With("no-not-happy", HttpStatusCode.NotAcceptable);
                s.Respond().With("2");
                s.Respond().With("a");
            });

            var thc = mcf.GetHttpClient("Backend")!.CreateTypedClient();

            var res = await thc.GetAsync<int>("test");
            var r = res.ToResult();
            Assert.That(r.Error, Is.InstanceOf<NotFoundException>());

            res = await thc.GetAsync<int>("test");
            r = res.ToResult();
            Assert.That(r.Error, Is.InstanceOf<HttpRequestException>().And.Message.EqualTo("no-not-happy"));

            res = await thc.GetAsync<int>("test");
            r = res.ToResult();
            Assert.AreEqual(2, r.Value);

            res = await thc.GetAsync<int>("test");
            r = res.ToResult();
            Assert.That(r.Error, Is.InstanceOf<InvalidOperationException>());
        }
    }
}