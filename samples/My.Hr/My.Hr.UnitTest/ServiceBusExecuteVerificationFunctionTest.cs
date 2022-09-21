using CoreEx.Events;
using System;
using System.Net.Http;
using Microsoft.Azure.WebJobs.ServiceBus;
using Moq;
using My.Hr.Business.External.Contracts;
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
            var evr = new EmployeeVerificationRequest { Name = "Wendy", Age = 37, Gender = "F" };
            var sbm = test.CreateServiceBusMessage(evr);
            var sba = new Mock<ServiceBusMessageActions>();

            var mcf = MockHttpClientFactory.Create();
            var agify = mcf.CreateClient("Agify");
            var nationalize = mcf.CreateClient("Nationalize");
            var genderize = mcf.CreateClient("Genderize");

            agify.Request(HttpMethod.Get, $"https://api.agify.mock.io/?name={evr.Name}")
                .Respond.WithJson(new
                {
                    age = 64,
                    count = 82293,
                    name = evr.Name
                });
            nationalize.Request(HttpMethod.Get, $"https://api.nationalize.mock.io/?name={evr.Name}")
                .Respond.WithJson(new
                {
                    country = new[]{
                        new {
                            country_Id= "SV",
                            probability= 0.07477553
                        },
                        new {
                            country_Id= "GT",
                            probability= 0.07223318
                        },
                        new {
                            country_Id= "NL",
                            probability= 0.067494206
                        }},
                    name = evr.Name
                });
            genderize.Request(HttpMethod.Get, $"https://api.genderize.mock.io/?name={evr.Name}")
                .Respond.WithJson(new
                {
                    count = 176697,
                    gender = "female",
                    name = evr.Name,
                    probability = 0.97
                });

            test.ReplaceScoped<IEventPublisher>(_ => imp)
                .ReplaceHttpClientFactory(mcf)
                .ServiceBusTrigger<ServiceBusExecuteVerificationFunction>()
                .Run(f => f.RunAsync(sbm, sba.Object))
                .AssertSuccess();

            Assert.AreEqual(1, imp.GetNames().Length);
            var e = imp.GetEvents("verificationResults");
            Assert.AreEqual(1, e.Length);
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                ObjectComparer.Assert(UnitTestEx.Resource.GetJsonValue<EmployeeVerificationResponse>("VerificationResult.Unix.json"), e[0].Value);
            else
                ObjectComparer.Assert(UnitTestEx.Resource.GetJsonValue<EmployeeVerificationResponse>("VerificationResult.Win32.json"), e[0].Value);
        }
    }
}