using CoreEx.Events;
using My.Hr.Business.External.Contracts;
using My.Hr.Functions;
using NUnit.Framework;
using System.Net.Http;
using UnitTestEx.NUnit;

namespace My.Hr.UnitTest
{
    [TestFixture]
    public class HttpTriggerQueueVerificationFunctionTest
    {
        [Test]
        public void A110_Verify_Success()
        {
            var test = FunctionTester.Create<Startup>();
            var imp = new InMemoryPublisher(test.Logger);

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .HttpTrigger<HttpTriggerQueueVerificationFunction>()
                .Run(f => f.RunAsync(test.CreateJsonHttpRequest(HttpMethod.Post, "employee/verify", new EmployeeVerificationRequest { Name = "Wendy", Age = 37, Gender = "F" })))
                .AssertAccepted();

            Assert.That(imp.GetNames(), Has.Length.EqualTo(1));

            var e = imp.GetEvents("pendingVerifications");
            Assert.That(e, Has.Length.EqualTo(1));
            ObjectComparer.Assert(new EmployeeVerificationRequest { Name = "Wendy", Age = 37, Gender = "F" }, e[0].Value);
        }
    }
}