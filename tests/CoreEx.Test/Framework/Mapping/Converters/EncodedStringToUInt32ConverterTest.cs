using CoreEx.Mapping.Converters;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Mapping.Converters
{
    [TestFixture]
    public class EncodedStringToUInt32ConverterTest
    {
        [Test]
        public void ConvertToSource()
        {
            uint i = 4020;
            var val = EncodedStringToUInt32Converter.Default.ToSource.Convert(i);
            Assert.That(val, Is.EqualTo("tA8AAA=="));
        }

        [Test]
        public void ConvertToDestination()
        {
            var i = EncodedStringToUInt32Converter.Default.ToDestination.Convert("tA8AAA==");
            Assert.That(i, Is.EqualTo(4020));
        }
    }
}