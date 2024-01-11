using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;
using static CoreEx.Test.Framework.Results.ThenExtensionsTest;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class WhenExtensionsTest
    {
        public void AssertSuccess(Result result) => Assert.True(result.IsSuccess);
        public void AssertFailure(Result result) => Assert.True(result.IsFailure);

        public T AssertSuccess<T>(Result<T> result)
        {
            Assert.True(result.IsSuccess);
            return result.Value;
        }

        public void AssertFailure<T>(Result<T> result) => Assert.True(result.IsFailure);

        [Test]
        public void Sync()
        {
            int j = 0;
            AssertSuccess(Result.Go().When(() => true, () => { j++; }).When(() => false, () => { j++; }));
            Assert.That(j, Is.EqualTo(1));
            AssertFailure(Result.Fail(new BusinessException()).When(() => true, () => { Assert.Fail(); }));

            AssertSuccess(Result.Go().When(() => true, () => Result.Success).When(() => false, () => Result.Fail()));
            AssertFailure(Result.Fail(new BusinessException()).When(() => true, () => Result.NotFoundError()));

            j = 0;
            AssertSuccess(Result.Go(0).When(_ => true, _ => { j++; }).When(_ => false, _ => { Assert.Fail(); }));
            Assert.That(j, Is.EqualTo(1));
            AssertFailure(Result.Go<int>(new NotFoundException()).When(_ => true, _ => { Assert.Fail(); }));

            Assert.That(AssertSuccess(Result.Go(0).When(_ => true, i => ++i).When(_ => false, _ => Assert.Fail())), Is.EqualTo(1));
            AssertFailure(Result.Go<int>(new NotFoundException()).When(_ => true, i => { Assert.Fail(); return +i; }));

            Assert.That(AssertSuccess(Result.Go(0).When(_ => true, i => Result.Ok(++i)).When(_ => false, i => Result.Ok(++i))), Is.EqualTo(1));
            AssertFailure(Result.Go<int>(new NotFoundException()).When(_ => true, i => { Assert.Fail(); return Result.Ok(++i); }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go().WhenAs(() => true, () => 1)), Is.EqualTo(1));
                Assert.That(AssertSuccess(Result.Go().WhenAs(() => false, () => 1)), Is.EqualTo(0));
            });
            AssertFailure(Result.NotFoundError().WhenAs(() => true, () => { Assert.Fail(); return 1; }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go().WhenAs(() => true, () => Result.Ok(1))), Is.EqualTo(1));
                Assert.That(AssertSuccess(Result.Go().WhenAs(() => false, () => Result.Ok(1))), Is.EqualTo(0));
            });
            AssertFailure(Result.NotFoundError().WhenAs(() => true, () => { Assert.Fail(); return Result.Ok(1); }));

            AssertSuccess(Result.Go(1).WhenAs(_ => true, _ => { }));
            AssertSuccess(Result.Go(1).WhenAs(_ => false, _ => { Assert.Fail(); }));
            AssertFailure(Result<int>.NotFoundError().WhenAs(_ => true, _ => { Assert.Fail(); }));

            AssertSuccess(Result.Go(1).WhenAs(_ => true, _ => Result.Success));
            AssertSuccess(Result.Go(1).WhenAs(_ => false, _ => Result.Fail()));
            AssertFailure(Result<int>.NotFoundError().WhenAs(_ => true, _ => Result.Success));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go(1).WhenAs(_ => true, i => i + 1f)), Is.EqualTo(2f));
                Assert.That(AssertSuccess(Result.Go(1).WhenAs(_ => false, i => i + 1f)), Is.EqualTo(0f));
            });
            AssertFailure(Result.Go<int>(new NotFoundException()).WhenAs(_ => true, i => { Assert.Fail(); return i + 1f; }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go(1).WhenAs(_ => true, i => Result.Ok(i + 1f))), Is.EqualTo(2f));
                Assert.That(AssertSuccess(Result.Go(1).WhenAs(_ => false, i => Result.Ok(i + 1f))), Is.EqualTo(0f));
            });
            AssertFailure(Result.Go<int>(new NotFoundException()).WhenAs(_ => true, i => { Assert.Fail(); return Result.Ok(i + 1f); }));

            AssertSuccess(Result.Go().WhenFrom(() => true, () => new IToResultTest()));
            AssertSuccess(Result.Go().WhenFrom(() => false, () => { Assert.Fail(); return new IToResultTest(); }));
            AssertFailure(Result.NotFoundError().WhenFrom(() => true, () => { Assert.Fail(); return new IToResultTest(); }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go(0).WhenFrom(_ => true, _ => new ITypedToResultTest())), Is.EqualTo(1));
                Assert.That(AssertSuccess(Result.Go(0).WhenFrom(_ => false, _ => new ITypedToResultTest())), Is.EqualTo(0));
            });
            AssertFailure(Result<int>.NotFoundError().WhenFrom(_ => true, _ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go(0).WhenFrom(_ => true, _ => new IToResultIntTest())), Is.EqualTo(1));
                Assert.That(AssertSuccess(Result.Go(0).WhenFrom(_ => false, _ => new IToResultIntTest())), Is.EqualTo(0));
            });
            AssertFailure(Result<int>.NotFoundError().WhenFrom(_ => true, _ => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go().WhenFromAs<int>(() => true, () => new ITypedToResultTest())), Is.EqualTo(1));
                Assert.That(AssertSuccess(Result.Go().WhenFromAs<int>(() => false, () => new ITypedToResultTest())), Is.EqualTo(0));
            });
            AssertFailure(Result.NotFoundError().WhenFromAs<int>(() => true, () => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go().WhenFromAs<int>(() => true, () => new IToResultIntTest())), Is.EqualTo(1));
                Assert.That(AssertSuccess(Result.Go().WhenFromAs<int>(() => false, () => new IToResultIntTest())), Is.EqualTo(0));
            });
            AssertFailure(Result.NotFoundError().WhenFromAs<int>(() => true, () => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go(2.2f).WhenFromAs<float, int>(_ => true, _ => new ITypedToResultTest())), Is.EqualTo(1));
                Assert.That(AssertSuccess(Result.Go(2.2f).WhenFromAs<float, int>(_ => false, _ => new ITypedToResultTest())), Is.EqualTo(0));
            });
            AssertFailure(Result<float>.NotFoundError().WhenFromAs<float, int>(_ => true, _ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.Multiple(() =>
            {
                Assert.That(AssertSuccess(Result.Go(2.2f).WhenFromAs<float, int>(_ => true, _ => new IToResultIntTest())), Is.EqualTo(1));
                Assert.That(AssertSuccess(Result.Go(2.2f).WhenFromAs<float, int>(_ => false, _ => new IToResultIntTest())), Is.EqualTo(0));
            });
            AssertFailure(Result<float>.NotFoundError().WhenFromAs<float, int>(_ => true, _ => { Assert.Fail(); return new IToResultIntTest(); }));
        }

        [Test]
        public void Sync_Otherwise()
        {
            int j = 0;
            AssertSuccess(Result.Go().When(() => j == 0, () => { ++j; }, () => { j += 10; }));
            Assert.That(j, Is.EqualTo(1));

            AssertSuccess(Result.Go().When(() => j == 0, () => { ++j; }, () => { j += 10; }));
            Assert.That(j, Is.EqualTo(11));

            j = AssertSuccess(Result.Go(0).When(x => x == 0, x => ++x, x => x += 10));
            Assert.That(j, Is.EqualTo(1));

            j = AssertSuccess(Result.Go(1).When(x => x == 0, x => ++x, x => x += 10));
            Assert.That(j, Is.EqualTo(11));
        }
    }
}