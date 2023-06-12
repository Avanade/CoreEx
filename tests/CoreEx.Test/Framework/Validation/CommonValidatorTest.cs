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

        private static readonly CommonValidator<string> _cv = Validator.CreateCommon<string>(v => v.String(5).Must(x => x.Value != "XXXXX"));
        private static readonly CommonValidator<int?> _cv2 = Validator.CreateCommon<int?>(v => v.Mandatory().CompareValue(CompareOperator.NotEqual, 1));

        [Test]
        public async Task Validate()
        {
            var r = await _cv.ValidateAsync("XXXXXX");
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Value must not exceed 5 characters in length.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("value", r.Messages[0].Property);

            r = await _cv.ValidateAsync("XXXXX", "Name");
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Name is invalid.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("Name", r.Messages[0].Property);

            r = await _cv.ValidateAsync("XXX", "Name");
            Assert.IsNotNull(r);
            Assert.IsFalse(r.HasErrors);
        }

        [Test]
        public async Task Common()
        {
            var r = await Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().Common(_cv))
                .ValidateAsync(new TestData { Text = "XXXXXX" });

            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Text must not exceed 5 characters in length.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("Text", r.Messages[0].Property);

            r = await Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().Common(_cv))
                .ValidateAsync(new TestData { Text = "XXXXX" });

            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Text is invalid.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("Text", r.Messages[0].Property);

            r = await Validator.Create<TestData>()
                .HasProperty(x => x.Text, p => p.Mandatory().Common(_cv))
                .ValidateAsync(new TestData { Text = "XXX" });

            Assert.IsNotNull(r);
            Assert.IsFalse(r.HasErrors);
        }

        [Test]
        public async Task Validate_Nullable()
        {
            int? v = 1;
            var r = await _cv2.ValidateAsync(v);
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Value must not be equal to 1.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("value", r.Messages[0].Property);
        }

        [Test]
        public async Task Common_Nullable()
        {
            var r = await Validator.Create<TestData>()
                .HasProperty(x => x.CountB, p => p.Mandatory().Common(_cv2))
                .ValidateAsync(new TestData { CountB = 1 });

            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(1, r.Messages!.Count);
            Assert.AreEqual("Count B must not be equal to 1.", r.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, r.Messages[0].Type);
            Assert.AreEqual("CountB", r.Messages[0].Property);
        }

        [Test]
        public async Task Common_FailureResult_ViaAdditional()
        {
            var cv = Validator.CreateCommon<string>(v => v.String(5)).AdditionalAsync((c, _) => Task.FromResult(Result.NotFoundError()));
            var r = await cv.ValidateAsync("abc");

            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.IsNotNull(r.FailureResult);
            Assert.That(r.FailureResult!.Value.Error, Is.Not.Null.And.TypeOf<NotFoundException>());
            Assert.Throws<NotFoundException>(() => r.ThrowOnError());
        }

        [Test]
        public async Task Common_FailureResult_ViaCustom()
        {
            var cv = Validator.CreateCommon<string>(v => v.String(5).Custom(ctx => Result.NotFoundError()));
            var r = await cv.ValidateAsync("abc");

            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.IsNotNull(r.FailureResult);
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
            Assert.IsNotNull(r);
            Assert.IsTrue(r.HasErrors);
            Assert.IsNotNull(r.FailureResult);
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
            Assert.IsTrue(vr.HasErrors);
            Assert.AreEqual(1, vr.Messages!.Count);
            Assert.AreEqual("Count must be greater than or equal to 10.", vr.Messages[0].Text);
            Assert.AreEqual("value", vr.Messages[0].Property);
        }

        [Test]
        public async Task Inherited_Basic2()
        {
            var iv = new IntValidator();
            var vr = await iv.ValidateAsync(28);
            Assert.IsTrue(vr.HasErrors);
            Assert.AreEqual(1, vr.Messages!.Count);
            Assert.AreEqual("Count must be less than or equal to 20.", vr.Messages[0].Text);
            Assert.AreEqual("value", vr.Messages[0].Property);
        }

        [Test]
        public async Task Inherited_OnValidate()
        {
            var iv = new IntValidator();
            var vr = await iv.ValidateAsync(11);
            Assert.IsTrue(vr.HasErrors);
            Assert.AreEqual(1, vr.Messages!.Count);
            Assert.AreEqual("Count is not allowed to be eleven.", vr.Messages[0].Text);
            Assert.AreEqual("value", vr.Messages[0].Property);
        }
    }
}