using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class CompareValuesRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await 1.Validate("value").CompareValues(new int[] { 1, 2 }).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await 1.Validate("value").CompareValues(new int[] { 2, 3 }).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_String()
        {
            var v1 = await "A".Validate("value").CompareValues(new string[] { "A", "B" }).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await "C".Validate("value").CompareValues(new string[] { "A", "B" }).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value is invalid."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await "a".Validate("value").CompareValues(new string[] { "A", "B" }, true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_String_Override()
        {
            var v = new Validator<CompareClass>().HasProperty(x => x.Option, p => p.CompareValues(new string[] { "A", "B" }, true, true));

            var cc = new CompareClass();
            var v1 = await v.ValidateAsync(cc);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.False);
                Assert.That(cc.Option, Is.EqualTo(null));
            });

            cc.Option = "A";
            v1 = await v.ValidateAsync(cc);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.False);
                Assert.That(cc.Option, Is.EqualTo("A"));
            });

            cc.Option = "a";
            v1 = await v.ValidateAsync(cc);
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.False);
                Assert.That(cc.Option, Is.EqualTo("A"));
            });
        }

        public class CompareClass
        {
            public string? Option { get; set; }
        }
    }
}