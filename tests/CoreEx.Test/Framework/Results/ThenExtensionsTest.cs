using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ThenExtensionsTest
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
            AssertSuccess(Result.Go().Then(() => { j++; }));
            Assert.AreEqual(1, j);
            AssertFailure(Result.Fail(new BusinessException()).Then(() => { Assert.Fail(); }));

            AssertSuccess(Result.Go().Then(() => Result.Success));
            AssertFailure(Result.Fail(new BusinessException()).Then(() => Result.NotFoundError()));

            j = 0;
            AssertSuccess(Result.Go(0).Then(_ => { j++; }));
            Assert.AreEqual(1, j);
            AssertFailure(Result.Go<int>(new NotFoundException()).Then(_ => { Assert.Fail(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).Then(i => ++i)));
            AssertFailure(Result.Go<int>(new NotFoundException()).Then(i => { Assert.Fail(); return +i; }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).Then(i => Result.Ok(++i))));
            AssertFailure(Result.Go<int>(new NotFoundException()).Then(i => { Assert.Fail(); return Result.Ok(++i); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().ThenAs(() => 1)));
            AssertFailure(Result.NotFoundError().ThenAs(() => { Assert.Fail(); return 1; }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().ThenAs(() => Result.Ok(1))));
            AssertFailure(Result.NotFoundError().ThenAs(() => { Assert.Fail(); return Result.Ok(1); }));

            AssertSuccess(Result.Go(1).ThenAs(_ => { }));
            AssertFailure(Result<int>.NotFoundError().ThenAs(_ => { Assert.Fail(); }));

            AssertSuccess(Result.Go(1).ThenAs(_ => Result.Success));
            AssertFailure(Result<int>.NotFoundError().ThenAs(_ => Result.Success));

            Assert.AreEqual(2f, AssertSuccess(Result.Go(1).ThenAs(i => i + 1f)));
            AssertFailure(Result.Go<int>(new NotFoundException()).ThenAs(i => { Assert.Fail(); return i + 1f; }));

            Assert.AreEqual(2f, AssertSuccess(Result.Go(1).ThenAs(i => Result.Ok(i + 1f))));
            AssertFailure(Result.Go<int>(new NotFoundException()).ThenAs(i => { Assert.Fail(); return Result.Ok(i + 1f); }));

            AssertSuccess(Result.Go().Then(() => new IToResultTest()));
            AssertFailure(Result.NotFoundError().ThenFrom(() => { Assert.Fail(); return new IToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).ThenFrom(_ => new ITypedToResultTest())));
            AssertFailure(Result<int>.NotFoundError().ThenFrom(_ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).ThenFrom(_ => new IToResultIntTest())));
            AssertFailure(Result<int>.NotFoundError().ThenFrom(_ => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().ThenFromAs<int>(() => new ITypedToResultTest())));
            AssertFailure(Result.NotFoundError().ThenFromAs<int>(() => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().ThenFromAs<int>(() => new IToResultIntTest())));
            AssertFailure(Result.NotFoundError().ThenFromAs<int>(() => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(1f).ThenFromAs<float, int>(_ => new ITypedToResultTest())));
            AssertFailure(Result<float>.NotFoundError().ThenFromAs<float, int>(_ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(1f).ThenFromAs<float, int>(_ => new IToResultIntTest())));
            AssertFailure(Result<float>.NotFoundError().ThenFromAs<float, int>(_ => { Assert.Fail(); return new IToResultIntTest(); }));
        }

        [Test]
        public async Task Async()
        {
            int j = 0;
            AssertSuccess(await Result.Go().ThenAsync(() => { ++j;  return Task.CompletedTask; })); 
            Assert.AreEqual(1, j);
            AssertFailure(await Result.Fail(new BusinessException()).ThenAsync(() => { Assert.Fail(); return Task.CompletedTask; }));

            AssertSuccess(await Result.Go().ThenAsync(() => Task.FromResult(Result.Success)));
            AssertFailure(await Result.Fail(new BusinessException()).ThenAsync(() => Task.FromResult(Result.NotFoundError())));

            j = 0;
            AssertSuccess(await Result.Go(0).ThenAsync(_ => { ++j; return Task.CompletedTask; }));
            Assert.AreEqual(1, j);
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsync(_ => { Assert.Fail(); return Task.CompletedTask; }));

            Assert.AreEqual(2, AssertSuccess(await Result.Go(1).ThenAsync(i => Task.FromResult(++i))));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsync(i => { Assert.Fail(); return Task.FromResult(++i); }));

            Assert.AreEqual(2, AssertSuccess(await Result.Go(1).ThenAsync(i => Task.FromResult(Result.Ok(++i)))));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsync(i => { Assert.Fail(); return Task.FromResult(Result.Ok(++i)); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go().ThenAsAsync(() => Task.FromResult(1))));
            AssertFailure(await Result.NotFoundError().ThenAsAsync(() => { Assert.Fail(); return Task.FromResult(1); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go().ThenAsAsync(() => Task.FromResult(Result.Ok(1)))));
            AssertFailure(await Result.NotFoundError().ThenAsAsync(() => { Assert.Fail(); return Task.FromResult(Result.Ok(1)); }));

            AssertSuccess(await Result.Go(1).ThenAsAsync(_ => Task.CompletedTask));
            AssertFailure(await Result<int>.NotFoundError().ThenAsAsync(_ => { Assert.Fail(); return Task.CompletedTask; }));

            AssertSuccess(await Result.Go(1).ThenAsAsync(_ => Task.FromResult(Result.Success)));
            AssertFailure(await Result<int>.NotFoundError().ThenAsAsync(_ => Task.FromResult(Result.Success)));

            Assert.AreEqual(2f, AssertSuccess(await Result.Go(1).ThenAsAsync(i => Task.FromResult(i + 1f))));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsAsync(i => { Assert.Fail(); return Task.FromResult(i + 1f); }));

            Assert.AreEqual(2f, AssertSuccess(await Result.Go(1).ThenAsAsync(i => Task.FromResult(Result.Ok(i + 1f)))));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsAsync(i => { Assert.Fail(); return Task.FromResult(Result.Ok(i + 1f)); }));

            AssertSuccess(await Result.Go().ThenFromAsync(async () => await Task.FromResult(new IToResultTest())));
            AssertFailure(await Result.NotFoundError().ThenFromAsync(async () => { Assert.Fail(); return await Task.FromResult(new IToResultTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go(0).ThenFromAsync(async _ => await Task.FromResult(new ITypedToResultTest()))));
            AssertFailure(await Result<int>.NotFoundError().ThenFromAsync(async _ => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go(0).ThenFromAsync(async _ => await Task.FromResult(new IToResultIntTest()))));
            AssertFailure(await Result<int>.NotFoundError().ThenFromAsync(async _ => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go().ThenFromAsAsync<int>(async () => await Task.FromResult(new ITypedToResultTest()))));
            AssertFailure(await Result.NotFoundError().ThenFromAsAsync<int>(async () => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go().ThenFromAsAsync<int>(async () => await Task.FromResult(new IToResultIntTest()))));
            AssertFailure(await Result.NotFoundError().ThenFromAsAsync<int>(async () => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go(1f).ThenFromAsAsync<float, int>(async _ => await Task.FromResult(new ITypedToResultTest()))));
            AssertFailure(await Result<float>.NotFoundError().ThenFromAsAsync<float, int>(async _ => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go(1f).ThenFromAsAsync<float, int>(async _ => await Task.FromResult(new IToResultIntTest()))));
            AssertFailure(await Result<float>.NotFoundError().ThenFromAsAsync<float, int>(async _ => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));
        }

        public class IToResultTest : IToResult
        {
            public Result ToResult() => Result.Success;
        }

        public class ITypedToResultTest : ITypedToResult
        {
            public Result<T> ToResult<T>() => Result.Ok((T)(object)1);
        }

        public class IToResultIntTest : IToResult<int>
        {
            public Result<int> ToResult() => Result.Ok(1);
        }
    }
}