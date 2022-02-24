using NUnit.Framework;

namespace CoreEx.Test.Framework
{
    [TestFixture]
    public class CompositeKeyTest
    {
        [Test]
        public void DefaultKey()
        {
            var ck = new CompositeKey();
            Assert.IsNull(ck.Args);
            Assert.IsTrue(ck.IsInitial);

            Assert.IsTrue(ck == new CompositeKey());
            Assert.IsFalse(ck == new CompositeKey(1));

            Assert.IsFalse(ck != new CompositeKey());
            Assert.IsTrue(ck != new CompositeKey(1));

            Assert.AreEqual(ck.GetHashCode(), new CompositeKey().GetHashCode());
            Assert.AreNotEqual(ck.GetHashCode(), new CompositeKey(1).GetHashCode());
        }

        [Test]
        public void SpecifiedKey()
        {
            var ck0 = new CompositeKey(0);
            Assert.IsNotNull(ck0.Args);
            Assert.AreEqual(1, ck0.Args.Length);
            Assert.IsTrue(ck0.IsInitial);

            var ck1 = new CompositeKey(1);
            Assert.IsNotNull(ck1.Args);
            Assert.AreEqual(1, ck1.Args.Length);
            Assert.IsFalse(ck1.IsInitial);

            Assert.IsTrue(ck0 == new CompositeKey(0));
            Assert.IsTrue(ck1 == new CompositeKey(1));
            Assert.IsFalse(ck0 == ck1);

            Assert.AreEqual(ck0.GetHashCode(), new CompositeKey(0).GetHashCode());
            Assert.AreEqual(ck1.GetHashCode(), new CompositeKey(1).GetHashCode());
            Assert.AreNotEqual(ck0.GetHashCode(), ck1.GetHashCode());
        }

        [Test]
        public void KeyComparisons()
        {
            Assert.IsFalse(new CompositeKey() == new CompositeKey(null));
            Assert.IsFalse(new CompositeKey("A") == new CompositeKey("A", null));
            Assert.IsFalse(new CompositeKey(1, "A") == new CompositeKey("A", 1));
        }
    }
}