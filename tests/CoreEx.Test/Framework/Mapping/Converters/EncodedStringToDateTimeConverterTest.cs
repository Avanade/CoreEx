using CoreEx.Mapping.Converters;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Mapping.Converters
{
    [TestFixture]
    public class EncodedStringToDateTimeConverterTest
    {
        [Test]
        public void ConvertToSource()
        {
            var dt = new DateTime(2022, 11, 28, 13, 09, 42, 987, DateTimeKind.Utc);
            var val = EncodedStringToDateTimeConverter.Default.ToSource.Convert(dt);
            Assert.That(val, Is.EqualTo("sImk0EHR2kg="));
        }

        [Test]
        public void ConvertToDestination()
        {
            var dt = EncodedStringToDateTimeConverter.Default.ToDestination.Convert("sImk0EHR2kg=");
            Assert.That(dt, Is.EqualTo(new DateTime(2022, 11, 28, 13, 09, 42, 987, DateTimeKind.Utc)));
        }
    }
}