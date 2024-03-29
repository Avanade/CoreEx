﻿using CoreEx.Entities;
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

        private static readonly CommonValidator<string> _cv = Validator.CreateCommon<string>(v => v.String(5).Must(x => x.Value != "XXXXX"));
        private static readonly CommonValidator<int?> _cv2 = Validator.CreateCommon<int?>(v => v.Mandatory().CompareValue(CompareOperator.NotEqual, 1));

        [Test]
        public async Task Validate()
        {
            var r = await _cv.ValidateAsync("XXXXXX");
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Value must not exceed 5 characters in length."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("value"));
            });

            r = await _cv.ValidateAsync("XXXXX", "Name");
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Name is invalid."));
                Assert.That(r.Messages[0].Type, Is.EqualTo(MessageType.Error));
                Assert.That(r.Messages[0].Property, Is.EqualTo("Name"));
            });

            r = await _cv.ValidateAsync("XXX", "Name");
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
            var r = await _cv2.ValidateAsync(v);
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
            var cv = Validator.CreateCommon<string>(v => v.String(5)).AdditionalAsync((c, _) => Task.FromResult(Result.NotFoundError()));
            var r = await cv.ValidateAsync("abc");

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
            var cv = Validator.CreateCommon<string>(v => v.String(5).Custom(ctx => Result.NotFoundError()));
            var r = await cv.ValidateAsync("abc");

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
            var cv = Validator.CreateCommon<string>(v => v.String(5).Custom(ctx => Result.NotFoundError()));
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
            var vr = await iv.ValidateAsync(8);
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
            var vr = await iv.ValidateAsync(28);
            Assert.Multiple(() =>
            {
                Assert.That(vr.HasErrors, Is.True);
                Assert.That(vr.Messages!, Has.Count.EqualTo(1));
                Assert.That(vr.Messages![0].Text, Is.EqualTo("Count must be less than or equal to 20."));
                Assert.That(vr.Messages[0].Property, Is.EqualTo("value"));
            });
        }

        [Test]
        public async Task Inherited_OnValidate()
        {
            var iv = new IntValidator();
            var vr = await iv.ValidateAsync(11);
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