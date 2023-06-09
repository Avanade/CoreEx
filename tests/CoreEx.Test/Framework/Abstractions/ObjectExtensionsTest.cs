using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Abstractions
{
    [TestFixture]
    public class ObjectExtensionsTest
    {
        [Test]
        public void ThrowIfNull_WhenNull()
        {
            string aussie = null!;
            var ex = Assert.Throws<ArgumentNullException>(() => aussie.ThrowIfNull());
            Assert.AreEqual("aussie", ex!.ParamName);
        }

        [Test]
        public void ThrowIfNull_WhenNotNull()
        {
            string aussie = "Aussie";
            Assert.AreEqual(aussie, aussie.ThrowIfNull());
        }
    }
}