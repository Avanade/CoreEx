using CoreEx.Validation;
using NUnit.Framework;
using CoreEx.Entities;
using System.Threading.Tasks;
using FluentAssertions;
using static CoreEx.Test.Framework.Validation.Rules.EnumValueRuleTest;

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
            var v1 = await 1.Validate().CompareValues(new int[] { 1, 2 }).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await 1.Validate().CompareValues(new int[] { 2, 3 }).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_String()
        {
            var v1 = await "A".Validate().CompareValues(new string[] { "A", "B" }).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await "C".Validate().CompareValues(new string[] { "A", "B" }).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is invalid.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await "a".Validate().CompareValues(new string[] { "A", "B" }, true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task Validate_String_Override()
        {
            var v = new Validator<CompareClass>().HasProperty(x => x.Option, p => p.CompareValues(new string[] { "A", "B" }, true, true));

            var cc = new CompareClass();
            var v1 = await v.ValidateAsync(cc);
            Assert.IsFalse(v1.HasErrors);
            Assert.AreEqual(null, cc.Option);

            cc.Option = "A";
            v1 = await v.ValidateAsync(cc);
            Assert.IsFalse(v1.HasErrors);
            Assert.AreEqual("A", cc.Option);

            cc.Option = "a";
            v1 = await v.ValidateAsync(cc);
            Assert.IsFalse(v1.HasErrors);
            Assert.AreEqual("A", cc.Option);
        }

        public class CompareClass
        {
            public string? Option { get; set; }
        }
    }
}