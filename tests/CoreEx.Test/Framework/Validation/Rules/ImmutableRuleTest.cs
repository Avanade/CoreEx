// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/Beef

using NUnit.Framework;
using CoreEx.Validation;
using CoreEx.Entities;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class ImmutableRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_Value()
        {
            var v1 = await 123.Validate().Immutable(x => true).RunAsync();
            Assert.IsFalse(v1.HasError);
            
            v1 = await 123.Validate().Immutable(x => false).RunAsync();
            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is not allowed to change; please reset value.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);

            v1 = await 123.Validate().Immutable(() => true).RunAsync();
            Assert.IsFalse(v1.HasError);

            v1 = await 123.Validate().Immutable(() => false).RunAsync();
            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value is not allowed to change; please reset value.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value", v1.Messages[0].Property);
        }
    }
}
