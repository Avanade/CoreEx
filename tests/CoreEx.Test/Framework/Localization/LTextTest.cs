using CoreEx.Localization;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Localization
{
    [TestFixture]
    public class LTextTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => TextProvider.SetTextProvider(new NullTextProvider());

        [Test]
        public void Null_ToString()
        {
            var l = new LText();
            string t = l;
            Assert.AreEqual(null, t);
            Assert.AreEqual(null, l.ToString());
        }

        [Test]
        public void Key_ToString()
        {
            var l = new LText("key");
            string t = l;
            Assert.AreEqual("key", t);
            Assert.AreEqual("key", l.ToString());
        }

        [Test]
        public void NumericKey_ToString()
        {
            var l = new LText(451);
            string t = l;
            Assert.AreEqual("000451", t);
            Assert.AreEqual("000451", l.ToString());
        }

        [Test]
        public void FallBack_ToString()
        {
            var l = new LText("key", "fallback");
            string t = l;
            Assert.AreEqual("fallback", t);
            Assert.AreEqual("fallback", l.ToString());
        }
    }
}