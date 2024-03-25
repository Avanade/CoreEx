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
using HttpRequestOptions = CoreEx.Http.HttpRequestOptions;

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.False);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(r.Result.ErrorCode, Is.Null);
                Assert.That(r.Result.ErrorType, Is.Null);
            });

            try
            {
                r.Result.ThrowOnError();
                Assert.Fail("Should not get here!");
            }
            catch (ValidationException vex)
            {
                Assert.That(vex.Messages, Is.Not.Null);
                Assert.That(vex.Messages!, Has.Count.EqualTo(1));
                Assert.Multiple(() =>
                {
                    Assert.That(vex.Messages[0].Property, Is.EqualTo("Name"));
                    Assert.That(vex.Messages[0].Text, Is.EqualTo("'Name' must not be empty."));
                    Assert.That(vex.Messages[0].Type, Is.EqualTo(MessageType.Error));
                });
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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.False);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(r.Result.ErrorCode, Is.EqualTo(1));
                Assert.That(r.Result.ErrorType, Is.EqualTo("ValidationError"));
                Assert.That(r.Result.Content, Is.EqualTo("Serious error occurred."));
            });

            var vex = Assert.Throws<ValidationException>(() => r.Result.ThrowOnError());
            Assert.That(vex!.Message, Is.EqualTo("Serious error occurred."));
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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.False);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(r.Result.ErrorCode, Is.EqualTo(2));
                Assert.That(r.Result.ErrorType, Is.EqualTo("BusinessError"));
                Assert.That(r.Result.Content, Is.EqualTo("Serious error occurred."));
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.EqualTo("test-content"));
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
            ObjectComparer.Assert(new ProductCollectionResult { Items = pc, Paging = new PagingResult(PagingArgs.CreateSkipAndTake(100, 25), 1000) }, r.Result.Value);

            mcf.VerifyAll();
        }

        [Test]
        public void Get_SuccessWithCollectionResultAndTokenPaging()
        {
            var pc = new ProductCollection { new Product { Id = "abc", Name = "banana", Price = 0.99m }, new Product { Id = "def", Name = "apple", Price = 0.49m } };

            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "product").Respond.WithJson(pc, HttpStatusCode.OK, r =>
            {
                r.Headers.Add(HttpConsts.PagingTokenHeaderName, "token");
                r.Headers.Add(HttpConsts.PagingTakeHeaderName, "25");
                r.Headers.Add(HttpConsts.PagingTotalCountHeaderName, "1000");
            });

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.GetAsync<ProductCollectionResult>("product"))
                .AssertSuccess();

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
            ObjectComparer.Assert(new ProductCollectionResult { Items = pc, Paging = new PagingResult(PagingArgs.CreateTokenAndTake("token", 25), 1000) }, r.Result.Value);

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.EqualTo("test-content"));
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.EqualTo("test-content"));
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.EqualTo("test-content"));
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.Null);
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.Null);
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.Null);
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.Null);
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.Null);
            });

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

            Assert.Multiple(() =>
            {
                Assert.That(r.Result.IsSuccess, Is.True);
                Assert.That(r.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Result.Content, Is.Null);
            });

            mcf.VerifyAll();
        }

        [Test]
        public void InternalServerErrorAsTransient()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new { ClassName = "Retry" }).Respond.With(HttpStatusCode.InternalServerError);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ConfigureServices(sc => mcf.Replace(sc))
                .Type<BackendHttpClient>()
                .Run(f => f.ThrowTransientException().PostAsync("test", new { ClassName = "Retry" } ))
                .AssertException<TransientException>();

            mcf.VerifyAll();
        }

        [Test]
        public void ServiceUnavailableAsTransient()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new { ClassName = "Retry" }).Respond.With(HttpStatusCode.ServiceUnavailable);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ConfigureServices(sc => mcf.Replace(sc))
                .Type<BackendHttpClient>()
                .Run(f => f.ThrowTransientException().PostAsync("test", new { ClassName = "Retry" }))
                .AssertException<TransientException>();

            mcf.VerifyAll();
        }

        [Test]
        public void RequestTimeoutAsTransient()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new { ClassName = "Retry" }).Respond.With(HttpStatusCode.RequestTimeout);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ConfigureServices(sc => mcf.Replace(sc))
                .Type<BackendHttpClient>()
                .Run(f => f.ThrowTransientException().PostAsync("test", new { ClassName = "Retry" }))
                .AssertException<TransientException>();

            mcf.VerifyAll();
        }

        [Test]
        public void TooManyRequestsAsTransient()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new { ClassName = "Retry" }).Respond.With(HttpStatusCode.TooManyRequests);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ConfigureServices(sc => mcf.Replace(sc))
                .Type<BackendHttpClient>()
                .Run(f => f.ThrowTransientException().PostAsync("test", new { ClassName = "Retry" }))
                .AssertException<TransientException>();

            mcf.VerifyAll();
        }

        [Test]
        public void SocketExceptionAsTransient()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new { ClassName = "Retry" }).Respond.With(HttpStatusCode.TooManyRequests, _ => throw new System.Net.Sockets.SocketException());

            using var test = FunctionTester.Create<Startup>();
            var r = test.ConfigureServices(sc => mcf.Replace(sc))
                .Type<BackendHttpClient>()
                .Run(f => f.ThrowTransientException().PostAsync("test", new { ClassName = "Retry" }))
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
            Assert.Multiple(() =>
            {
                Assert.That(res.IsSuccess, Is.True);
                Assert.That(res.Content, Is.EqualTo("test-content"));
                Assert.That(res.Request!.Headers.MaxForwards, Is.EqualTo(88));
            });

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
            Assert.Multiple(() =>
            {
                Assert.That(res.IsSuccess, Is.True);
                Assert.That(res.Content, Is.EqualTo("test-content"));
                Assert.That(res.Request!.Headers.MaxForwards, Is.EqualTo(88));
            });

            mcf.VerifyAll();
        }

        [Test]
        public async Task TypedHttpClientInstance_OnConfiguration()
        {
            try
            {
                TypedMappedHttpClient.OnDefaultOptionsConfiguration = null;
                TypedHttpClient.OnDefaultOptionsConfiguration = x => x.EnsureSuccess();

                Assert.Multiple(() =>
                {
                    Assert.That(TypedHttpClient.OnDefaultOptionsConfiguration, Is.Not.Null);
                    Assert.That(TypedMappedHttpClient.OnDefaultOptionsConfiguration, Is.Null);
                });

                var mcf = MockHttpClientFactory.Create();
                var mc = mcf.CreateClient("Backend", "https://backend/");
                mc.Request(HttpMethod.Get, "test").Respond.With("test-content");

                var thc = mcf.GetHttpClient("Backend")!.CreateTypedClient(onBeforeRequest: (req, ct) => { req.Headers.MaxForwards = 88; return Task.CompletedTask; });

                Assert.That(thc.SendOptions.ShouldEnsureSuccess, Is.True);

                var res = await thc.GetAsync("test");

                Assert.That(res, Is.Not.Null);
                Assert.Multiple(() =>
                {
                    Assert.That(res.IsSuccess, Is.True);
                    Assert.That(res.Content, Is.EqualTo("test-content"));
                    Assert.That(res.Request!.Headers.MaxForwards, Is.EqualTo(88));
                });

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
            Assert.Multiple(() =>
            {
                Assert.That(res.IsSuccess, Is.False);
                Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(res.ErrorType, Is.Null);
                Assert.That(res.ErrorCode, Is.Null);
                Assert.That(res.WillResultInNullAsNotFound, Is.False);
            });
            Assert.Throws<NotFoundException>(() => _ = res.Value);

            res.NullOnNotFoundResponse = true;
            Assert.Multiple(() =>
            {
                Assert.That(res.IsSuccess, Is.True);
                Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(res.ErrorType, Is.Null);
                Assert.That(res.ErrorCode, Is.Null);
                Assert.That(res.WillResultInNullAsNotFound, Is.True);
                Assert.That(res.Value, Is.EqualTo(0));
            });

            res = await thc.GetAsync<int>("test");
            Assert.Multiple(() =>
            {
                Assert.That(res.NullOnNotFoundResponse, Is.False);
                Assert.That(res.IsSuccess, Is.True);
                Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res.ErrorType, Is.Null);
                Assert.That(res.ErrorCode, Is.Null);
                Assert.That(res.WillResultInNullAsNotFound, Is.False);
                Assert.That(res.Value, Is.EqualTo(1));
            });

            res = await thc.NullOnNotFound().GetAsync<int>("test");
            Assert.Multiple(() =>
            {
                Assert.That(res.NullOnNotFoundResponse, Is.True);
                Assert.That(res.IsSuccess, Is.True);
                Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(res.ErrorType, Is.Null);
                Assert.That(res.ErrorCode, Is.Null);
                Assert.That(res.WillResultInNullAsNotFound, Is.False);
                Assert.That(res.Value, Is.EqualTo(2));
            });
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
            Assert.That(r, Is.EqualTo(CoreEx.Results.Result.Success));
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
            Assert.That(r.Value, Is.EqualTo(2));

            res = await thc.GetAsync<int>("test");
            r = res.ToResult();
            Assert.That(r.Error, Is.InstanceOf<InvalidOperationException>());
        }
    }
}