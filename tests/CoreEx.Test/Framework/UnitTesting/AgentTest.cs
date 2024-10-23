using CoreEx.TestApi;
using NUnit.Framework;
using UnitTestEx.NUnit;
using UnitTestEx;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using CoreEx.Json;
using CoreEx.Configuration;
using CoreEx.Http;
using CoreEx.TestFunction.Models;
using UnitTestEx.Expectations;
using UnitTestEx.Assertors;

namespace CoreEx.Test.Framework.UnitTesting
{
    [TestFixture]
    public class AgentTest
    {
        [OneTimeSetUp]
        public void SetUp() => TestSetUp.Default.JsonSerializer = new CoreEx.Text.Json.JsonSerializer().ToUnitTestEx();

        [Test]
        public void Get()
        {
            var test = ApiTester.Create<Startup>();
            test.Agent().With<ProductAgent, Product>()
                .ExpectIdentifier()
                .ExpectValue(new Product { Name = "Apple", Price = 0.79m })
                .Run(a => a.GetAsync("abc"))
                .AssertOK();
        }

        [Test]
        public void Update_Error()
        {
            var test = ApiTester.Create<Startup>();
            var result = test.Agent().With<ProductAgent, Product>()
                .ExpectErrorType(CoreEx.Abstractions.ErrorType.ValidationError)
                .ExpectError("Zed is dead.")
                .Run(a => a.UpdateAsync(new Product { Name = "Apple", Price = 0.79m }, "Zed"))
                .Result;

            Assert.That(result.Response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        }

        [Test]
        public void Delete()
        {
            var test = ApiTester.Create<Startup>();
            var res = test.Agent().With<ProductAgent>()
                .Run(a => a.DeleteAsync("abc"))
                .AssertNoContent();

            var result = (HttpResultAssertor)res;
            Assert.That(result.Response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));
        }

        [Test]
        public void Catalogue()
        {
            var test = ApiTester.Create<Startup>();
            var res = test.Agent().With<ProductAgent>()
                .Run(a => a.CatalogueAsync("abc"))
                .AssertOK()
                .AssertContentTypePlainText()
                .AssertContent("Catalog for 'abc'.");

            var result = (HttpResultAssertor)res;
            Assert.That(result.Response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        }

        public class ProductAgent(HttpClient client, IJsonSerializer jsonSerializer, CoreEx.ExecutionContext executionContext) : CoreEx.Http.TypedHttpClientBase<ProductAgent>(client, jsonSerializer, executionContext)
        {
            public Task<HttpResult<Product>> GetAsync(string id, CoreEx.Http.HttpRequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
                => GetAsync<Product>("products/{id}", requestOptions: requestOptions, args: new IHttpArg[] { new HttpArg<string>("id", id) }, cancellationToken: cancellationToken);

            public Task<HttpResult> DeleteAsync(string id, CoreEx.Http.HttpRequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
                => DeleteAsync("products/{id}", requestOptions: requestOptions, args: new IHttpArg[] { new HttpArg<string>("id", id) }, cancellationToken: cancellationToken);

            public Task<HttpResult<Product>> UpdateAsync(Product value, string id, CoreEx.Http.HttpRequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
                => PutAsync<Product, Product>("products/{id}", value, requestOptions: requestOptions, args: new IHttpArg[] { new HttpArg<string>("id", id) }, cancellationToken: cancellationToken);

            public Task<HttpResult> CatalogueAsync(string id, CoreEx.Http.HttpRequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
                => GetAsync("products/{id}/catalogue", requestOptions: requestOptions, args: new IHttpArg[] { new HttpArg<string>("id", id) }, cancellationToken: cancellationToken);
        }
    }
}