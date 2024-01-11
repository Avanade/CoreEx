using CoreEx.Globalization;
using NUnit.Framework;
using System.Globalization;

namespace CoreEx.Test.Framework.Globalization
{
    [TestFixture]
    public class TextInfoTest
    {
        [Test]
        public void ToCasing_None() => Assert.That(CultureInfo.InvariantCulture.TextInfo.ToCasing("AbCd", TextInfoCasing.None), Is.EqualTo("AbCd"));

        [Test]
        public void ToCasing_Lower() => Assert.That(CultureInfo.InvariantCulture.TextInfo.ToCasing("AbCd", TextInfoCasing.Lower), Is.EqualTo("abcd"));

        [Test]
        public void ToCasing_Upper() => Assert.That(CultureInfo.InvariantCulture.TextInfo.ToCasing("AbCd", TextInfoCasing.Upper), Is.EqualTo("ABCD"));

        [Test]
        public void ToCasing_Title() => Assert.That(CultureInfo.InvariantCulture.TextInfo.ToCasing("the qUick BROWN fox.", TextInfoCasing.Title), Is.EqualTo("The Quick BROWN Fox."));
    }
}