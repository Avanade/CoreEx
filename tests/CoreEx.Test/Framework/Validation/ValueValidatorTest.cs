using CoreEx.Results;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Validation
{
    [TestFixture]
    public class ValueValidatorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Run_With_NotNull()
        {
            string name = "George";
            var r = await name.Validate().Mandatory().String(50).ValidateAsync();
            Assert.That(r, Is.Not.Null);
            Assert.That(r.HasErrors, Is.False);
        }

        [Test]
        public async Task Run_With_Null()
        {
            string? name = null;
            var r = await name.Validate().Mandatory().String(50).ValidateAsync();
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.Messages!, Has.Count.EqualTo(1));
                Assert.That(r.Messages![0].Text, Is.EqualTo("Name is required."));
            });
        }

        [Test]
        public async Task Validate_With_FailureResult()
        {
            string name = "Bill";
            var r = await name.Validate().Mandatory().Custom(ctx => Result.Go().When(() => ctx.Value == "Bill", () => Result.NotFoundError())).String(5).ValidateAsync();
            Assert.That(r, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(r.HasErrors, Is.True);
                Assert.That(r.FailureResult, Is.Not.Null);
            });
            Assert.That(r.FailureResult!.Value.Error, Is.Not.Null.And.TypeOf<NotFoundException>());
            Assert.Throws<NotFoundException>(() => r.ThrowOnError());
        }
    }
}