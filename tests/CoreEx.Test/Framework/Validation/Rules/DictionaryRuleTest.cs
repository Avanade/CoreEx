using NUnit.Framework;
using CoreEx.Validation;
using System.Collections.Generic;
using CoreEx.Entities;
using CoreEx.Validation.Rules;
using System.Threading.Tasks;
using static CoreEx.Test.Framework.Validation.ValidatorTest;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class DictionaryRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var v1 = await new Dictionary<string, string> { { "k1", "v1" } }.Validate("Dict").Dictionary(2).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Dict must have at least 2 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Dict", v1.Messages[0].Property);

            v1 = await new Dictionary<string, string> { { "k1", "v1" } }.Validate("Dict").Dictionary(1).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" }, { "k3", "v3" } }.Validate("Dict").Dictionary(maxCount: 2).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Dict must not exceed 2 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Dict", v1.Messages[0].Property);

            v1 = await new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" }, { "k3", "v3" } }.Validate("Dict").Dictionary(maxCount: 3).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            //v1 = await ((int[])null).Validate().Collection(1).RunAsync();
            v1 = await ((Dictionary<string, string>?)null).Validate().Collection(1).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            //v1 = await new int[0].Validate().Collection(1).RunAsync();
            v1 = await new Dictionary<string, string> { }.Validate("Dict").Dictionary(1).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Dict must have at least 1 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Dict", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Value()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await new Dictionary<string, TestItem>().Validate("Dict").Dictionary(item: DictionaryRuleItem.Create<string, TestItem>(value: iv)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new Dictionary<string, TestItem> { { "k1", new TestItem() } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create<string, TestItem>(value: iv)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Identifier is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Dict[k1].Id", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Null_Value()
        {
            var v1 = await new Dictionary<string, TestItem?> { { "k1", new TestItem() } }.Validate("Dict").Dictionary().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new Dictionary<string, TestItem?> { { "k1", null } }.Validate("Dict").Dictionary().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Dict contains one or more values that are not specified.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Dict", v1.Messages[0].Property);

            v1 = await new Dictionary<string, TestItem?> { { "k1", null } }.Validate("Dict").Dictionary(allowNullValues: true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task Validate_Ints()
        {
            var v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 }, { "k3", 3 }, { "k4", 4 } }.Validate("Dict").Dictionary(maxCount: 4).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 }, { "k3", 3 }, { "k4", 4} }.Validate("Dict").Dictionary(maxCount: 3).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Dict must not exceed 3 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Dict", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Key()
        {
            var kv = Validator.CreateCommon<string?>(r => r.Text("Key").Mandatory().String(2));

            var v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create<string?, int>(kv)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2x", 2 } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create<string?, int>(kv)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Key must not exceed 2 characters in length.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Dict[k2x]", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_KeyAndValue()
        {
            var kv = Validator.CreateCommon<string?>(r => r.Text("Key").Mandatory().String(2));
            var vv = Validator.CreateCommon<int>(r => r.Mandatory().CompareValue(CompareOperator.LessThanEqual, 10));

            var v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create(kv, vv)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new Dictionary<string, int> { { "k1", 11 }, { "k2x", 2 } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create(kv, vv)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(2, v1.Messages!.Count);
            Assert.AreEqual("Value must be less than or equal to 10.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Dict[k1]", v1.Messages[0].Property);
            Assert.AreEqual("Key must not exceed 2 characters in length.", v1.Messages[1].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[1].Type);
            Assert.AreEqual("Dict[k2x]", v1.Messages[1].Property);
        }
    }
}