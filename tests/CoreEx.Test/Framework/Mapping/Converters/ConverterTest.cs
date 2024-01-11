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
            Assert.Multiple(() =>
            {
                Assert.That(converter.ToDestination.Convert("123"), Is.EqualTo(123));
                Assert.That(converter.ToSource.Convert(123), Is.EqualTo("123"));
            });
        }
    }
}