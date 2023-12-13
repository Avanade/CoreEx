using NUnit.Framework;

namespace CoreEx.Test.Framework.Mapping.Converters
{
    [TestFixture]
    public class ConverterTest
    {
        [Test]
        public void Convert()
        {
            var converter = CoreEx.Mapping.Converters.Converter.Create<string, int>(s => int.Parse(s), i => i.ToString());
            Assert.AreEqual(123, converter.ToDestination.Convert("123"));
            Assert.AreEqual("123", converter.ToSource.Convert(123));
        }
    }
}