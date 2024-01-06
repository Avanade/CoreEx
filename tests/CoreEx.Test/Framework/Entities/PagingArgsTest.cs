using CoreEx.Entities;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Entities
{
    [TestFixture]
    public class PagingArgsTest
    {
        [Test]
        public void CreateSkipAndTake()
        {
            var pa = PagingArgs.CreateSkipAndTake(10, 20);
            Assert.That(pa.Skip, Is.EqualTo(10));
            Assert.That(pa.Take, Is.EqualTo(20));
            Assert.That(pa.Option, Is.EqualTo(PagingOption.SkipAndTake));
        }

        [Test]
        public void CreatePageAndSize()
        {
            var pa = PagingArgs.CreatePageAndSize(10, 20);
            Assert.That(pa.Page, Is.EqualTo(10));
            Assert.That(pa.Size, Is.EqualTo(20));
            Assert.That(pa.Option, Is.EqualTo(PagingOption.PageAndSize));
        }

        [Test]
        public void CreateTokenAndTake()
        {
            var pa = PagingArgs.CreateTokenAndTake("blah-blah", 20);
            Assert.That(pa.Token, Is.EqualTo("blah-blah"));
            Assert.That(pa.Take, Is.EqualTo(20));
            Assert.That(pa.Option, Is.EqualTo(PagingOption.TokenAndTake));
        }
    }
}