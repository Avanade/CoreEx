using CoreEx.Entities;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework
{
    [TestFixture]
    public class CompositeKeyTest
    {
        [Test]
        public void DefaultKey()
        {
            var ck = new CompositeKey();
            Assert.IsEmpty(ck.Args);
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

        [Test]
        public void KeyCopy()
        {
            var ck0 = new CompositeKey("Xyz");
            var ck1 = ck0;

            Assert.AreEqual(ck0, ck1);

        }

        [Test]
        public void KeyToString()
        {
            var ck = new CompositeKey();
            Assert.AreEqual(string.Empty, ck.ToString());

            ck = new CompositeKey(null);
            Assert.AreEqual(string.Empty, ck.ToString());

            ck = new CompositeKey(88);
            Assert.AreEqual("88", ck.ToString());

            ck = new CompositeKey("abc");
            Assert.AreEqual("abc", ck.ToString());

            ck = new CompositeKey("");
            Assert.AreEqual(string.Empty, ck.ToString());

            ck = new CompositeKey(null, null);
            Assert.AreEqual(",", ck.ToString());

            ck = new CompositeKey("text", 'x', short.MinValue, int.MinValue, long.MinValue, ushort.MaxValue, uint.MaxValue, long.MaxValue, Guid.Parse("8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc"),
                new DateTime(1970, 01, 22, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 01, 22, 20, 59, 43, DateTimeKind.Utc), new DateTimeOffset(2000, 01, 22, 20, 59, 43, TimeSpan.FromHours(-8)));

            Assert.AreEqual("text,x,-32768,-2147483648,-9223372036854775808,65535,4294967295,9223372036854775807,8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc,1970-01-22T00:00:00,2000-01-22T20:59:43Z,2000-01-23T04:59:43Z", ck.ToString());

            ck = new CompositeKey("ab@cdef<gh>ij\"kl'mn;op");
            Assert.AreEqual("ab@cdef\\u003Cgh\\u003Eij\\u0022kl\\u0027mn;op", ck.ToString());

            var str = $"line1{Environment.NewLine}line2";
            ck = new CompositeKey(str);
            Assert.AreEqual("line1\\r\\nline2", ck.ToString());

            ck = new CompositeKey(char.MinValue);
            Assert.AreEqual("\\u0000", ck.ToString());

            ck = new CompositeKey("a", "b");
            Assert.AreEqual("a/b", ck.ToString('/'));
        }

        [Test]
        public void KeySerializeDeserialize()
        {
            var ck = new CompositeKey();
            Assert.AreEqual("null", ck.ToJsonString());
            ck = CompositeKey.Create(ck.ToJsonString());
            Assert.IsTrue(ck == new CompositeKey());

            ck = new CompositeKey(88);
            Assert.AreEqual("[{\"int\":88}]", ck.ToJsonString());
            ck = CompositeKey.Create(ck.ToJsonString());
            Assert.IsTrue(ck == new CompositeKey(88));

            ck = new CompositeKey((int?)null);
            Assert.AreEqual("[{}]", ck.ToJsonString());
            ck = CompositeKey.Create(ck.ToJsonString());
            Assert.IsTrue(ck == new CompositeKey((int?)null));

            ck = new CompositeKey((int?)88);
            Assert.AreEqual("[{\"int\":88}]", ck.ToJsonString());
            ck = CompositeKey.Create(ck.ToJsonString());
            Assert.IsTrue(ck == new CompositeKey(88));

            ck = new CompositeKey("text", 'x', short.MinValue, int.MinValue, long.MinValue, ushort.MaxValue, uint.MaxValue, long.MaxValue, Guid.Parse("8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc"),
                new DateTime(1970, 01, 22, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 01, 22, 20, 59, 43, DateTimeKind.Utc), new DateTimeOffset(2000, 01, 22, 20, 59, 43, TimeSpan.FromHours(-8)));

            Assert.AreEqual("[{\"string\":\"text\"},{\"char\":\"x\"},{\"short\":-32768},{\"int\":-2147483648},{\"long\":-9223372036854775808},{\"ushort\":65535},{\"uint\":4294967295},{\"long\":9223372036854775807},{\"guid\":\"8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc\"},{\"date\":\"1970-01-22T00:00:00\"},{\"date\":\"2000-01-22T20:59:43Z\"},{\"date\":\"2000-01-23T04:59:43Z\"}]", ck.ToJsonString());

            var ck2 = ck;
            ck = CompositeKey.Create(ck.ToJsonString());
            Assert.IsTrue(ck == ck2);
        }

        [Test]
        public void KeyDeserializeErrors()
        {
            Assert.Throws<ArgumentException>(() => CompositeKey.Create("{}"));
            Assert.Throws<ArgumentException>(() => CompositeKey.Create("[[]]"));
            Assert.Throws<ArgumentException>(() => CompositeKey.Create("[{\"xxx\":1}]"));
            Assert.Throws<ArgumentException>(() => CompositeKey.Create("[{\"int\":\"x\"}]"));
        }
    }
}