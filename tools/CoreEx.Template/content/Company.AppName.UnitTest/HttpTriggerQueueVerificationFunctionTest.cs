using CoreEx.Events;
using Company.AppName.Business.External.Contracts;
using Company.AppName.Functions;
using NUnit.Framework;
using System.Net.Http;
using UnitTestEx.NUnit;

namespace Company.AppName.UnitTest
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

            Assert.AreEqual(1, imp.GetNames().Length);
            var e = imp.GetEvents("pendingVerifications");
            Assert.AreEqual(1, e.Length);
            ObjectComparer.Assert(new EmployeeVerificationRequest { Name = "Wendy", Age = 37, Gender = "F" }, e[0].Value);
        }
    }
}