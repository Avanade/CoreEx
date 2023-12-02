using NUnit.Framework;
using UnitTestEx;

namespace My.Hr.UnitTest
{
    [SetUpFixture]
    public class OneTimeSetUp
    {
        [OneTimeSetUp]
        public void OneTime()
        {
            // Configure the TestSetUp to use the CoreEx-based Newtonsoft.Json.JsonSerialize; needed to enable the deserialization of RefData.
            TestSetUp.Default.JsonSerializer = new CoreEx.Newtonsoft.Json.JsonSerializer().ToUnitTestEx();
        }
    }
}