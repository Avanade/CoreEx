using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Http
{
    [TestFixture]
    public class HttpClientRetryTest
    {
        [Test]
        public void Retry_Should_Retry_50x_StatusCodes()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.InternalServerError);
                s.Respond().With(HttpStatusCode.VariantAlsoNegotiates);
                s.Respond().With(HttpStatusCode.BadGateway);
                s.Respond().With(HttpStatusCode.LoopDetected);
            });

            using var test = FunctionTester.Create<Startup>();
            var result = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.WithRetry(3, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }));
            mcf.VerifyAll();

            result.Result.StatusCode.Should().Be(HttpStatusCode.LoopDetected);
        }

        [Test]
        public void Retry_Should_Retry_50x_StatusCodes_And_HttpRequestException()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.InternalServerError);
                s.Respond().With(HttpStatusCode.VariantAlsoNegotiates, _ => throw new HttpRequestException("Unit test request failure (whoop whoop)."));
                s.Respond().With(HttpStatusCode.BadGateway);
                s.Respond().With(HttpStatusCode.LoopDetected);
            });

            using var test = FunctionTester.Create<Startup>();
            var result = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.WithRetry(3, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }));
            mcf.VerifyAll();

            result.Result.StatusCode.Should().Be(HttpStatusCode.LoopDetected);
        }

        [Test]
        public void Client_Should_Retry_Server_50xErrors_And_Return_Success()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.InternalServerError);
                s.Respond().With(HttpStatusCode.VariantAlsoNegotiates);
                s.Respond().WithJson("{ }");
            });

            using var test = FunctionTester.Create<Startup>();
            var result = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.WithRetry(3, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }))
                .AssertSuccess();
            mcf.VerifyAll();
        }

        [Test]
        public void Client_Should_RetrySocketException_And_Return_Result()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.WithSequence(s =>
            {
                s.Respond().With(HttpStatusCode.InternalServerError, _ => throw new SocketException());
                s.Respond().With(HttpStatusCode.InternalServerError, _ => throw new SocketException());
                s.Respond().WithJson("{ }");
            });

            using var test = FunctionTester.Create<Startup>();
            var result = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.WithRetry(3, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }))
                .AssertSuccess();
            mcf.VerifyAll();
        }

        [Test]
        public void Client_Should_RetryAfterTimeout_And_ReturnData_When_CallEventuallySuccessful()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.WithSequence(s =>
            {
                s.Respond().Delay(TimeSpan.FromSeconds(60)).With(HttpStatusCode.InternalServerError);
                s.Respond().Delay(TimeSpan.FromSeconds(60)).With(HttpStatusCode.InternalServerError);
                s.Respond().WithJson("{ }");
            });

            using var test = FunctionTester.Create<Startup>();
            var sw = Stopwatch.StartNew();

            // Act
            test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.WithTimeout(TimeSpan.FromSeconds(1)).WithRetry(3, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }))
                .AssertSuccess();

            // Assert
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
            mcf.VerifyAll();
        }

        [Test]
        public void Client_Should_RetryAfterTimeout_And_Return_TimeoutException_When_AllCallsFail()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.WithSequence(s =>
            {
                s.Respond().Delay(TimeSpan.FromSeconds(60)).With(HttpStatusCode.InternalServerError);
                s.Respond().Delay(TimeSpan.FromSeconds(60)).With(HttpStatusCode.InternalServerError);
                s.Respond().Delay(TimeSpan.FromSeconds(60)).With(HttpStatusCode.InternalServerError);
                s.Respond().Delay(TimeSpan.FromSeconds(60)).With(HttpStatusCode.InternalServerError);
            });

            using var test = FunctionTester.Create<Startup>();
            var sw = Stopwatch.StartNew();

            // Act
            var result = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.WithTimeout(TimeSpan.FromSeconds(1)).WithRetry(3, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }))
                .AssertException<TransientException>();

            // Assert
            result.Exception!.InnerException.Should().BeOfType<TimeoutException>();
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20), because: "There is 1 http call and 3 retries, with built in delay between each retry.");
            mcf.VerifyAll();
        }

        [Test]
        public void Client_Should_RetryAfterError_And_Return_Result_When_AllCallsFail()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "test").WithJsonBody(new Product { Id = "abc", Name = "banana", Price = 0.99m }).Respond.WithSequence(s =>
            {
                s.Respond().Delay(TimeSpan.FromSeconds(1)).With(HttpStatusCode.InternalServerError);
                s.Respond().Delay(TimeSpan.FromSeconds(1)).With(HttpStatusCode.InternalServerError);
                s.Respond().Delay(TimeSpan.FromSeconds(1)).With(HttpStatusCode.InternalServerError);
                s.Respond().Delay(TimeSpan.FromSeconds(1)).With(HttpStatusCode.InternalServerError);
            });

            using var test = FunctionTester.Create<Startup>();
            var sw = Stopwatch.StartNew();

            // Act
            var result = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(f => f.WithTimeout(Timeout.InfiniteTimeSpan).WithRetry(3, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }));

            // Assert
            result.Result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20), because: "There is 1 http call and 3 retries, with built in delay between each retry.");
            mcf.VerifyAll();
        }

        [Test]
        public void Client_Should_ThrowTransientException_When_SocketException()
        {
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Backend", "https://backend/").Request(HttpMethod.Post, "test").WithAnyBody().Respond.With(HttpStatusCode.OK, _ => throw new SocketException());

            using var test = FunctionTester.Create<Startup>()
            .ConfigureServices(sc => mcf.Replace(sc));

            test.Type<BackendHttpClient>().Run(dhc => dhc.ThrowTransientException().PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }))
            .AssertException<TransientException>();
        }

        [Test]
        public async Task Client_Should_Retry_When_Server_Returns_429()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Backend", "https://backend/").Request(HttpMethod.Post, "test").WithAnyBody().Respond.WithSequence(seq =>
            {
                seq.Respond().With(statusCode: HttpStatusCode.TooManyRequests, response => response.Headers.Add("Retry-After", "5"));
                seq.Respond().WithJson("{ }", HttpStatusCode.OK);
            });

            using var test = FunctionTester.Create<Startup>();
            var services = test.ConfigureServices(sc => mcf.Replace(sc)).Services;
            var dhc = services.GetService<BackendHttpClient>()!;

            // Act
            var result = await dhc.ThrowTransientException().WithRetry(1, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m });

            // Assert
            result.IsSuccess.Should().BeTrue();
            mcf.VerifyAll();
        }

        [Test]
        public async Task Client_Call_Should_RespectMaxDelay_When_Server_Returns_429()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Backend", "https://backend/").Request(HttpMethod.Post, "test").WithAnyBody().Respond.WithSequence(seq =>
            {
                seq.Respond().With(statusCode: HttpStatusCode.TooManyRequests, response => response.Headers.Add("Retry-After", "60"));
                seq.Respond().WithJson("{ }", HttpStatusCode.OK);
            });

            using var test = FunctionTester.Create<Startup>();
            var services = test.ConfigureServices(sc => mcf.Replace(sc)).Services;
            var dhc = services.GetService<BackendHttpClient>()!;

            // Act
            var result = await dhc
                .ThrowTransientException()
                .WithMaxRetryDelay(TimeSpan.FromSeconds(5))
                .WithRetry(1, 0)
                .PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m });

            // Assert
            result.IsSuccess.Should().BeTrue();
            mcf.VerifyAll();
        }

        [Test]
        public async Task Client_Call_Should_Retry_When_Server_Returns_429_With_Date()
        {
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Backend", "https://backend/").Request(HttpMethod.Post, "test").WithAnyBody().Respond.WithSequence(seq =>
            {
                seq.Respond().With(statusCode: HttpStatusCode.TooManyRequests, response => response.Headers.Add("Retry-After", DateTime.UtcNow.AddSeconds(5).ToString("R")));
                seq.Respond().WithJson("{ }", HttpStatusCode.OK);
            });

            using var test = FunctionTester.Create<Startup>();
            var services = test.ConfigureServices(sc => mcf.Replace(sc)).Services;
            var dhc = services.GetService<BackendHttpClient>()!;

            // Act
            var result = await dhc.ThrowTransientException().WithRetry(1, 0).PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m });

            // Assert
            result.IsSuccess.Should().BeTrue();
            mcf.VerifyAll();
        }

        [Test]
        public void Client_Call_Should_ThrowTransientException_When_429_Response()
        {
            // Arrange
            var mcf = MockHttpClientFactory.Create();
            mcf.CreateClient("Backend", "https://backend/").Request(HttpMethod.Post, "test").WithAnyBody().Respond
                .With(statusCode: HttpStatusCode.TooManyRequests, response => response.Headers.Add("Retry-After", "5"));

            using var test = FunctionTester.Create<Startup>();
            var services = test.ConfigureServices(sc => mcf.Replace(sc)).Services;
            var dhc = services.GetService<BackendHttpClient>();

            // Act
            test.Type<BackendHttpClient>()
                .Run(x => x.ThrowTransientException().PostAsync("test", new Product { Id = "abc", Name = "banana", Price = 0.99m }))
                .AssertException<TransientException>();

            // Assert
            mcf.VerifyAll();
        }
    }
}