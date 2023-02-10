using CoreEx.Mapping.Converters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEx.Test.Framework.Mapping.Converters
{
    [TestFixture]
    public class TypeToStringConverterTest
    {
        private const string GuidString = "382c74c3-721d-4f34-80e5-57657b6cbc27";
        private readonly Guid GuidValue = new Guid(GuidString);

        [Test]
        public void Convert()
        {
            Assert.That(TypeToStringConverter<Guid>.Default.ToDestination.Convert(GuidValue), Is.EqualTo(GuidString));
            Assert.That(TypeToStringConverter<Guid>.Default.ToDestination.Convert(null!), Is.EqualTo(Guid.Empty.ToString()));
            Assert.That(TypeToStringConverter<Guid?>.Default.ToDestination.Convert(GuidValue), Is.EqualTo(GuidString));
            Assert.That(TypeToStringConverter<Guid?>.Default.ToDestination.Convert(null), Is.Null);

            Assert.That(TypeToStringConverter<Guid>.Default.ToSource.Convert(GuidString), Is.EqualTo(GuidValue));
            Assert.That(TypeToStringConverter<Guid>.Default.ToSource.Convert(null), Is.EqualTo(Guid.Empty));
            Assert.That(TypeToStringConverter<Guid?>.Default.ToSource.Convert(GuidString), Is.EqualTo(GuidValue));
            Assert.That(TypeToStringConverter<Guid?>.Default.ToSource.Convert(null), Is.Null);

            Assert.That(TypeToStringConverter<int>.Default.ToSource.Convert("123"), Is.EqualTo(123));

            Assert.That(TypeToStringConverter<TestOption>.Default.ToDestination.Convert(TestOption.Abc), Is.EqualTo("Abc"));
            Assert.That(TypeToStringConverter<TestOption>.Default.ToSource.Convert("Abc"), Is.EqualTo(TestOption.Abc));
            Assert.That(TypeToStringConverter<TestOption>.Default.ToSource.Convert(null), Is.EqualTo(TestOption.None));
        }

        public enum TestOption
        {
            None = 0,
            Abc = 1
        }
    }
}