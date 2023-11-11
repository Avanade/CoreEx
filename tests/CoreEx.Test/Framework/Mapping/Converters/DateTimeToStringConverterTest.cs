using CoreEx.Mapping.Converters;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Mapping.Converters
{
    [TestFixture]
    public class DateTimeToStringConverterTest
    {
        [Test]
        public void ConvertToDestination()
        {
            var dt = new DateTime(2022, 11, 28, 13, 09, 42, 987, DateTimeKind.Unspecified);
            var val = new DateTimeToStringConverter("yyyy-MMM-dd").ToDestination.Convert(dt);
            Assert.AreEqual("2022-Nov-28", val);
        }

        [Test]
        public void ConvertToSource()
        {
            var dt = new DateTimeToStringConverter("yyyy-MMM-dd").ToSource.Convert("2022-Nov-28");
            Assert.AreEqual(new DateTime(2022, 11, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), dt);
        }
    }
}