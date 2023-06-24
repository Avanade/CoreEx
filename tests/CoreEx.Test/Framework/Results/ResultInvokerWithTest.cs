using CoreEx.Invokers;
using CoreEx.Results;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultInvokerWithTest
    {
        [Test]
        public void Manager_Sycn()
        {
            var r = Result.Go().Manager(this).With(r => r);
            Assert.That(r.IsSuccess, Is.True);

            r = Result.Go().Manager(this).With(r => Result.Fail("boo hoo"));
            Assert.That(r.Error, Is.TypeOf<BusinessException>().And.Message.EqualTo("boo hoo"));
        }
    }
}