using CoreEx.Json;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Functions;
using CoreEx.TestFunction.Models;
using NUnit.Framework;
using System.Net.Http;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace CoreEx.Test.TestFunction
{
    [TestFixture]
    public class HttpTriggerFunctionTest
    {
        [Test]
        public void NoBody()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>()
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PostAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");
        }

        [Test]
        public void InvalidBody()
        {
            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceScoped<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>()
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PostAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products", "<xml/>")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: '<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.");
        }

        [Test]
        public void InvalidBody_Newtonsoft()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PostAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products", "<xml/>")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Unexpected character encountered while parsing value: <. Path '', line 0, position 0.");
        }

        [Test]
        public void InvalidJson()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>()
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PostAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products", "{\"price\": \"xx.xx\"}")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: The JSON value could not be converted to System.Decimal. Path: $.price | LineNumber: 0 | BytePositionInLine: 17.");
        }

        [Test]
        public void InvalidJson_Newtonsoft()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PostAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products", "{\"price\": \"xx.xx\"}")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Could not convert string to decimal: xx.xx. Path 'price', line 1, position 17.");
        }

        [Test]
        public void InvalidValue()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>()
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PostAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest/products", new { id = "A", price = 1.99m })))
                .AssertBadRequest()
                .AssertErrors(new ApiError("Name", "'Name' must not be empty."));
        }

        [Test]
        public void InvalidValue_Newtonsoft()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PostAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest/products", new { id = "A", price = 1.99m })))
                .AssertBadRequest()
                .AssertErrors(new ApiError("Name", "'Name' must not be empty."));
        }

        [Test]
        public void Success()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/products");
            mc.Request(HttpMethod.Post, "").WithJsonBody(new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m }).Respond.WithJson(new BackendProduct { Code = "AX", Description = "BX", RetailPrice = 10.99m });

            using var test = FunctionTester.Create<Startup>();
            test.ReplaceHttpClientFactory(mcf)
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PutAsync(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest/products", new { id = "A", name = "B", price = 1.99m })))
                .AssertOK()
                .Assert(new Product { Id = "AX", Name = "BX", Price = 10.99m });

            mcf.VerifyAll();
        }

        [Test]
        public void Success_Newtonsoft()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/products");
            mc.Request(HttpMethod.Post, "").WithJsonBody(new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m }).Respond.WithJson(new BackendProduct { Code = "AX", Description = "BX", RetailPrice = 10.99m });

            using var test = FunctionTester.Create<Startup>();
            test.ReplaceHttpClientFactory(mcf)
                .ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.PutAsync(test.CreateJsonHttpRequest(HttpMethod.Put, "https://unittest/products", new { id = "A", name = "B", price = 1.99m })))
                .AssertOK()
                .Assert(new Product { Id = "AX", Name = "BX", Price = 10.99m });

            mcf.VerifyAll();
        }
    }
}