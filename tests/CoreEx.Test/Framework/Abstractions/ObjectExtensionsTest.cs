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

        [Test]
        public void Adjust_Value_NonNullable()
        {
            var p = new Person();
            var p2 = p.Adjust(x => x.Name = "Babs");
            Assert.AreEqual("Babs", p.Name);
            Assert.AreEqual("Babs", p2.Name);
        }

        [Test]
        public void Adjust_Value_Nullable()
        {
            Person? p = null;
            p.Adjust(x => x.Name = "Babs");
            Assert.IsNull(p);
        }

        [Test]
        public void Adjust_Value_Nullable_With_Value()
        {
            Person? p = new();
            var p2 = p.Adjust(x => x.Name = "Babs");
            Assert.IsNotNull(p);
            Assert.AreEqual("Babs", p.Name);
            Assert.AreEqual("Babs", p2.Name);
        }

        public class Person { public string? Name { get; set; } }
    }
}