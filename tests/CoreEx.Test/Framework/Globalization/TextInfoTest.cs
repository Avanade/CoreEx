using CoreEx.Globalization;
using NUnit.Framework;
using System.Globalization;

namespace CoreEx.Test.Framework.Globalization
{
    [TestFixture]
    public class TextInfoTest
    {
        [Test]
        public void ToCasing_None() => Assert.AreEqual("AbCd", CultureInfo.InvariantCulture.TextInfo.ToCasing("AbCd", TextInfoCasing.None));

        [Test]
        public void ToCasing_Lower() => Assert.AreEqual("abcd", CultureInfo.InvariantCulture.TextInfo.ToCasing("AbCd", TextInfoCasing.Lower));

        [Test]
        public void ToCasing_Upper() => Assert.AreEqual("ABCD", CultureInfo.InvariantCulture.TextInfo.ToCasing("AbCd", TextInfoCasing.Upper));

        [Test]
        public void ToCasing_Title() => Assert.AreEqual("The Quick BROWN Fox.", CultureInfo.InvariantCulture.TextInfo.ToCasing("the qUick BROWN fox.", TextInfoCasing.Title));
    }
}