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
            Assert.That(ck.Args, Is.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(ck.IsInitial, Is.True);

                Assert.That(ck, Is.EqualTo(new CompositeKey()));
            });
            Assert.That(ck, Is.Not.EqualTo(new CompositeKey(1)));

            Assert.That(ck, Is.EqualTo(new CompositeKey()));
            Assert.Multiple(() =>
            {
                Assert.That(ck, Is.Not.EqualTo(new CompositeKey(1)));

                Assert.That(new CompositeKey().GetHashCode(), Is.EqualTo(ck.GetHashCode()));
                Assert.That(new CompositeKey(1).GetHashCode(), Is.Not.EqualTo(ck.GetHashCode()));
            });
        }

        [Test]
        public void SpecifiedKey()
        {
            var ck0 = new CompositeKey(0);
            Assert.Multiple(() =>
            {
                Assert.That(ck0.Args, Has.Length.EqualTo(1));
                Assert.That(ck0.IsInitial, Is.True);
            });

            var ck1 = new CompositeKey(1);
            Assert.Multiple(() =>
            {
                Assert.That(ck1.Args, Has.Length.EqualTo(1));
                Assert.That(ck1.IsInitial, Is.False);

                Assert.That(ck0, Is.EqualTo(new CompositeKey(0)));
                Assert.That(ck1, Is.EqualTo(new CompositeKey(1)));
                Assert.That(ck0, Is.Not.EqualTo(ck1));

                Assert.That(new CompositeKey(0).GetHashCode(), Is.EqualTo(ck0.GetHashCode()));
                Assert.That(new CompositeKey(1).GetHashCode(), Is.EqualTo(ck1.GetHashCode()));
                Assert.That(ck1.GetHashCode(), Is.Not.EqualTo(ck0.GetHashCode()));
            });
        }

        [Test]
        public void KeyComparisons()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new CompositeKey(), Is.Not.EqualTo(new CompositeKey(null!)));
                Assert.That(new CompositeKey("A"), Is.Not.EqualTo(new CompositeKey("A", null)));
                Assert.That(new CompositeKey(1, "A"), Is.Not.EqualTo(new CompositeKey("A", 1)));
            });
        }

        [Test]
        public void KeyCopy()
        {
            var ck0 = new CompositeKey("Xyz");
            var ck1 = ck0;

            Assert.That(ck1, Is.EqualTo(ck0));
        }

        [Test]
        public void KeyToString_And_CreateFromString()
        {
            var ck = new CompositeKey();
            Assert.That(ck.ToString(), Is.Null);
            ck = CompositeKey.CreateFromString(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(0));

            int? iv = null;
            ck = new CompositeKey(iv);
            Assert.That(ck.ToString(), Is.EqualTo(string.Empty));
            ck = CompositeKey.CreateFromString<int?>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.Null);

            ck = new CompositeKey(88);
            Assert.That(ck.ToString(), Is.EqualTo("88"));
            ck = CompositeKey.CreateFromString<int>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo(88));

            ck = new CompositeKey("abc");
            Assert.That(ck.ToString(), Is.EqualTo("abc"));
            ck = CompositeKey.CreateFromString<string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo("abc"));

            ck = new CompositeKey("");
            Assert.That(ck.ToString(), Is.EqualTo(string.Empty));
            ck = CompositeKey.CreateFromString<string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.Null);

            ck = new CompositeKey(null, null);
            Assert.That(ck.ToString(), Is.EqualTo(","));
            ck = CompositeKey.CreateFromString<string, string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(ck.Args[0], Is.Null);
                Assert.That(ck.Args[1], Is.Null);
            });

            ck = new CompositeKey("text", 'x', short.MinValue, int.MinValue, long.MinValue, ushort.MaxValue, uint.MaxValue, ulong.MaxValue, Guid.Parse("8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc"),
                new DateTime(1970, 01, 22, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 01, 22, 20, 59, 43, DateTimeKind.Utc), new DateTimeOffset(2000, 01, 22, 20, 59, 43, TimeSpan.FromHours(-8)));

            Assert.That(ck.ToString(), Is.EqualTo("text,x,-32768,-2147483648,-9223372036854775808,65535,4294967295,18446744073709551615,8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc,1970-01-22T00:00:00,2000-01-22T20:59:43Z,2000-01-22T20:59:43-08:00"));
            ck = CompositeKey.CreateFromString(ck.ToString(), new Type[] { typeof(string), typeof(char), typeof(short), typeof(int), typeof(long), typeof(ushort), typeof(uint), typeof(ulong), typeof(Guid), typeof(DateTime), typeof(DateTime), typeof(DateTimeOffset) });
            Assert.That(ck.Args, Has.Length.EqualTo(12));
            Assert.Multiple(() =>
            {
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
            });

            ck = new CompositeKey("ab@cdef<gh>ij\"kl'mn;op,"); // Included comma is escaped.
            Assert.That(ck.ToString(), Is.EqualTo("ab@cdef\\u003Cgh\\u003Eij\\u0022kl\\u0027mn;op\\u002c"));
            ck = CompositeKey.CreateFromString<string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo("ab@cdef<gh>ij\"kl'mn;op,"));

            var str = $"line1{Environment.NewLine}line2";
            ck = new CompositeKey(str);
            Assert.That(ck.ToString(), Is.EqualTo($"line1{(Environment.NewLine == "\n" ? "\\n" : "\\r\\n")}line2"));
            ck = CompositeKey.CreateFromString<string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo($"line1{Environment.NewLine}line2"));

            ck = new CompositeKey(char.MinValue);
            Assert.That(ck.ToString(), Is.EqualTo("\\u0000"));
            ck = CompositeKey.CreateFromString<char>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(1));
            Assert.That(ck.Args[0], Is.EqualTo(char.MinValue));

            ck = new CompositeKey("a", "b", "c");
            Assert.That(ck.ToString('/'), Is.EqualTo("a/b/c"));
            ck = CompositeKey.CreateFromString<string, string, string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(ck.Args[0], Is.EqualTo("a"));
                Assert.That(ck.Args[1], Is.EqualTo("b"));
                Assert.That(ck.Args[2], Is.EqualTo("c"));
            });

            ck = new CompositeKey("a,a", "b,b", "c,c");
            Assert.That(ck.ToString(), Is.EqualTo("a\\u002ca,b\\u002cb,c\\u002cc"));
            ck = CompositeKey.CreateFromString<string, string, string>(ck.ToString());
            Assert.That(ck.Args, Has.Length.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(ck.Args[0], Is.EqualTo("a,a"));
                Assert.That(ck.Args[1], Is.EqualTo("b,b"));
                Assert.That(ck.Args[2], Is.EqualTo("c,c"));
            });
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
            Assert.That(ck.ToJsonString(), Is.EqualTo("null"));
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.That(ck, Is.EqualTo(new CompositeKey()));

            ck = new CompositeKey(88);
            Assert.That(ck.ToJsonString(), Is.EqualTo("[{\"int\":88}]"));
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.That(ck, Is.EqualTo(new CompositeKey(88)));

            ck = new CompositeKey((int?)null);
            Assert.That(ck.ToJsonString(), Is.EqualTo("[null]"));
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.That(ck, Is.EqualTo(new CompositeKey((int?)null)));

            ck = new CompositeKey((int?)88);
            Assert.That(ck.ToJsonString(), Is.EqualTo("[{\"int\":88}]"));
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.That(ck, Is.EqualTo(new CompositeKey(88)));

            ck = new CompositeKey("text", 'x', short.MinValue, int.MinValue, long.MinValue, ushort.MaxValue, uint.MaxValue, long.MaxValue, Guid.Parse("8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc"),
                new DateTime(1970, 01, 22, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2000, 01, 22, 20, 59, 43, DateTimeKind.Utc), new DateTimeOffset(2000, 01, 22, 20, 59, 43, TimeSpan.FromHours(-8)));

            Assert.That(ck.ToJsonString(), Is.EqualTo("[{\"string\":\"text\"},{\"char\":\"x\"},{\"short\":-32768},{\"int\":-2147483648},{\"long\":-9223372036854775808},{\"ushort\":65535},{\"uint\":4294967295},{\"long\":9223372036854775807},{\"guid\":\"8bd5f616-ed6b-4fc5-9cb8-4472cc8955fc\"},{\"datetime\":\"1970-01-22T00:00:00\"},{\"datetime\":\"2000-01-22T20:59:43Z\"},{\"datetimeoffset\":\"2000-01-22T20:59:43-08:00\"}]"));

            var ck2 = ck;
            ck = CompositeKey.CreateFromJson(ck.ToJsonString());
            Assert.That(ck, Is.EqualTo(ck2));
        }

        [Test]
        public void KeyDeserializeErrors()
        {
            Assert.Throws<System.Text.Json.JsonException>(() => CompositeKey.CreateFromJson("{}"));
            Assert.Throws<System.Text.Json.JsonException>(() => CompositeKey.CreateFromJson("[[]]"));
            Assert.Throws<System.Text.Json.JsonException>(() => CompositeKey.CreateFromJson("[{\"xxx\":1}]"));
            Assert.Throws<System.Text.Json.JsonException>(() => CompositeKey.CreateFromJson("[{\"int\":\"x\"}]"));
        }
    }
}