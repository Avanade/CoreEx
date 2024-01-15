using CoreEx.Results;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ThenExtensionsTest
    {
        internal static void AssertSuccess(Result result) => Assert.That(result.IsSuccess, Is.True);
        internal static void AssertFailure(Result result) => Assert.That(result.IsFailure, Is.True);

        internal static T AssertSuccess<T>(Result<T> result)
        {
            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
        }

        internal static void AssertFailure<T>(Result<T> result) => Assert.That(result.IsFailure, Is.True);


        [Test]
        public void Sync()
        {
            int j = 0;
            AssertSuccess(Result.Go().Then(() => { j++; }));
            Assert.That(j, Is.EqualTo(1));
            AssertFailure(Result.Fail(new BusinessException()).Then(() => { Assert.Fail(); }));

            AssertSuccess(Result.Go().Then(() => Result.Success));
            AssertFailure(Result.Fail(new BusinessException()).Then(() => Result.NotFoundError()));

            j = 0;
            AssertSuccess(Result.Go(0).Then(_ => { j++; }));
            Assert.That(j, Is.EqualTo(1));
            AssertFailure(Result.Go<int>(new NotFoundException()).Then(_ => { Assert.Fail(); }));

            Assert.That(AssertSuccess(Result.Go(0).Then(i => ++i)), Is.EqualTo(1));
            AssertFailure(Result.Go<int>(new NotFoundException()).Then(i => { Assert.Fail(); return +i; }));

            Assert.That(AssertSuccess(Result.Go(0).Then(i => Result.Ok(++i))), Is.EqualTo(1));
            AssertFailure(Result.Go<int>(new NotFoundException()).Then(i => { Assert.Fail(); return Result.Ok(++i); }));

            Assert.That(AssertSuccess(Result.Go().ThenAs(() => 1)), Is.EqualTo(1));
            AssertFailure(Result.NotFoundError().ThenAs(() => { Assert.Fail(); return 1; }));

            Assert.That(AssertSuccess(Result.Go().ThenAs(() => Result.Ok(1))), Is.EqualTo(1));
            AssertFailure(Result.NotFoundError().ThenAs(() => { Assert.Fail(); return Result.Ok(1); }));

            AssertSuccess(Result.Go(1).ThenAs(_ => { }));
            AssertFailure(Result<int>.NotFoundError().ThenAs(_ => { Assert.Fail(); }));

            AssertSuccess(Result.Go(1).ThenAs(_ => Result.Success));
            AssertFailure(Result<int>.NotFoundError().ThenAs(_ => Result.Success));

            Assert.That(AssertSuccess(Result.Go(1).ThenAs(i => i + 1f)), Is.EqualTo(2f));
            AssertFailure(Result.Go<int>(new NotFoundException()).ThenAs(i => { Assert.Fail(); return i + 1f; }));

            Assert.That(AssertSuccess(Result.Go(1).ThenAs(i => Result.Ok(i + 1f))), Is.EqualTo(2f));
            AssertFailure(Result.Go<int>(new NotFoundException()).ThenAs(i => { Assert.Fail(); return Result.Ok(i + 1f); }));

            AssertSuccess(Result.Go().Then(() => new IToResultTest()));
            AssertFailure(Result.NotFoundError().ThenFrom(() => { Assert.Fail(); return new IToResultTest(); }));

            Assert.That(AssertSuccess(Result.Go(0).ThenFrom(_ => new ITypedToResultTest())), Is.EqualTo(1));
            AssertFailure(Result<int>.NotFoundError().ThenFrom(_ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.That(AssertSuccess(Result.Go(0).ThenFrom(_ => new IToResultIntTest())), Is.EqualTo(1));
            AssertFailure(Result<int>.NotFoundError().ThenFrom(_ => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.That(AssertSuccess(Result.Go().ThenFromAs<int>(() => new ITypedToResultTest())), Is.EqualTo(1));
            AssertFailure(Result.NotFoundError().ThenFromAs<int>(() => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.That(AssertSuccess(Result.Go().ThenFromAs<int>(() => new IToResultIntTest())), Is.EqualTo(1));
            AssertFailure(Result.NotFoundError().ThenFromAs<int>(() => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.That(AssertSuccess(Result.Go(1f).ThenFromAs<float, int>(_ => new ITypedToResultTest())), Is.EqualTo(1));
            AssertFailure(Result<float>.NotFoundError().ThenFromAs<float, int>(_ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.That(AssertSuccess(Result.Go(1f).ThenFromAs<float, int>(_ => new IToResultIntTest())), Is.EqualTo(1));
            AssertFailure(Result<float>.NotFoundError().ThenFromAs<float, int>(_ => { Assert.Fail(); return new IToResultIntTest(); }));
        }

        [Test]
        public async Task Async()
        {
            int j = 0;
            AssertSuccess(await Result.Go().ThenAsync(() => { ++j;  return Task.CompletedTask; })); 
            Assert.That(j, Is.EqualTo(1));
            AssertFailure(await Result.Fail(new BusinessException()).ThenAsync(() => { Assert.Fail(); return Task.CompletedTask; }));

            AssertSuccess(await Result.Go().ThenAsync(() => Task.FromResult(Result.Success)));
            AssertFailure(await Result.Fail(new BusinessException()).ThenAsync(() => Task.FromResult(Result.NotFoundError())));

            j = 0;
            AssertSuccess(await Result.Go(0).ThenAsync(_ => { ++j; return Task.CompletedTask; }));
            Assert.That(j, Is.EqualTo(1));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsync(_ => { Assert.Fail(); return Task.CompletedTask; }));

            Assert.That(AssertSuccess(await Result.Go(1).ThenAsync(i => Task.FromResult(++i))), Is.EqualTo(2));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsync(i => { Assert.Fail(); return Task.FromResult(++i); }));

            Assert.That(AssertSuccess(await Result.Go(1).ThenAsync(i => Task.FromResult(Result.Ok(++i)))), Is.EqualTo(2));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsync(i => { Assert.Fail(); return Task.FromResult(Result.Ok(++i)); }));

            Assert.That(AssertSuccess(await Result.Go().ThenAsAsync(() => Task.FromResult(1))), Is.EqualTo(1));
            AssertFailure(await Result.NotFoundError().ThenAsAsync(() => { Assert.Fail(); return Task.FromResult(1); }));

            Assert.That(AssertSuccess(await Result.Go().ThenAsAsync(() => Task.FromResult(Result.Ok(1)))), Is.EqualTo(1));
            AssertFailure(await Result.NotFoundError().ThenAsAsync(() => { Assert.Fail(); return Task.FromResult(Result.Ok(1)); }));

            AssertSuccess(await Result.Go(1).ThenAsAsync(_ => Task.CompletedTask));
            AssertFailure(await Result<int>.NotFoundError().ThenAsAsync(_ => { Assert.Fail(); return Task.CompletedTask; }));

            AssertSuccess(await Result.Go(1).ThenAsAsync(_ => Task.FromResult(Result.Success)));
            AssertFailure(await Result<int>.NotFoundError().ThenAsAsync(_ => Task.FromResult(Result.Success)));

            Assert.That(AssertSuccess(await Result.Go(1).ThenAsAsync(i => Task.FromResult(i + 1f))), Is.EqualTo(2f));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsAsync(i => { Assert.Fail(); return Task.FromResult(i + 1f); }));

            Assert.That(AssertSuccess(await Result.Go(1).ThenAsAsync(i => Task.FromResult(Result.Ok(i + 1f)))), Is.EqualTo(2f));
            AssertFailure(await Result.Go<int>(new NotFoundException()).ThenAsAsync(i => { Assert.Fail(); return Task.FromResult(Result.Ok(i + 1f)); }));

            AssertSuccess(await Result.Go().ThenFromAsync(async () => await Task.FromResult(new IToResultTest())));
            AssertFailure(await Result.NotFoundError().ThenFromAsync(async () => { Assert.Fail(); return await Task.FromResult(new IToResultTest()); }));

            Assert.That(AssertSuccess(await Result.Go(0).ThenFromAsync(async _ => await Task.FromResult(new ITypedToResultTest()))), Is.EqualTo(1));
            AssertFailure(await Result<int>.NotFoundError().ThenFromAsync(async _ => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.That(AssertSuccess(await Result.Go(0).ThenFromAsync(async _ => await Task.FromResult(new IToResultIntTest()))), Is.EqualTo(1));
            AssertFailure(await Result<int>.NotFoundError().ThenFromAsync(async _ => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));

            Assert.That(AssertSuccess(await Result.Go().ThenFromAsAsync<int>(async () => await Task.FromResult(new ITypedToResultTest()))), Is.EqualTo(1));
            AssertFailure(await Result.NotFoundError().ThenFromAsAsync<int>(async () => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.That(AssertSuccess(await Result.Go().ThenFromAsAsync<int>(async () => await Task.FromResult(new IToResultIntTest()))), Is.EqualTo(1));
            AssertFailure(await Result.NotFoundError().ThenFromAsAsync<int>(async () => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));

            Assert.That(AssertSuccess(await Result.Go(1f).ThenFromAsAsync<float, int>(async _ => await Task.FromResult(new ITypedToResultTest()))), Is.EqualTo(1));
            AssertFailure(await Result<float>.NotFoundError().ThenFromAsAsync<float, int>(async _ => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.That(AssertSuccess(await Result.Go(1f).ThenFromAsAsync<float, int>(async _ => await Task.FromResult(new IToResultIntTest()))), Is.EqualTo(1));
            AssertFailure(await Result<float>.NotFoundError().ThenFromAsAsync<float, int>(async _ => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));

            Func<int, Task<Result>>? func = null;
            var t = Result.Go().ThenAsync(() => func?.Invoke(1) ?? Result.SuccessTask).Then(() => { });
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