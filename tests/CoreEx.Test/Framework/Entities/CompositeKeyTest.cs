using CoreEx.Entities;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Entities
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
            Assert.IsFalse(new CompositeKey() == new CompositeKey(null!));
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
        public void KeyToString_And_CreateFromString()
        {
            var ck = new CompositeKey();
            Assert.AreEqual(string.Empty, ck.ToString());
            ck = CompositeKey.CreateFromString(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(0));

            int? iv = null;
            ck = new CompositeKey(iv);
            Assert.AreEqual(string.Empty, ck.ToString());
            ck = CompositeKey.CreateFromString<int?>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.Null);

            ck = new CompositeKey(88);
            Assert.AreEqual("88", ck.ToString());
            ck = CompositeKey.CreateFromString<int>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo(88));

            ck = new CompositeKey("abc");
            Assert.AreEqual("abc", ck.ToString());
            ck = CompositeKey.CreateFromString<string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo("abc"));

            ck = new CompositeKey("");
            Assert.AreEqual(string.Empty, ck.ToString());
            ck = CompositeKey.CreateFromString<string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.Null);

            ck = new CompositeKey(null, null);
            Assert.AreEqual(",", ck.ToString());
            ck = CompositeKey.CreateFromString<string, string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(2));
            Assert.That(ck.Args[0], Is.Null);
            Assert.That(ck.Args[1], Is.Null);

            ck = new CompositeKey("text", 'x', short.MinValue, int.MinValue, long.MinValue, ushort.MaxValue, uint.MaxValue, ulong.MaxValue, Guid.Parse("8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc"),
                new DateTime(1970, 01, 22, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 01, 22, 20, 59, 43, DateTimeKind.Utc), new DateTimeOffset(2000, 01, 22, 20, 59, 43, TimeSpan.FromHours(-8)));

            Assert.AreEqual("text,x,-32768,-2147483648,-9223372036854775808,65535,4294967295,18446744073709551615,8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc,1970-01-22T00:00:00,2000-01-22T20:59:43Z,2000-01-22T20:59:43-08:00", ck.ToString());
            ck = CompositeKey.CreateFromString(ck.ToString(), new Type[] { typeof(string), typeof(char), typeof(short), typeof(int), typeof(long), typeof(ushort), typeof(uint), typeof(ulong), typeof(Guid), typeof(DateTime), typeof(DateTime), typeof(DateTimeOffset) });
            Assert.That(ck.Args, Has.Length.EqualTo(12));
            Assert.That(ck.Args[0], Is.EqualTo("text"));
            Assert.That(ck.Args[1], Is.EqualTo('x'));
            Assert.That(ck.Args[2], Is.EqualTo(short.MinValue));
            Assert.That(ck.Args[3], Is.EqualTo(int.MinValue));
            Assert.That(ck.Args[4], Is.EqualTo(long.MinValue));
            Assert.That(ck.Args[5], Is.EqualTo(ushort.MaxValue));
            Assert.That(ck.Args[6], Is.EqualTo(uint.MaxValue));
            Assert.That(ck.Args[7], Is.EqualTo(ulong.MaxValue));
            Assert.That(ck.Args[8], Is.EqualTo(Guid.Parse("8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc")));
            Assert.That(ck.Args[9], Is.EqualTo(new DateTime(1970, 01, 22, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(ck.Args[10], Is.EqualTo(new DateTime(2000, 01, 22, 20, 59, 43, DateTimeKind.Utc)));
            Assert.That(ck.Args[11], Is.EqualTo(new DateTimeOffset(2000, 01, 22, 20, 59, 43, TimeSpan.FromHours(-8))));

            ck = new CompositeKey("ab@cdef<gh>ij\"kl'mn;op,"); // Included comma is escaped.
            Assert.AreEqual("ab@cdef\\u003Cgh\\u003Eij\\u0022kl\\u0027mn;op\\u002c", ck.ToString());
            ck = CompositeKey.CreateFromString<string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo("ab@cdef<gh>ij\"kl'mn;op,"));

            var str = $"line1{Environment.NewLine}line2";
            ck = new CompositeKey(str);
            Assert.AreEqual($"line1{(Environment.NewLine == "\n" ? "\\n" : "\\r\\n")}line2", ck.ToString());
            ck = CompositeKey.CreateFromString<string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo($"line1{Environment.NewLine}line2"));

            ck = new CompositeKey(char.MinValue);
            Assert.AreEqual("\\u0000", ck.ToString());
            ck = CompositeKey.CreateFromString<char>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo(char.MinValue));

            ck = new CompositeKey("a", "b", "c");
            Assert.AreEqual("a/b/c", ck.ToString('/'));
            ck = CompositeKey.CreateFromString<string, string, string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(3));
            Assert.That(ck.Args[0], Is.EqualTo("a"));
            Assert.That(ck.Args[1], Is.EqualTo("b"));
            Assert.That(ck.Args[2], Is.EqualTo("c"));
        }

        [Test]
        public void KeyToString_And_CreateFromString_Perf()
        {
            CompositeKey ck;
            for (int i = 0; i < 1000; i++)
            {
                ck = new CompositeKey("text", 'x', short.MinValue, int.MinValue, long.MinValue, ushort.MaxValue, uint.MaxValue, ulong.MaxValue, Guid.Parse("8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc"),
    new DateTime(1970, 01, 22, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 01, 22, 20, 59, 43, DateTimeKind.Utc), new DateTimeOffset(2000, 01, 22, 20, 59, 43, TimeSpan.FromHours(-8)));
                _ = CompositeKey.CreateFromString(ck.ToString(), new Type[] { typeof(string), typeof(char), typeof(short), typeof(int), typeof(long), typeof(ushort), typeof(uint), typeof(ulong), typeof(Guid), typeof(DateTime), typeof(DateTime), typeof(DateTimeOffset) });
            }
        }

        [Test]
        public void KeySerializeDeserialize()
        {
            var ck = new CompositeKey();
            Assert.AreEqual("null", ck.ToJsonString());
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.IsTrue(ck == new CompositeKey());

            ck = new CompositeKey(88);
            Assert.AreEqual("[{\"int\":88}]", ck.ToJsonString());
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.IsTrue(ck == new CompositeKey(88));

            ck = new CompositeKey((int?)null);
            Assert.AreEqual("[{}]", ck.ToJsonString());
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.IsTrue(ck == new CompositeKey((int?)null));

            ck = new CompositeKey((int?)88);
            Assert.AreEqual("[{\"int\":88}]", ck.ToJsonString());
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.IsTrue(ck == new CompositeKey(88));

            ck = new CompositeKey("text", 'x', short.MinValue, int.MinValue, long.MinValue, ushort.MaxValue, uint.MaxValue, long.MaxValue, Guid.Parse("8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc"),
                new DateTime(1970, 01, 22, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 01, 22, 20, 59, 43, DateTimeKind.Utc), new DateTimeOffset(2000, 01, 22, 20, 59, 43, TimeSpan.FromHours(-8)));

            Assert.AreEqual("[{\"string\":\"text\"},{\"char\":\"x\"},{\"short\":-32768},{\"int\":-2147483648},{\"long\":-9223372036854775808},{\"ushort\":65535},{\"uint\":4294967295},{\"long\":9223372036854775807},{\"guid\":\"8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc\"},{\"date\":\"1970-01-22T00:00:00\"},{\"date\":\"2000-01-22T20:59:43Z\"},{\"offset\":\"2000-01-22T20:59:43-08:00\"}]", ck.ToJsonString());

            var ck2 = ck;
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.IsTrue(ck == ck2);
        }

        [Test]
        public void KeyDeserializeErrors()
        {
            Assert.Throws<ArgumentException>(() => CompositeKey.CreateFromJson("{}"));
            Assert.Throws<ArgumentException>(() => CompositeKey.CreateFromJson("[[]]"));
            Assert.Throws<ArgumentException>(() => CompositeKey.CreateFromJson("[{\"xxx\":1}]"));
            Assert.Throws<ArgumentException>(() => CompositeKey.CreateFromJson("[{\"int\":\"x\"}]"));
        }
    }
}