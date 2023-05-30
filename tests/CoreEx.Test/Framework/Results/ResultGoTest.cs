using Azure.Storage.Blobs.Models;
using CoreEx.Results;
using CoreEx.TestFunction;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultGoTest
    {
        [Test]
        public void Go_No_Args()
        {
            var r = Result.Go();
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public void Go_With_Action()
        {
            var r = Result.Go(() => { });
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public void Go_With_Result_Func()
        {
            var r = Result.Go(() => Result.Fail("Test"));
            Assert.IsTrue(r.IsFailure);
        }

        [Test]
        public void Go_Value_Func()
        {
            var r = Result.Go(() => 1);
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public void Go_Value_Result_Func()
        {
            var r = Result.Go(() => Result.Ok(1));
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public void Go_Value_No_Args()
        {
            var r = Result.Go<int>();
            Assert.AreEqual(Result<int>.None, r);
        }

        /* Go_Async */

        [Test]
        public async Task GoAsync_With_Action()
        {
            var r = await Result.GoAsync(() => Task.CompletedTask);
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public async Task GoAsync_With_Result_Func()
        {
            var r = await Result.GoAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r.IsFailure);
        }

        [Test]
        public async Task GoAsync_Value_Func()
        {
            var r = await Result.GoAsync<int>(() => Task.FromResult(1));
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public async Task GoAsync_Value_Result_Func()
        {
            var r = await Result.GoAsync(() => Task.FromResult(Result.Ok(1)));
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public void GoFromAsync_Http_Result_OK()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Get, "test").Respond.With("{\"Name\":\"Steve\"}", HttpStatusCode.OK);

            using var test = FunctionTester.Create<Startup>();
            var r = test.ReplaceHttpClientFactory(mcf)
                .Type<BackendHttpClient>()
                .Run(hc => Result.GoFromAsync<Person>(async () => await hc.GetAsync<Person>("test")))
                .AssertSuccess();

            Assert.AreEqual("Steve", r.Result.Value.Name);
        }

        public class Person
        {
            public string? Name { get; set; }
        }
    }
}