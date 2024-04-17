using CoreEx.Entities;
using CoreEx.Validation;
using CoreEx.Results;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation
{
    [TestFixture]
    public class CommonValidatorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        private static readonly CommonValidator<string?> _cv = Validator.CreateFor<string?>(v => v.String(5).Must(x => x.Value != "XXXXX"));
        private static readonly CommonValidator<int?> _cv2 = Validator.CreateFor<int?>(v => v.Mandatory().CompareValue(CompareOperator.NotEqual, 1));

        [Test]
        public async Task Validate()
        {
            var r = await "XXXXXX".Validate(_cv, null).ValidateAsync();
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Value must not exceed 5 characters in length."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });

            r = await "XXXXX".Validate(_cv, "Name").ValidateAsync();
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Name is invalid."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Name"));
            });

            r = await "XXX".Validate(_cv, "Name").ValidateAsync();
            Assert.That(r, Is.Not.Null);
            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task Common()
        {
            var r = await Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().Common(_cv))
                .ValidateAsync(new TestData { Text = "XXXXXX" });

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Text must not exceed 5 characters in length."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
            });

            r = await Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().Common(_cv))
                .ValidateAsync(new TestData { Text = "XXXXX" });

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Text is invalid."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Text"));
            });

            r = await Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().Common(_cv))
                .ValidateAsync(new TestData { Text = "XXX" });

            Assert.That(r, Is.Not.Null);
            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task Validate_Nullable()
        {
            int? v = 1;
            var r = await v.Validate(_cv2, null).ValidateAsync();
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Value must not be equal to 1."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Common_Nullable()
        {
            var r = await Validator.Create<TestData>()
                .HasProperty(x => x.CountB, p => p.Mandatory().Common(_cv2))
                .ValidateAsync(new TestData { CountB = 1 });

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Count B must not be equal to 1."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("CountB"));
            });
        }

        [Test]
        public async Task Common_FailureResult_ViaAdditional()
        {
            var cv = Validator.CreateFor<string?>(v => v.String(5)).AdditionalAsync((c, _) => Task.FromResult(Result.NotFoundError()));
            var r = await "abc".Validate(cv).ValidateAsync();

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.FailureResult, Is.Not.Null);
            });
            Assert.That(r.FailureResult!.Value.Error, Is.Not.Null.And.TypeOf<NotFoundException>());
            Assert.Throws<NotFoundException>(() => r.ThrowOnError());
        }

        [Test]
        public async Task Common_FailureResult_ViaCustom()
        {
            var cv = CommonValidator.Create<string>(v => v.String(5).Custom(ctx => Result.NotFoundError()));
            var r = await "abc".Validate(cv).ValidateAsync();

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.FailureResult, Is.Not.Null);
            });
            Assert.That(r.FailureResult!.Value.Error, Is.Not.Null.And.TypeOf<NotFoundException>());
            Assert.Throws<NotFoundException>(() => r.ThrowOnError());
        }

        [Test]
        public async Task Common_FailureResult_WithOwningValidator()
        {
            var cv = CommonValidator.Create<string>(v => v.String(5).Custom(ctx => Result.NotFoundError()));
            var pv = Validator.Create<Person>().HasProperty(x => x.Name, p => p.Common(cv));

            var p = new Person { Name = "abc" };
            var r = await pv.ValidateAsync(p);
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.FailureResult, Is.Not.Null);
            });
            Assert.That(r.FailureResult!.Value.Error, Is.Not.Null.And.TypeOf<NotFoundException>());
            Assert.Throws<NotFoundException>(() => r.ThrowOnError());
        }

        [Test]
        public async Task CreateFor()
        {
            var cv = Validator.CreateFor<string>().Configure(v => v.MaximumLength(5));
            var r = await "abcdef".Validate(cv, null).ValidateAsync();

            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Value must not exceed 5 characters in length."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task CommonExtensionMethod()
        {
            await CommonExtensionMethod(null, false);
            await CommonExtensionMethod("12345", false);
            await CommonExtensionMethod("1234567", true);
        }

        private async Task CommonExtensionMethod(string? accountId, bool expectErrors)
        {
            var r = await accountId.Validate().Configure(cv => cv.Common(Validators.String5)).ValidateAsync();
            Assert.That(r.HasErrors, Is.EqualTo(expectErrors));
        }

        [Test]
        public async Task CommonEntity()
        {
            var pv = new PersonValidator();
            var r = await pv.ValidateAsync(new Person());
            Assert.That(r.HasErrors, Is.False);

            r = await pv.ValidateAsync(new Person { Name = "12345" });
            Assert.That(r.HasErrors, Is.False);

            r = await pv.ValidateAsync(new Person { Name = "12345678" });
            Assert.That(r.HasErrors, Is.True);
        }

        public static class Validators
        {
            public static CommonValidator<string> String5 => CommonValidator.Create<string>(c => c.String(5));
        }

        public class PersonValidator : Validator<Person>
        {
            public PersonValidator() => HasProperty(x => x.Name, p => p.Common(Validators.String5));
        }

        public class Person
        {
            public string? Name { get; set; }
        }   

        public class IntValidator : CommonValidator<int>
        {
            public IntValidator() => this.Text("Count").Mandatory().CompareValue(CompareOperator.GreaterThanEqual, 10).CompareValue(CompareOperator.LessThanEqual, 20);

            protected override Task<Result> OnValidateAsync(PropertyContext<ValidationValue<int>, int> context, CancellationToken ct)
            {
                if (context.Value == 11)
                    context.CreateErrorMessage("{0} is not allowed to be eleven.");

                return Task.FromResult(Result.Success);
            }
        }

        [Test]
        public async Task Inherited_Basic()
        {
            var iv = new IntValidator();
            var vr = await 8.Validate(iv, null).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(vr.HasErrors, Is.True);
                Assert.That(vr.Messages!, Has.Count.EqualTo(1));
                Assert.That(vr.Messages![0].Text, Is.EqualTo("Count must be greater than or equal to 10."));
                Assert.That(vr.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Inherited_Basic2()
        {
            var iv = new IntValidator();
            var vr = await 28.Validate(iv, "length").ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(vr.HasErrors, Is.True);
                Assert.That(vr.Messages!, Has.Count.EqualTo(1));
                Assert.That(vr.Messages![0].Text, Is.EqualTo("Count must be less than or equal to 20."));
                Assert.That(vr.Messages[0].Property, Is.EqualTo("length"));
            });
        }

        [Test]
        public async Task Inherited_OnValidate()
        {
            var iv = new IntValidator();
            var vr = await 11.Validate(iv, null).ValidateAsync();
            Assert.Multiple(() =>
            {
                Assert.That(vr.HasErrors, Is.True);
                Assert.That(vr.Messages!, Has.Count.EqualTo(1));
                Assert.That(vr.Messages![0].Text, Is.EqualTo("Count is not allowed to be eleven."));
                Assert.That(vr.Messages[0].Property, Is.EqualTo("value"));
            });
        }
    }
}