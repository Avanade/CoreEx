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
            var v1 = await new int[] { 1 }.Validate("value").Configure(c => c.Collection(2)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must have at least 2 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await new int[] { 1 }.Validate("value").Configure(c => c.Collection(1)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new int[] { 1, 2, 3 }.Validate("value").Configure(c => c.Collection(maxCount: 2)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must not exceed 2 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await new int[] { 1, 2 }.Validate("value").Configure(c => c.Collection(maxCount: 2)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await ((int[]?)null).Validate("value").Configure(c => c.Collection(1)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await Array.Empty<int>().Validate("value").Configure(c => c.Collection(1)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must have at least 1 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await new int[] { 1, 2, 3 }.Validate("value").Configure(c => c.Collection()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_MinCount()
        {
            var v1 = await new List<int> { 1 }.Validate("value").Configure(c => c.Collection(2)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value must have at least 2 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Item()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await Array.Empty<TestItem>().Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new TestItem[] { new() }.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv))).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Identifier is required."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value[0].Id"));
            });
        }

        [Test]
        public async Task Validate_ItemInt()
        {
            var iv = Validator.CreateFor<int>(r => r.Text("Number").CompareValue(CompareOperator.LessThanEqual, 5));

            var v1 = await new int[] { 1, 2, 3, 4, 5 }.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new int[] { 6, 2, 3, 4, 5 }.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv))).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Number must be less than or equal to 5."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value[0]"));
            });
        }

        [Test]
        public async Task Validate_ItemInt2()
        {
            var iv = Validator.CreateFor<int>(r => r.Text("Number").LessThanOrEqualTo(5));

            var v1 = await new int[] { 1, 2, 3, 4, 5 }.Validate("value").Configure(c => c.Collection(iv)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new int[] { 6, 2, 3, 4, 5 }.Validate("value").Configure(c => c.Collection(iv)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Number must be less than or equal to 5."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value[0]"));
            });
        }

        [Test]
        public async Task Validate_Item_Null()
        {
            var v1 = await new List<TestItem?> { new() }.Validate("value").Configure(c => c.Collection()).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new List<TestItem?>() { null }.Validate("value").Configure(c => c.Collection()).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value contains one or more items that are not specified."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            v1 = await new List<TestItem?> { null }.Validate("value").Configure(c => c.Collection(allowNullItems: true)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_Item_Duplicates()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await Array.Empty<TestItem>().Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck(x => x.Id))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var tis = new TestItem[] { new() { Id = "ABC", Text = "Abc" }, new() { Id = "DEF", Text = "Def" }, new() { Id = "GHI", Text = "Ghi" } };

            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck(x => x.Id))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            tis[2].Id = "ABC";
            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck(x => x.Id))).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value contains duplicates; Identifier 'ABC' specified more than once."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Item_Duplicates_PrimaryKey()
        {
            var iv = Validator.Create<TestItem2>().HasProperty(x => x.Part1, p => p.Mandatory()).HasProperty(x => x.Part2, p => p.Mandatory());

            var v1 = await Array.Empty<TestItem2>().Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck())).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var tis = new TestItem2[] { new() { Part1 = "ABC", Part2 = 1, Text = "Abc" }, new() { Part1 = "DEF", Part2 = 1, Text = "Def" }, new() { Part1 = "GHI", Part2 = 1, Text = "Ghi" } };

            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck())).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            tis[2].Part1 = "ABC";
            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck())).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value contains duplicates; Primary Key specified more than once."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Item_Duplicates_Identifier()
        {
            var iv = Validator.Create<TestItem>().HasProperty(x => x.Id, p => p.Mandatory());

            var v1 = await Array.Empty<TestItem>().Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck(true))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var tis = new TestItem[] { new() { Id = "ABC", Text = "Abc" }, new() { Id = "DEF", Text = "Def" }, new() { Id = "GHI", Text = "Ghi" } };

            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck(true))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            tis[2].Id = "ABC";
            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create(iv).DuplicateCheck(true))).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Value contains duplicates; Identifier 'ABC' specified more than once."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Validate_Item_Duplicates_Identifier2()
        {
            var v1 = await Array.Empty<TestItem3>().Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create<TestItem3>().DuplicateCheck(true))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var tis = new TestItem3[] { new() { Id = 1.ToGuid() }, new() { Id = 2.ToGuid() }, new() { Id = 3.ToGuid() } };

            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create<TestItem3>().DuplicateCheck(true))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            tis[2].Id = 1.ToGuid();
            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create<TestItem3>().DuplicateCheck(true))).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo($"Value contains duplicates; Identifier '{1.ToGuid()}' specified more than once."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            tis[2].Id = Guid.Empty;
            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create<TestItem3>().DuplicateCheck(true))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_Item_Duplicates_IgnoreInitial()
        {
            var v1 = await Array.Empty<TestItem3>().Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create<TestItem3>().DuplicateCheck(true))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            var tis = new TestItem3[] { new() { Id = Guid.Empty }, new() { Id = 2.ToGuid() }, new() { Id = Guid.Empty } };

            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create<TestItem3>().DuplicateCheck(true))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            tis[2].Id = 2.ToGuid();
            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create<TestItem3>().DuplicateCheck(true))).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo($"Value contains duplicates; Identifier '{2.ToGuid()}' specified more than once."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("value"));
            });

            tis[2].Id = Guid.Empty;
            v1 = await tis.Validate("value").Configure(c => c.Collection(item: CollectionRuleItem.Create<TestItem3>().DuplicateCheck(true))).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_Ints_MinCount()
        {
            var v1 = await new int[] { 1, 2, 3, 4 }.Validate(name: "Array").Configure(c => c.MinimumCount(4)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new int[] { 1, 2, 3, 4 }.Validate(name: "Array").Configure(c => c.MinimumCount(5)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Array must have at least 5 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Array"));
            });
        }

        [Test]
        public async Task Validate_Ints2_MaxCount()
        {
            var v1 = await new int[] { 1, 2, 3, 4 }.Validate(name: "Array").Configure(c => c.MaximumCount(5)).ValidateAsync();
            Assert.That(v1.HasErrors, Is.False);

            v1 = await new int[] { 1, 2, 3, 4 }.Validate(name: "Array").Configure(c => c.MaximumCount(3)).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(v1.HasErrors, Is.True);
                Assert.That(v1.Messages!, Has.Count.EqualTo(1));
                Assert.That(v1.Messages![0].Text, Is.EqualTo("Array must not exceed 3 item(s)."));
                Assert.That(v1.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(v1.Messages[0].Property, Is.EqualTo("Array"));
            });
        }

        public class TestItem3 : IIdentifier<Guid>
        {
            public Guid Id { get; set; }
        }
    }
}