using NUnit.Framework;
using CoreEx.Validation;
using System.Collections.Generic;
using CoreEx.Entities;
using CoreEx.Validation.Rules;
using System.Threading.Tasks;
using static CoreEx.Test.Framework.Validation.ValidatorTest;
using System;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class CollectionRuleTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate_Errors()
        {
            var v1 = await new int[] { 1 }.Validate().Collection(2).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must have at least 2 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await new int[] { 1 }.Validate().Collection(1).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new int[] { 1, 2, 3 }.Validate().Collection(maxCount: 2).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must not exceed 2 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await new int[] { 1, 2 }.Validate().Collection(maxCount: 2).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await ((int[]?)null).Validate().Collection(1).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new int[0].Validate().Collection(1).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must have at least 1 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await new int[] { 1, 2, 3 }.Validate().Collection().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task Validate_MinCount()
        {
            var v1 = await new List<int> { 1 }.Validate().Collection(2).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value must have at least 2 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Item()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await new TestItem[0].Validate().Collection(item: CollectionRuleItem.Create(iv)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new TestItem[] { new TestItem() }.Validate().Collection(item: CollectionRuleItem.Create(iv)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Identifier is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value[0].Id", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_ItemInt()
        {
            var iv = Validator.CreateCommon<int>(r => r.Text("Number").CompareValue(CompareOperator.LessThanEqual, 5));

            var v1 = await new int[] { 1, 2, 3, 4, 5 }.Validate().Collection(item: CollectionRuleItem.Create(iv)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new int[] { 6, 2, 3, 4, 5 }.Validate().Collection(item: CollectionRuleItem.Create(iv)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Number must be less than or equal to 5.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value[0]", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Item_Null()
        {
            var v1 = await new List<TestItem?> { new TestItem() }.Validate().Collection().ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new List<TestItem?>() { null }.Validate().Collection().ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value contains one or more items that are not specified.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            v1 = await new List<TestItem?> { null }.Validate().Collection(allowNullItems: true).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task Validate_Item_Duplicates()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await new TestItem[0].Validate().Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck(x => x.Id)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            var tis = new TestItem[] { new TestItem { Id = "ABC", Text = "Abc" }, new TestItem { Id = "DEF", Text = "Def" }, new TestItem { Id = "GHI", Text = "Ghi" } };

            v1 = await tis.Validate().Collection(item:  CollectionRuleItem.Create(iv).DuplicateCheck(x => x.Id)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            tis[2].Id = "ABC";
            v1 = await tis.Validate().Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck(x => x.Id)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value contains duplicates; Identifier 'ABC' specified more than once.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Item_Duplicates_PrimaryKey()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await new TestItem[0].Validate().Collection(item: CollectionRuleItem.Create(iv).PrimaryKeyDuplicateCheck()).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            var tis = new TestItem[] { new TestItem { Id = "ABC", Text = "Abc" }, new TestItem { Id = "DEF", Text = "Def" }, new TestItem { Id = "GHI", Text = "Ghi" } };

            v1 = await tis.Validate().Collection(item: CollectionRuleItem.Create(iv).PrimaryKeyDuplicateCheck()).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            tis[2].Id = "ABC";
            v1 = await tis.Validate().Collection(item: CollectionRuleItem.Create(iv).PrimaryKeyDuplicateCheck()).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value contains duplicates; Primary Key 'ABC' specified more than once.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Item_Duplicates_Identifier()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await new TestItem[0].Validate().Collection(item: CollectionRuleItem.Create(iv).IdentifierDuplicateCheck(true)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            var tis = new TestItem[] { new TestItem { Id = "ABC", Text = "Abc" }, new TestItem { Id = "DEF", Text = "Def" }, new TestItem { Id = "GHI", Text = "Ghi" } };

            v1 = await tis.Validate().Collection(item: CollectionRuleItem.Create(iv).IdentifierDuplicateCheck(true)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            tis[2].Id = "ABC";
            v1 = await tis.Validate().Collection(item: CollectionRuleItem.Create(iv).IdentifierDuplicateCheck(true)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Value contains duplicates; Identifier 'ABC' specified more than once.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);
        }

        [Test]
        public async Task Validate_Item_Duplicates_Identifier2()
        {
            var v1 = await new TestItem2[0].Validate().Collection(item: CollectionRuleItem.Create<TestItem2>().IdentifierDuplicateCheck(true)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            var tis = new TestItem2[] { new TestItem2 { Id = 1.ToGuid() }, new TestItem2 { Id = 2.ToGuid() }, new TestItem2 { Id = 3.ToGuid() } };

            v1 = await tis.Validate().Collection(item: CollectionRuleItem.Create<TestItem2>().IdentifierDuplicateCheck(true)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            tis[2].Id = 1.ToGuid();
            v1 = await tis.Validate().Collection(item: CollectionRuleItem.Create<TestItem2>().IdentifierDuplicateCheck(true)).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual($"Value contains duplicates; Identifier '{1.ToGuid()}' specified more than once.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("value", v1.Messages[0].Property);

            tis[2].Id = Guid.Empty;
            v1 = await tis.Validate().Collection(item: CollectionRuleItem.Create<TestItem2>().IdentifierDuplicateCheck(true)).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);
        }

        [Test]
        public async Task Validate_Ints()
        {
            var v1 = await new int[] { 1, 2, 3, 4 }.Validate(name: "Array").Collection(maxCount: 5).ValidateAsync();
            Assert.IsFalse(v1.HasErrors);

            v1 = await new int[] { 1, 2, 3, 4 }.Validate(name: "Array").Collection(maxCount: 3).ValidateAsync();
            Assert.IsTrue(v1.HasErrors);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Array must not exceed 3 item(s).", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Array", v1.Messages[0].Property);
        }

        public class TestItem2 : IIdentifier<Guid>
        {
            public Guid Id { get; set; }
        }
    }
}