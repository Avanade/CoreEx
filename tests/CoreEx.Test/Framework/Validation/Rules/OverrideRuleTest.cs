using CoreEx.Validation;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class OverrideRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public void Validate_Value()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await 123.Validate().Override(456).ValidateAsync());
        }
    }
}
