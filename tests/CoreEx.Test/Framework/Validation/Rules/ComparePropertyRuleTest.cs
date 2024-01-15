using CoreEx.Entities;
using CoreEx.Validation;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class ComparePropertyRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v = Validator.Create<TestData>()
                .HasProperty(x => x.DateA, p => p.CompareValue(CompareOperator.GreaterThan, new DateTime(1950, 1, 1), "Minimum"))
                .HasProperty(x => x.DateB, p => p.CompareProperty(CompareOperator.GreaterThanEqual, y => y.DateA));

            // Date B will be bad.
            var v1 = await v.ValidateAsync(new TestData { DateA = new DateTime(2000, 1, 1), DateB = new DateTime(1999, 1, 1) });
            Assert.That(v1, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Date B must be greater than or equal to Date A."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("DateB"));
            });

            // Date B should not validate as dependent DateA has already failed.
            var v2 = await v.ValidateAsync(new TestData { DateA = new DateTime(1949, 1, 1), DateB = new DateTime(1939, 1, 1) });
            Assert.That(v2, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(v2.HasErrors, Is.True);
                Assert.That(v2.Messages!, Has.Count.EqualTo(1));
                Assert.That(v2.Messages![0].Text, Is.EqualTo("Date A must be greater than Minimum."));
                Assert.That(v2.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v2.Messages[0].Property, Is.EqualTo("DateA"));
            });

            // All is a-ok.
            var v3 = await v.ValidateAsync(new TestData { DateA = new DateTime(2001, 1, 1), DateB = new DateTime(2001, 1, 1) });
            Assert.That(v3, Is.Not.Null);
            Assert.That(v3.HasErrors, Is.False);
        }
    }
}
