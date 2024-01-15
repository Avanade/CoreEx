﻿using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class EmailRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await ((string?)null).Validate().Email().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "blah@.com".Validate().Email().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "blah.com".Validate().Email().ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await "@blah.com".Validate().Email().ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await "blah@".Validate().Email().ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await $"mynameis@{new string('x', 250)}.com".Validate().Email().ValidateAsync();
            Assert.That(v1.HasErrors, Is.True);

            v1 = await $"mynameis@{new string('x', 250)}.com".Validate().Email(null).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await $"mynameis@{new string('x', 500)}.com".Validate().Email(null).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }
    }
}