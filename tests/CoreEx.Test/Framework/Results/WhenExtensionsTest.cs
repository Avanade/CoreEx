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
            Assert.AreEqual(1, j);
            AssertFailure(Result.Fail(new BusinessException()).When(() => true, () => { Assert.Fail(); }));

            AssertSuccess(Result.Go().When(() => true, () => Result.Success).When(() => false, () => Result.Fail()));
            AssertFailure(Result.Fail(new BusinessException()).When(() => true, () => Result.NotFoundError()));

            j = 0;
            AssertSuccess(Result.Go(0).When(_ => true, _ => { j++; }).When(_ => false, _ => { Assert.Fail(); }));
            Assert.AreEqual(1, j);
            AssertFailure(Result.Go<int>(new NotFoundException()).When(_ => true, _ => { Assert.Fail(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).When(_ => true, i => ++i).When(_ => false, _ => Assert.Fail())));
            AssertFailure(Result.Go<int>(new NotFoundException()).When(_ => true, i => { Assert.Fail(); return +i; }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).When(_ => true, i => Result.Ok(++i)).When(_ => false, i => Result.Ok(++i))));
            AssertFailure(Result.Go<int>(new NotFoundException()).When(_ => true, i => { Assert.Fail(); return Result.Ok(++i); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().WhenAs(() => true, () => 1)));
            Assert.AreEqual(0, AssertSuccess(Result.Go().WhenAs(() => false, () => 1)));
            AssertFailure(Result.NotFoundError().WhenAs(() => true, () => { Assert.Fail(); return 1; }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().WhenAs(() => true, () => Result.Ok(1))));
            Assert.AreEqual(0, AssertSuccess(Result.Go().WhenAs(() => false, () => Result.Ok(1))));
            AssertFailure(Result.NotFoundError().WhenAs(() => true, () => { Assert.Fail(); return Result.Ok(1); }));

            AssertSuccess(Result.Go(1).WhenAs(_ => true, _ => { }));
            AssertSuccess(Result.Go(1).WhenAs(_ => false, _ => { Assert.Fail(); }));
            AssertFailure(Result<int>.NotFoundError().WhenAs(_ => true, _ => { Assert.Fail(); }));

            AssertSuccess(Result.Go(1).WhenAs(_ => true, _ => Result.Success));
            AssertSuccess(Result.Go(1).WhenAs(_ => false, _ => Result.Fail()));
            AssertFailure(Result<int>.NotFoundError().WhenAs(_ => true, _ => Result.Success));

            Assert.AreEqual(2f, AssertSuccess(Result.Go(1).WhenAs(_ => true, i => i + 1f)));
            Assert.AreEqual(1f, AssertSuccess(Result.Go(1).WhenAs(_ => false, i => i + 1f)));
            AssertFailure(Result.Go<int>(new NotFoundException()).WhenAs(_ => true, i => { Assert.Fail(); return i + 1f; }));

            Assert.AreEqual(2f, AssertSuccess(Result.Go(1).WhenAs(_ => true, i => Result.Ok(i + 1f))));
            Assert.AreEqual(1f, AssertSuccess(Result.Go(1).WhenAs(_ => false, i => Result.Ok(i + 1f))));
            AssertFailure(Result.Go<int>(new NotFoundException()).WhenAs(_ => true, i => { Assert.Fail(); return Result.Ok(i + 1f); }));

            AssertSuccess(Result.Go().When(() => true, () => new IToResultTest()));
            AssertSuccess(Result.Go().When(() => false, () => { Assert.Fail(); return new IToResultTest(); }));
            AssertFailure(Result.NotFoundError().When(() => true, () => { Assert.Fail(); return new IToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).When(_ => true, _ => new ITypedToResultTest())));
            Assert.AreEqual(0, AssertSuccess(Result.Go(0).When(_ => false, _ => new ITypedToResultTest())));
            AssertFailure(Result<int>.NotFoundError().When(_ => true, _ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).When(_ => true, _ => new IToResultIntTest())));
            Assert.AreEqual(0, AssertSuccess(Result.Go(0).When(_ => false, _ => new IToResultIntTest())));
            AssertFailure(Result<int>.NotFoundError().When(_ => true, _ => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().WhenAs<int>(() => true, () => new ITypedToResultTest())));
            Assert.AreEqual(0, AssertSuccess(Result.Go().WhenAs<int>(() => false, () => new ITypedToResultTest())));
            AssertFailure(Result.NotFoundError().WhenAs<int>(() => true, () => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().WhenAs<int>(() => true, () => new IToResultIntTest())));
            Assert.AreEqual(0, AssertSuccess(Result.Go().WhenAs<int>(() => false, () => new IToResultIntTest())));
            AssertFailure(Result.NotFoundError().WhenAs<int>(() => true, () => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(2.2f).WhenAs<float, int>(_ => true, _ => new ITypedToResultTest())));
            Assert.AreEqual(2, AssertSuccess(Result.Go(2.2f).WhenAs<float, int>(_ => false, _ => new ITypedToResultTest())));
            AssertFailure(Result<float>.NotFoundError().WhenAs<float, int>(_ => true, _ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(2.2f).WhenAs<float, int>(_ => true, _ => new IToResultIntTest())));
            Assert.AreEqual(2, AssertSuccess(Result.Go(2.2f).WhenAs<float, int>(_ => false, _ => new IToResultIntTest())));
            AssertFailure(Result<float>.NotFoundError().WhenAs<float, int>(_ => true, _ => { Assert.Fail(); return new IToResultIntTest(); }));
        }
    }
}