using CoreEx.Json;
using CoreEx.TestFunction;
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
        public void NoValidator_NoBody()
        {
            using var test = FunctionTester.Create<Startup>();
            test.HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.RunNoValidatorAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/novalidator")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");
        }

        [Test]
        public void NoValidator_InvalidBody()
        {
            using var test = FunctionTester.Create<Startup>();
            test.HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.RunNoValidatorAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/novalidator", "<xml/>")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: '<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.");
        }

        [Test]
        public void NoValidator_InvalidBody_Newtonsoft()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ConfigureServices(sc => sc.ReplaceScoped<IJsonSerializer>(new CoreEx.Newtonsoft.Json.JsonSerializer()))
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.RunNoValidatorAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/novalidator", "<xml/>")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Unexpected character encountered while parsing value: <. Path '', line 0, position 0.");
        }

        [Test]
        public void NoValidator_InvalidJson()
        {
            using var test = FunctionTester.Create<Startup>();
            test.HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.RunNoValidatorAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/novalidator", "{\"price\": \"xx.xx\"}")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: The JSON value could not be converted to System.Decimal. Path: $.price | LineNumber: 0 | BytePositionInLine: 17.");
        }

        [Test]
        public void NoValidator_InvalidJson_Newtonsoft()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ConfigureServices(sc => sc.ReplaceScoped<IJsonSerializer>(new CoreEx.Newtonsoft.Json.JsonSerializer()))
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.RunNoValidatorAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/novalidator", "{\"price\": \"xx.xx\"}")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Could not convert string to decimal: xx.xx. Path 'price', line 1, position 17.");
        }

        [Test]
        public void NoValidator_Success()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "").WithJsonBody(new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m }).Respond.WithJson(new BackendProduct { Code = "AX", Description = "BX", RetailPrice = 10.99m });

            using var test = FunctionTester.Create<Startup>();
            test.ConfigureServices(sc => mcf.Replace(sc))
                .HttpTrigger<HttpTriggerFunction>()
                .Run(f => f.RunNoValidatorAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest/novalidator", new { id = "A", name = "B", price = 1.99m })))
                .AssertOK()
                .Assert(new Product { Id = "AX", Name = "BX", Price = 10.99m });

            mcf.VerifyAll();
        }
    }
}