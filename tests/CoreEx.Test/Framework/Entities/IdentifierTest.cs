using CoreEx.Entities;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Entities
{
    [TestFixture]
    public class IdentifierTest
    {
        [Test]
        public void VerifyBaseIdProperty()
        {
            var p = new Person { Id = 88 };
            Assert.AreEqual(88, p.Id);

            var iii = (IIdentifier<int>)p;
            Assert.AreEqual(88, iii.Id);

            var ii = (IIdentifier)p;
            Assert.AreEqual(88, ii.Id);

            ii.Id = 99;
            Assert.AreEqual(99, ii.Id);
            Assert.AreEqual(99, iii.Id);
            Assert.AreEqual(99, p.Id);
        }

        private class Person : IIdentifier<int>
        {
            public int Id { get; set; }
        }
    }
}