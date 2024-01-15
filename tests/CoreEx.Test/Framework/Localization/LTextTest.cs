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
            Assert.Multiple(() =>
            {
                Assert.That(t, Is.EqualTo(null));
                Assert.That(l.ToString(), Is.EqualTo(null));
            });
        }

        [Test]
        public void Key_ToString()
        {
            var l = new LText("key");
            string t = l;
            Assert.Multiple(() =>
            {
                Assert.That(t, Is.EqualTo("key"));
                Assert.That(l.ToString(), Is.EqualTo("key"));
            });
        }

        [Test]
        public void NumericKey_ToString()
        {
            var l = new LText(451);
            string t = l;
            Assert.Multiple(() =>
            {
                Assert.That(t, Is.EqualTo("000451"));
                Assert.That(l.ToString(), Is.EqualTo("000451"));
            });
        }

        [Test]
        public void FallBack_ToString()
        {
            var l = new LText("key", "fallback");
            string t = l;
            Assert.Multiple(() =>
            {
                Assert.That(t, Is.EqualTo("fallback"));
                Assert.That(l.ToString(), Is.EqualTo("fallback"));
            });
        }
    }
}