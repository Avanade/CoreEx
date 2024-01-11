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
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Dict must have at least 2 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Dict"));
            });

            v1 = await new Dictionary<string, string> { { "k1", "v1" } }.Validate("Dict").Dictionary(1).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" }, { "k3", "v3" } }.Validate("Dict").Dictionary(maxCount: 2).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Dict must not exceed 2 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Dict"));
            });

            v1 = await new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" }, { "k3", "v3" } }.Validate("Dict").Dictionary(maxCount: 3).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            //v1 = await ((int[])null).Validate().Collection(1).RunAsync();
            v1 = await ((Dictionary<string, string>?)null).Validate().Collection(1).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            //v1 = await new int[0].Validate().Collection(1).RunAsync();
            v1 = await new Dictionary<string, string> { }.Validate("Dict").Dictionary(1).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Dict must have at least 1 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Dict"));
            });
        }

        [Test]
        public async Task Validate_Value()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await new Dictionary<string, TestItem>().Validate("Dict").Dictionary(item: DictionaryRuleItem.Create<string, TestItem>(value: iv)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new Dictionary<string, TestItem> { { "k1", new TestItem() } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create<string, TestItem>(value: iv)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Identifier is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Dict[k1].Id"));
            });
        }

        [Test]
        public async Task Validate_Null_Value()
        {
            var v1 = await new Dictionary<string, TestItem?> { { "k1", new TestItem() } }.Validate("Dict").Dictionary().ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new Dictionary<string, TestItem?> { { "k1", null } }.Validate("Dict").Dictionary().ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Dict contains one or more values that are not specified."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Dict"));
            });

            v1 = await new Dictionary<string, TestItem?> { { "k1", null } }.Validate("Dict").Dictionary(allowNullValues: true).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_Ints()
        {
            var v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 }, { "k3", 3 }, { "k4", 4 } }.Validate("Dict").Dictionary(maxCount: 4).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 }, { "k3", 3 }, { "k4", 4} }.Validate("Dict").Dictionary(maxCount: 3).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Dict must not exceed 3 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Dict"));
            });
        }

        [Test]
        public async Task Validate_Key()
        {
            var kv = Validator.CreateCommon<string?>(r => r.Text("Key").Mandatory().String(2));

            var v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create<string?, int>(kv)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2x", 2 } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create<string?, int>(kv)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Key must not exceed 2 characters in length."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Dict[k2x]"));
            });
        }

        [Test]
        public async Task Validate_KeyAndValue()
        {
            var kv = Validator.CreateCommon<string?>(r => r.Text("Key").Mandatory().String(2));
            var vv = Validator.CreateCommon<int>(r => r.Mandatory().CompareValue(CompareOperator.LessThanEqual, 10));

            var v1 = await new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create(kv, vv)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new Dictionary<string, int> { { "k1", 11 }, { "k2x", 2 } }.Validate("Dict").Dictionary(item: DictionaryRuleItem.Create(kv, vv)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(2));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must be less than or equal to 10."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Dict[k1]"));
                Assert.That(v1.Messages[1].Text, Is.EqualTo("Key must not exceed 2 characters in length."));
                Assert.That(v1.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[1].Property, Is.EqualTo("Dict[k2x]"));
            });
        }
    }
}