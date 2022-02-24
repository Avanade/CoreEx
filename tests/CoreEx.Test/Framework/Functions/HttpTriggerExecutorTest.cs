using CoreEx.Events;
using CoreEx.Functions;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Functions
{
    [TestFixture]
    public class HttpTriggerExecutorTest
    {
        [Test]
        public void RunAsync_Core_Success()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => Task.FromResult((IActionResult)new OkResult())))
                .ToActionResultAssertor()
                .AssertOK();
        }

        [Test]
        public void RunAsync_Core_ValidationException1()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => ThrowValidationException(true)))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .Assert("A data validation error occurred.");
        }

        [Test]
        public void RunAsync_Core_ValidationException2()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => ThrowValidationException(false)))
                .ToActionResultAssertor()
                .AssertBadRequest()
                .AssertErrors(new ApiError("Test", "Invalid."));
        }

        [Test]
        public void RunAsync_Core_TransientException()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => ThrowTransientException()))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public void RunAsync_Core_EventPublisherException()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => ThrowEventPublisherException()))
                .ToActionResultAssertor()
                .Assert(HttpStatusCode.InternalServerError)
                .Assert(new EventPublisherDataError[] { new EventPublisherDataError { Index = 1, Message = "Not sent." } });
        }

        private Task<IActionResult> ThrowValidationException(bool basic)
        {
            if (basic)
                throw new ValidationException();

            var msd = new ModelStateDictionary();
            msd.AddModelError("Test", "Invalid.");
            throw new ValidationException(msd);
        }

        private Task<IActionResult> ThrowTransientException() => throw new TransientException();

        private Task<IActionResult> ThrowEventPublisherException() => throw new EventPublisherException(new EventPublisherDataError[] { new EventPublisherDataError { Index = 1, Message = "Not sent." } });

        [Test]
        public void RunAsync_NoResult()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }), (p) => Task.CompletedTask, true, HttpStatusCode.NoContent))
                .ToActionResultAssertor()
                .AssertNoContent();
        }

        [Test]
        public void RunWithResultAsync_NoRequest()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunWithResultAsync(test.CreateHttpRequest(HttpMethod.Post, "https://unittest"), () => Task.FromResult(new Product { Id = "A", Name = "B", Price = 1.99m }), HttpStatusCode.OK))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new Product { Id = "A", Name = "B", Price = 1.99m });
        }

        [Test]
        public void RunWithResultAsync_WithRequest()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<HttpTriggerExecutor>()
                .Run(f => f.RunWithResultAsync<Product, Product>(test.CreateJsonHttpRequest(HttpMethod.Post, "https://unittest", new { id = "A", name = "B", price = 1.99m }), (p) => Task.FromResult(p), true, HttpStatusCode.OK))
                .ToActionResultAssertor()
                .AssertOK()
                .Assert(new Product { Id = "A", Name = "B", Price = 1.99m });
        }
    }
}