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
            Assert.That(p.Id, Is.EqualTo(88));

            var iii = (IIdentifier<int>)p;
            Assert.That(iii.Id, Is.EqualTo(88));

            var ii = (IIdentifier)p;
            Assert.That(ii.Id, Is.EqualTo(88));

            ii.Id = 99;
            Assert.Multiple(() =>
            {
                Assert.That(ii.Id, Is.EqualTo(99));
                Assert.That(iii.Id, Is.EqualTo(99));
                Assert.That(p.Id, Is.EqualTo(99));
            });
        }

        private class Person : IIdentifier<int>
        {
            public int Id { get; set; }
        }
    }
}