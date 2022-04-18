using CoreEx.Events;
using Microsoft.Azure.WebJobs.ServiceBus;
using Moq;
using My.Hr.Business.ServiceContracts;
using My.Hr.Functions;
using NUnit.Framework;
using UnitTestEx.NUnit;

namespace My.Hr.UnitTest
{
    [TestFixture]
    public class ServiceBusExecuteVerificationFunctionTest
    {
        [Test]
        public void A110_Verify_Success()
        {
            var test = FunctionTester.Create<Startup>();
            var imp = new InMemoryPublisher(test.Logger);
            var sbm = test.CreateServiceBusMessage(new EmployeeVerificationRequest { Name = "Wendy", Age = 37, Gender = "F" });
            var sba = new Mock<ServiceBusMessageActions>();

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .ServiceBusTrigger<ServiceBusExecuteVerificationFunction>()
                .Run(f => f.RunAsync(sbm, sba.Object))
                .AssertSuccess();

            Assert.AreEqual(1, imp.GetNames().Length);
            var e = imp.GetEvents("verificationResults");
            Assert.AreEqual(1, e.Length);
            ObjectComparer.Assert(UnitTestEx.Resource.GetJsonValue<EmployeeVerificationResponse>("VerificationResult.json"), e[0].Value);
        }
    }
}