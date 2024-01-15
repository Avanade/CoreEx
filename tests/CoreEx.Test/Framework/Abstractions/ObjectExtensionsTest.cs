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
            Assert.That(ex!.ParamName, Is.EqualTo("aussie"));
        }

        [Test]
        public void ThrowIfNull_WhenNotNull()
        {
            string aussie = "Aussie";
            Assert.That(aussie.ThrowIfNull(), Is.EqualTo(aussie));
        }

        [Test]
        public void Adjust_Value_NonNullable()
        {
            var p = new Person();
            var p2 = p.Adjust(x => x.Name = "Babs");
            Assert.Multiple(() =>
            {
                Assert.That(p.Name, Is.EqualTo("Babs"));
                Assert.That(p2.Name, Is.EqualTo("Babs"));
            });
        }

        [Test]
        public void Adjust_Value_Nullable()
        {
            Person? p = null;
            p.Adjust(x => x.Name = "Babs");
            Assert.That(p, Is.Null);
        }

        [Test]
        public void Adjust_Value_Nullable_With_Value()
        {
            Person? p = new();
            var p2 = p.Adjust(x => x.Name = "Babs");
            Assert.That(p, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(p.Name, Is.EqualTo("Babs"));
                Assert.That(p2.Name, Is.EqualTo("Babs"));
            });
        }

        public class Person { public string? Name { get; set; } }
    }
}