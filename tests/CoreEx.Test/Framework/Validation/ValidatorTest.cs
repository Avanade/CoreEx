using CoreEx.Entities;
using CoreEx.Results;
using CoreEx.Validation;
using CoreEx.Validation.Rules;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation
{
    [TestFixture]
    public class ValidatorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Create_NewValidator()
        {
            var r = await Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().String(10))
                .HasProperty(x => x.CountB, p => p.Mandatory().CompareValue(CompareOperator.GreaterThan, 10))
                .ValidateAsync(new TestData { CountB = 0 });

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);

                Assert.That(r.Messages!, Has.Count.EqualTo(2));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Text is required."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));

                Assert.That(r.Messages[1].Text, Is.EqualTo("Count B must be greater than 10."));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Property, Is.EqualTo("CountB"));
            });
        }

        [Test]
        public async Task Create_NewValidator_WithIncludeBase()
        {
            var v = Validator.Create<TestDataBase>()
                .HasProperty(x => x.Text, p => p.Mandatory().String(10));

            var r = await Validator.Create<TestData>()
                .IncludeBase(v)
                .HasProperty(x => x.CountB, p => p.Mandatory().CompareValue(CompareOperator.GreaterThan, 10))
                .ValidateAsync(new TestData { CountB = 0 });

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);

                Assert.That(r.Messages!, Has.Count.EqualTo(2));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Text is required."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));

                Assert.That(r.Messages[1].Text, Is.EqualTo("Count B must be greater than 10."));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Property, Is.EqualTo("CountB"));
            });
        }

        [Test]
        public async Task Ruleset_UsingValidatorClass()
        {
            var r = await new TestItemValidator().ValidateAsync(new TestItem { Id = "A", Text = "X" });
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Description is invalid."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
            });

            r = await new TestItemValidator().ValidateAsync(new TestItem { Id = "A", Text = "A" });
            Assert.That(r.HasErrors, Is.False);

            r = await new TestItemValidator().ValidateAsync(new TestItem { Id = "B", Text = "X" });
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Description is invalid."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
            });

            r = await new TestItemValidator().ValidateAsync(new TestItem { Id = "B", Text = "B" });
            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task Ruleset_UsingInline()
        {
            var v = Validator.Create<TestItem>()
                .HasRuleSet(x => x.Value!.Id == "A", y =>
                {
                    y.Property(x => x.Text).Mandatory().Must(x => x.Text == "A");
                })
                .HasRuleSet(x => x.Value!.Id == "B", (y) =>
                {
                    y.Property(x => x.Text).Mandatory().Must(x => x.Text == "B");
                });

            var r = await v.ValidateAsync(new TestItem { Id = "A", Text = "X" });
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Description is invalid."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
            });

            r = await v.ValidateAsync(new TestItem { Id = "A", Text = "A" });
            Assert.That(r.HasErrors, Is.False);

            r = await v.ValidateAsync(new TestItem { Id = "B", Text = "X" });
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Description is invalid."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
            });

            r = await v.ValidateAsync(new TestItem { Id = "B", Text = "B" });
            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task CheckJsonNamesUsage()
        {
            var v = Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory())
                .HasProperty(x => x.DateA, p => p.Mandatory())
                .HasProperty(x => x.DateA, p => p.Mandatory());

            var r = await v.ValidateAsync(new TestData(), new ValidationArgs { UseJsonNames = true });
        }

        [Test]
        public async Task Override_OnValidate_WithCheckPredicate()
        {
            var r = await new TestItemValidator2().ValidateAsync(new TestItem(), new ValidationArgs { UseJsonNames = true });
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(2));

                Assert.That(r.Messages![0].Text, Is.EqualTo("Identifier is invalid."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("id"));

                Assert.That(r.Messages[1].Text, Is.EqualTo("Description must not exceed 10 item(s)."));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Property, Is.EqualTo("Text"));
            });
        }

        [Test]
        public async Task Inline_OnValidate_WithWhen()
        {
            var r = await Validator.Create<TestItem>()
                .AdditionalAsync((context, _) =>
                {
                    context.Check(x => x.Text, true, ValidatorStrings.MaxCountFormat, 10);
                    context.Check(x => x.Text, true, ValidatorStrings.MaxCountFormat, 10);
                    return Task.FromResult(Result.Success);
                }).ValidateAsync(new TestItem());

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Description must not exceed 10 item(s)."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
            });
        }

        [Test]
        public async Task Multi_Common_Validator()
        {
            var cv1 = CommonValidator.Create<string?>(v => v.String(5).Must(x => x.Value != "XXXXX"));
            var cv2 = CommonValidator.Create<string?>(v => v.String(2).Must(x => x.Value != "YYY"));

            var vx = Validator.Create<TestItem>()
                .HasProperty(x => x.Id, p => p.Common(cv2))
                .HasProperty(x => x.Text, p => p.Common(cv1));

            var r = await vx.ValidateAsync(new TestItem { Id = "YYY", Text = "XXXXX" });

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public async Task Entity_SubEntity_Mandatory()
        {
            var r = await Validator.Create<TestEntity>()
                .HasProperty(x => x.Items, (p) => p.Mandatory())
                .HasProperty(x => x.Item, (p) => p.Mandatory())
                .ValidateAsync(new TestEntity { Items = null });

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public async Task NonNullableString()
        {
            var v = Validator.Create<TestDataString>().HasProperty(x => x.Name, p => p.Mandatory().String(10));
            var r = await v.ValidateAsync(new TestDataString("a"));
            Assert.That(r, Is.Not.Null);
            Assert.That(r.HasErrors, Is.False);

            r = await v.ValidateAsync(new TestDataString(null!));
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Name is required."));
            });
        }

        public class TestItemValidator : Validator<TestItem>
        {
            public TestItemValidator()
            {
                RuleSet(x => x.Value!.Id == "A", () =>
                {
                    Property(x => x.Text).Mandatory().Must(x => x.Text == "A");
                });

                RuleSet(x => x.Value!.Id == "B", () =>
                {
                    Property(x => x.Text).Mandatory().Must(x => x.Text == "B");
                });
            }
        }

        public class TestItemValidator2 : Validator<TestItem>
        {
            protected override Task<Result> OnValidateAsync(ValidationContext<TestItem> context, CancellationToken ct)
            {
                if (!context.HasError(x => x.Id))
                    context.AddError(x => x.Id, ValidatorStrings.InvalidFormat);

                if (!context.HasError(x => x.Id))
                    Assert.Fail();

                context.Check(x => x.Text, (v) => string.IsNullOrEmpty(v), ValidatorStrings.MaxCountFormat, 10);
                context.Check(x => x.Text, (v) => throw new NotFoundException(), ValidatorStrings.MaxCountFormat, 10);
                return Task.FromResult(Result.Success);
            }
        }

        public class TestEntity
        {
            public List<TestItem>? Items { get; set; } = [];

            public TestItem? Item { get; set; }

            public Dictionary<string, string>? Dict { get; set; }

            public Dictionary<string, TestItem>? Dict2 { get; set; }
        }

        public class TestItem : IIdentifier<string>
        {
            public string? Id { get; set; }

            [JsonPropertyName("Text")]
            [System.ComponentModel.DataAnnotations.Display(Name = "Description")]
            public string? Text { get; set; }
        }

        public class TestItem2 : IPrimaryKey
        {
            public string? Part1 { get; set; }

            public int Part2 { get; set; }

            [JsonPropertyName("Text")]
            [System.ComponentModel.DataAnnotations.Display(Name = "Description")]
            public string? Text { get; set; }

            public CompositeKey PrimaryKey => new(Part1, Part2);

        }

        public class TestDataString(string name)
        {
            public string Name { get; set; } = name;
        }

        [Test]
        public async Task Create_NewValidator_CollectionDuplicate()
        {
            var e = new TestEntity();
            e.Items!.Add(new TestItem { Id = "ABC", Text = "Abc" });
            e.Items.Add(new TestItem { Id = "DEF", Text = "Abc" });
            e.Items.Add(new TestItem { Id = "ABC", Text = "Def" });
            e.Items.Add(new TestItem { Id = "XYZ", Text = "Xyz" });

            var v = Validator.Create<TestItem>();

            var r = await Validator.Create<TestEntity>()
                .HasProperty(x => x.Items, p => p.Collection(item: CollectionRuleItem.Create(v).DuplicateCheck(y => y.Id)))
                .ValidateAsync(e);

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);

                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Items contains duplicates; Identifier 'ABC' specified more than once."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Items"));
            });
        }

        [Test]
        public void ThrowValidationException_NoArgs()
        {
            try
            {
                Validator.Create<TestItem>().ThrowValidationException(x => x.Id, "Some text.");
                Assert.Fail();
            }
            catch (ValidationException vex)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(vex.Messages![0].Text, Is.EqualTo("Some text."));
                    Assert.That(vex.Messages[0].Type, Is.EqualTo(MessageType.Error));
                    Assert.That(vex.Messages[0].Property, Is.EqualTo("Id"));
                });
            }
        }

        [Test]
        public void ThrowValidationException_WithArgs()
        {
            try
            {
                Validator.Create<TestItem>().ThrowValidationException(x => x.Id, "{0} {1} {2} Stuff.", "XXX", "ZZZ");
                Assert.Fail();
            }
            catch (ValidationException vex)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(vex.Messages![0].Text, Is.EqualTo("Identifier XXX ZZZ Stuff."));
                    Assert.That(vex.Messages[0].Type, Is.EqualTo(MessageType.Error));
                    Assert.That(vex.Messages[0].Property, Is.EqualTo("Id"));
                });
            }
        }

        private class TestInject
        {
            public string? Text { get; set; }
            public object? Value { get; set; }
        }

        public class TestInjectChild
        {
            public int Code { get; set; }
        }

        [Test]
        public async Task ManualProperty_Inject()
        {
            var vx = await Validator.Create<TestInject>()
                .HasProperty(x => x.Text, p => p.Mandatory())
                .HasProperty(x => x.Value, p => p.Mandatory().Custom(TestInjectValueValidate))
                .ValidateAsync(new TestInject { Text = "X", Value = new TestInjectChild { Code = 5 } });

            Assert.Multiple(() =>
            {
                Assert.That(vx.Messages!, Has.Count.EqualTo(1));
                Assert.That(vx.Messages![0].Text, Is.EqualTo("Code must be greater than 10."));
                Assert.That(vx.Messages[0].Property, Is.EqualTo("Value.Code"));
            });
        }

        private Result TestInjectValueValidate(PropertyContext<TestInject, object?> context)
        {
            var vxc = Validator.Create<TestInjectChild>()
                .HasProperty(x => x.Code, p => p.Mandatory().CompareValue(CompareOperator.GreaterThan, 10));

            var type = vxc.GetType();
            var mi = type.GetMethod("ValidateAsync")!;
            var vc = ((Task<ValidationContext<TestInjectChild>>)mi.Invoke(vxc, [context.Value, context.CreateValidationArgs(), System.Threading.CancellationToken.None])!).GetAwaiter().GetResult();
            context.Parent.MergeResult(vc);
            return Result.Success;
        }

        [Test]
        public async Task Entity_ValueOverrideAndDefault()
        {
            var vc = CommonValidator.Create<decimal>(v => v.Default(100));

            var ti = new TestData { Text = "ABC", CountA = 1 };

            var vx = await Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Override("XYZ"))
                .HasProperty(x => x.CountA, p => p.Default(x => 10))
                .HasProperty(x => x.CountB, p => p.Default(x => 20))
                .HasProperty(x => x.AmountA, p => p.Common(vc))
                .ValidateAsync(ti);

            Assert.Multiple(() =>
            {
                Assert.That(vx.HasErrors, Is.False);
                Assert.That(ti.Text, Is.EqualTo("XYZ"));
                Assert.That(ti.CountA, Is.EqualTo(1));
                Assert.That(ti.CountB, Is.EqualTo(20));
                Assert.That(ti.AmountA, Is.EqualTo(100));
            });
        }

        public class Employee
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public DateTime Birthdate { get; set; }
            public decimal Salary { get; set; }
            public int WorkingYears { get; set; }
        }

        public class EmployeeValidator : Validator<Employee>
        {
            public EmployeeValidator()
            {
                Property(x => x.FirstName).Mandatory().String(100);
                Property(x => x.LastName).Mandatory().String(100);
                Property(x => x.Birthdate).Mandatory().CompareValue(CompareOperator.LessThanEqual, DateTime.UtcNow, "today");
                Property(x => x.Salary).Mandatory().Numeric(allowNegatives: false, maxDigits: 10, decimalPlaces: 2);
                Property(x => x.WorkingYears).Numeric(allowNegatives: false).CompareValue(CompareOperator.LessThanEqual, 50);
            }
        }

        [Test]
        public void Entity_ValueCachePerfSync()
        {
            InstantiateValidators();
        }

        private static void InstantiateValidators()
        {
            for (int i = 0; i < 1000; i++)
            {
                _ = new EmployeeValidator();
            }
        }

        [Test]
        public void Entity_ValueCachePerfAsync()
        {
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => InstantiateValidators());
            }

            Task.WaitAll(tasks);
        }

        [Test]
        public async Task Coll_Validator_MaxCount()
        {
            var vxc = Validator.CreateForCollection<List<TestItem>>(minCount: 1, maxCount: 2, item: CollectionRuleItem.Create(new TestItemValidator()));
            var tc = new List<TestItem> { new() { Id = "A", Text = "aaa" }, new() { Id = "B", Text = "bbb" }, new() { Id = "C", Text = "ccc" } };

            var r = await tc.Validate(vxc, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(3));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Description is invalid."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value[0].Text"));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Text, Is.EqualTo("Description is invalid."));
                Assert.That(r.Messages[1].Property, Is.EqualTo("value[1].Text"));
                Assert.That(r.Messages[2].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[2].Text, Is.EqualTo("Value must not exceed 2 item(s)."));
                Assert.That(r.Messages[2].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Coll_Validator_MinCount()
        {
            var vxc = Validator.CreateForCollection<List<TestItem>, TestItem>(new TestItemValidator(), minCount: 3);
            var tc = new List<TestItem> { new() { Id = "A", Text = "A" }, new() { Id = "B", Text = "B" } };

            var r = await tc.Validate(vxc, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Value must have at least 3 item(s)."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Coll_Validator_MinCount2()
        {
            var vxc = Validator.CreateFor<List<TestItem>>().Configure(c => c.Collection(new TestItemValidator(), minCount: 3));
            var tc = new List<TestItem> { new() { Id = "A", Text = "A" }, new() { Id = "B", Text = "B" } };

            var r = await tc.Validate(vxc, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Value must have at least 3 item(s)."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Coll_Validator_Duplicate()
        {
            var vxc = Validator.CreateForCollection<List<TestItem>>(item: CollectionRuleItem.Create(new TestItemValidator()).DuplicateCheck(x => x.Id));
            var tc = new List<TestItem> { new() { Id = "A", Text = "A" }, new() { Id = "A", Text = "A" } };

            var r = await tc.Validate(vxc, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Value contains duplicates; Identifier 'A' specified more than once."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Coll_Validator_OK()
        {
            var vxc = Validator.CreateForCollection<List<TestItem>>(minCount: 1, maxCount: 2, item: CollectionRuleItem.Create(new TestItemValidator()).DuplicateCheck(x => x.Id));
            var tc = new List<TestItem> { new() { Id = "A", Text = "A" }, new() { Id = "B", Text = "B" } };

            var r = await tc.Validate(vxc, null).ValidateAsync();

            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task Coll_Validator_Int_OK()
        {
            var vxc = Validator.CreateForCollection<List<int>>(minCount: 1, maxCount: 5);
            var ic = new List<int> { 1, 2, 3, 4, 5 };

            var r = await ic.Validate(vxc, null).ValidateAsync();

            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task Coll_Validator_Int_Error()
        {
            var vxc = Validator.CreateFor<List<int>>(v => v.Collection(1, 3));
            var ic = new List<int> { 1, 2, 3, 4, 5 };

            var r = await ic.Validate(vxc, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Value must not exceed 3 item(s)."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Dict_Validator_MaxCount()
        {
            var vxd = Validator.CreateForDictionary<Dictionary<string, TestItem>>(minCount: 1, maxCount: 2, item: DictionaryRuleItem.Create<string, TestItem>(value: new TestItemValidator()));
            var tc = new Dictionary<string, TestItem> { { "k1", new TestItem { Id = "A", Text = "aaa" } }, { "k2", new TestItem { Id = "B", Text = "bbb" } }, { "k3", new TestItem { Id = "C", Text = "ccc" } } };

            var r = await tc.Validate(vxd, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(3));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Description is invalid."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value[k1].Text"));
                Assert.That(r.Messages[1].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[1].Text, Is.EqualTo("Description is invalid."));
                Assert.That(r.Messages[1].Property, Is.EqualTo("value[k2].Text"));
                Assert.That(r.Messages[2].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[2].Text, Is.EqualTo("Value must not exceed 2 item(s)."));
                Assert.That(r.Messages[2].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Dict_Validator_MinCount()
        {
            var vxd = Validator.CreateForDictionary<Dictionary<string, TestItem>>(minCount: 3, item: DictionaryRuleItem.Create<string, TestItem>(value: new TestItemValidator()));
            var tc = new Dictionary<string, TestItem> { { "k1", new TestItem { Id = "A", Text = "A" } }, { "k2", new TestItem { Id = "B", Text = "B" } } };

            var r = await tc.Validate(vxd, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Value must have at least 3 item(s)."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Dict_Validator_OK()
        {
            var vxd = Validator.CreateForDictionary<Dictionary<string, TestItem>, string, TestItem>(new TestItemValidator(), minCount: 2);
            var tc = new Dictionary<string, TestItem> { { "k1", new TestItem { Id = "A", Text = "A" } }, { "k2", new TestItem { Id = "B", Text = "B" } } };

            var r = await tc.Validate(vxd, null).ValidateAsync();

            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task Dict_Validator_Int_OK()
        {
            var vxd = Validator.CreateForDictionary<Dictionary<string, int>>(minCount: 1, maxCount: 5);
            var id = new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 }, { "k3", 3 }, { "k4", 4 }, { "k5", 5 } };

            var r = await id.Validate(vxd, null).ValidateAsync();

            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task Dict_Validator_Int_Error()
        {
            var vxd = Validator.CreateForDictionary<Dictionary<string, int>>(minCount: 1, maxCount: 3);
            var id = new Dictionary<string, int> { { "k1", 1 }, { "k2", 2 }, { "k3", 3 }, { "k4", 4 }, { "k5", 5 } };

            var r = await id.Validate(vxd, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Value must not exceed 3 item(s)."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Dict_Validator_KeyError()
        {
            var kv = CommonValidator.Create<string>(x => x.Text("Key").Mandatory().String(2));
            var vxd = Validator.CreateForDictionary<Dictionary<string, TestItem>, string, TestItem>(kv, new TestItemValidator(), minCount: 2);
            var tc = new Dictionary<string, TestItem> { { "k1", new TestItem { Id = "A", Text = "A" } }, { "k2x", new TestItem { Id = "B", Text = "B" } } };

            var r = await tc.Validate(vxd, null).ValidateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Key must not exceed 2 characters in length."));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value[k2x]"));
            });
        }

        [Test]
        public async Task Validator_Perf_SameValidator()
        {
            var ev = new EmployeeValidator();
            var v = new Employee { FirstName = "Speedy", LastName = "Fasti", Birthdate = new DateTime(1999, 10, 22), Salary = 51000m, WorkingYears = 20 };

            await ev.ValidateAsync(v).ConfigureAwait(false);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                var r = await ev.ValidateAsync(v).ConfigureAwait(false);
                r.ThrowOnError();
            }

            sw.Stop();
            System.Console.WriteLine($"100K validations - elapsed: {sw.Elapsed.TotalMilliseconds}ms (per {sw.Elapsed.TotalMilliseconds / 100000}ms)");
        }

        [Test]
        public void Required()
        {
            var vex = Assert.Throws<ValidationException>(() => 0.Required());
            Assert.That(vex, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vex!.Messages, Is.Not.Null);
                Assert.That(vex.Messages!, Has.Count.EqualTo(1));
                Assert.That(vex.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(vex.Messages[0].Text, Is.EqualTo("0 is required."));
                Assert.That(vex.Messages[0].Property, Is.EqualTo("0"));
            });

            var count = 0;
            vex = Assert.Throws<ValidationException>(() => count.Required());
            Assert.That(vex, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vex!.Messages, Is.Not.Null);
                Assert.That(vex.Messages!, Has.Count.EqualTo(1));
                Assert.That(vex.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(vex.Messages[0].Text, Is.EqualTo("Count is required."));
                Assert.That(vex.Messages[0].Property, Is.EqualTo("count"));
            });

            vex = Assert.Throws<ValidationException>(() => 0.Required("count"));
            Assert.That(vex, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vex!.Messages, Is.Not.Null);
                Assert.That(vex.Messages!, Has.Count.EqualTo(1));
                Assert.That(vex.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(vex.Messages[0].Text, Is.EqualTo("Count is required."));
                Assert.That(vex.Messages[0].Property, Is.EqualTo("count"));
            });

            vex = Assert.Throws<ValidationException>(() => 0.Required("count", "Counter"));
            Assert.That(vex, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vex!.Messages, Is.Not.Null);
                Assert.That(vex.Messages!, Has.Count.EqualTo(1));
                Assert.That(vex.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(vex.Messages[0].Text, Is.EqualTo("Counter is required."));
                Assert.That(vex.Messages[0].Property, Is.EqualTo("count"));
            });

            vex = Assert.Throws<ValidationException>(() => 0.Required("numberOfPlayers"));
            Assert.That(vex, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vex!.Messages, Is.Not.Null);
                Assert.That(vex.Messages!, Has.Count.EqualTo(1));
                Assert.That(vex.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(vex.Messages[0].Text, Is.EqualTo("Number Of Players is required."));
                Assert.That(vex.Messages[0].Property, Is.EqualTo("numberOfPlayers"));
            });

            vex = Assert.Throws<ValidationException>(() => 0.Required("id"));
            Assert.That(vex, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vex!.Messages, Is.Not.Null);
                Assert.That(vex.Messages!, Has.Count.EqualTo(1));
                Assert.That(vex.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(vex.Messages[0].Text, Is.EqualTo("Identifier is required."));
                Assert.That(vex.Messages[0].Property, Is.EqualTo("id"));
            });

            vex = Assert.Throws<ValidationException>(() => 0.Required());
            Assert.That(vex, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vex!.Messages, Is.Not.Null);
                Assert.That(vex.Messages!, Has.Count.EqualTo(1));
                Assert.That(vex.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(vex.Messages[0].Text, Is.EqualTo("0 is required."));
                Assert.That(vex.Messages[0].Property, Is.EqualTo("0"));

                Assert.That(123.Required(), Is.EqualTo(123));
            });
        }

        [Test]
        public async Task Validator_FailureResult()
        {
            var ev = new EmployeeValidator2();
            var v = new Employee { FirstName = "Speedy", LastName = "Fasti", Birthdate = new DateTime(1999, 10, 22), Salary = 51000m, WorkingYears = 20 };

            var r = await ev.ValidateAsync(v).ConfigureAwait(false);
            Assert.That(r, Is.Not.Null);
            Assert.That(r.HasErrors, Is.False);

            v.Salary += 88000;
            r = await ev.ValidateAsync(v).ConfigureAwait(false);
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.FailureResult, Is.Not.Null);
            });
            Assert.That(r.FailureResult!.Value.Error, Is.Not.Null.And.TypeOf<ConflictException>());

            Assert.Throws<ConflictException>(() => r.ThrowOnError());
        }

        public class EmployeeValidator2 : Validator<Employee>
        {
            public EmployeeValidator2()
            {
                Property(x => x.FirstName).Mandatory().String(100);
                Property(x => x.LastName).Mandatory().String(100);
                Property(x => x.Birthdate).Mandatory().CompareValue(CompareOperator.LessThanEqual, DateTime.UtcNow, "today");
                Property(x => x.Salary).Mandatory().Numeric(allowNegatives: false, maxDigits: 10, decimalPlaces: 2);
                Property(x => x.WorkingYears).Numeric(allowNegatives: false).CompareValue(CompareOperator.LessThanEqual, 50);
            }

            protected override async Task<Result> OnValidateAsync(ValidationContext<Employee> context, CancellationToken cancellationToken)
            {
                if (context.Value.Salary > 88000m)
                    return Result.ConflictError("Highly paid individual already exists.");

                return await base.OnValidateAsync(context, cancellationToken);
            }
        }

        public class TeamLeader
        {
            public Employee? Person { get; set; }

            public string? TeamName { get; set; }
        }

        public class TeamLeaderValidator : Validator<TeamLeader>
        {
            public TeamLeaderValidator()
            {
                Property(x => x.Person).Mandatory().Entity(new EmployeeValidator2());
                Property(x => x.TeamName).Mandatory().String(20);
            }
        }

        [Test]
        public async Task Validator_Nested_FailureResult()
        {
            var tlv = new TeamLeaderValidator();
            var v = new TeamLeader { Person = new Employee { FirstName = "Speedy", LastName = "Fasti", Birthdate = new DateTime(1999, 10, 22), Salary = 51000m, WorkingYears = 20 }, TeamName = "Bananas" };

            var r = await tlv.ValidateAsync(v).ConfigureAwait(false);
            Assert.That(r, Is.Not.Null);
            Assert.That(r.HasErrors, Is.False);

            v.TeamName += " and Oranges and Apples and Kiwi Fruit";
            r = await tlv.ValidateAsync(v).ConfigureAwait(false);
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Text, Is.EqualTo("Team Name must not exceed 20 characters in length."));
            });

            v.Person.Salary += 88000;
            r = await tlv.ValidateAsync(v).ConfigureAwait(false);
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.FailureResult, Is.Not.Null);
            });
            Assert.That(r.FailureResult!.Value.Error, Is.Not.Null.And.TypeOf<ConflictException>());
        }
    }
}