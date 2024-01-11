using CoreEx.TestFunctionIso;
using UnitTestEx.NUnit;

namespace CoreEx.Test.TestFunctionIso
{
    [TestFixture]
    public class HttpFunctionTest
    {
        [Test]
        public void Get()
        {
            using var test = FunctionTester.Create<Startup>();
            test.HttpTrigger<HttpFunction>()
                .Run(f => f.Run(test.CreateHttpRequest(HttpMethod.Get, "https://unittest/blah"), "blah"))
                .AssertOK()
                .AssertJson("{\"message\":\"Hello blah\"}");
        }
    }
}