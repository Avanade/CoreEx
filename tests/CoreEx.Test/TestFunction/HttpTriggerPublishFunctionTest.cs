using System.Linq;
using System.Net.Http;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Functions;
using CoreEx.TestFunction.Models;
using NUnit.Framework;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace CoreEx.Test.TestFunction
{
    [TestFixture]
    public class HttpTriggerPublishFunctionTest
    {
        [Test]
        public void NoBody()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products/publish")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.");

            Assert.AreEqual(0, imp.GetNames().Length);
        }

        [Test]
        public void InvalidBody()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products/publish", "<xml/>")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: '<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.");

            Assert.AreEqual(0, imp.GetNames().Length);
        }

        [Test]
        public void InvalidBody_Newtonsoft()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();

            test.ConfigureServices(sc => sc.ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>())
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products/publish", "<xml/>")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Unexpected character encountered while parsing value: <. Path '', line 0, position 0.");

            Assert.AreEqual(0, imp.GetNames().Length);
        }

        [Test]
        public void InvalidJson()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products/publish", "{\"price\": \"xx.xx\"}")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: The JSON value could not be converted to System.Decimal. Path: $.price | LineNumber: 0 | BytePositionInLine: 17.");

            Assert.AreEqual(0, imp.GetNames().Length);
        }

        [Test]
        public void InvalidJson_Newtonsoft()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .ConfigureServices(sc => sc.ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>())
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest/products/publish", "{\"price\": \"xx.xx\"}")))
                .AssertBadRequest()
                .AssertErrors("Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted: Could not convert string to decimal: xx.xx. Path 'price', line 1, position 17.");

            Assert.AreEqual(0, imp.GetNames().Length);
        }

        [Test]
        public void InvalidValue()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest/products", new { id = "A", price = 1.99m })))
                .AssertBadRequest()
                .AssertErrors(new ApiError("Name", "'Name' must not be empty."));

            Assert.AreEqual(0, imp.GetNames().Length);
        }

        [Test]
        public void InvalidValue_Newtonsoft()
        {
            var imp = new InMemoryPublisher();
            using var test = FunctionTester.Create<Startup>();

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .ConfigureServices(sc => sc.ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>())
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest/products", new { id = "A", price = 1.99m })))
                .AssertBadRequest()
                .AssertErrors(new ApiError("Name", "'Name' must not be empty."));

            Assert.AreEqual(0, imp.GetNames().Length);
        }

        [Test]
        public void Success()
        {
            using var test = FunctionTester.Create<Startup>();
            var imp = new InMemoryPublisher(test.Logger);

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest/products", new { id = "A", name = "B", price = 1.99m })))
                .AssertAccepted();

            Assert.AreEqual(1, imp.GetNames().Length);
            Assert.AreEqual("test-queue", imp.GetNames().First());
            var events = imp.GetEvents("test-queue");
            Assert.AreEqual(1, events.Count());
            ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, events[0].Value);
        }

        [Test]
        public void Success_Newtonsoft()
        {
            using var test = FunctionTester.Create<Startup>();
            var imp = new InMemoryPublisher(test.Logger, new CoreEx.Newtonsoft.Json.JsonSerializer());

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .ConfigureServices(sc => sc.ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>())
                .HttpTrigger<HttpTriggerPublishFunction>()
                .Run(f => f.RunAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest/products", new { id = "A", name = "B", price = 1.99m })))
                .AssertAccepted();

            Assert.AreEqual(1, imp.GetNames().Length);
            Assert.AreEqual("test-queue", imp.GetNames().First());
            var events = imp.GetEvents("test-queue");
            Assert.AreEqual(1, events.Count());
            ObjectComparer.Assert(new Product { Id = "A", Name = "B", Price = 1.99m }, events[0].Value);
        }
    }
}