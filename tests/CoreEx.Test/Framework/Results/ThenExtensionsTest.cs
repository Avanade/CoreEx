using CoreEx.Results;
using NUnit.Framework;
using System;
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
            AssertFailure(Result.NotFoundError().Then(() => { Assert.Fail(); return new IToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).Then(_ => new ITypedToResultTest())));
            AssertFailure(Result<int>.NotFoundError().Then(_ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(0).Then(_ => new IToResultIntTest())));
            AssertFailure(Result<int>.NotFoundError().Then(_ => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().ThenAs<int>(() => new ITypedToResultTest())));
            AssertFailure(Result.NotFoundError().ThenAs<int>(() => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go().ThenAs<int>(() => new IToResultIntTest())));
            AssertFailure(Result.NotFoundError().ThenAs<int>(() => { Assert.Fail(); return new IToResultIntTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(1f).ThenAs<float, int>(_ => new ITypedToResultTest())));
            AssertFailure(Result<float>.NotFoundError().ThenAs<float, int>(_ => { Assert.Fail(); return new ITypedToResultTest(); }));

            Assert.AreEqual(1, AssertSuccess(Result.Go(1f).ThenAs<float, int>(_ => new IToResultIntTest())));
            AssertFailure(Result<float>.NotFoundError().ThenAs<float, int>(_ => { Assert.Fail(); return new IToResultIntTest(); }));
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

            AssertSuccess(await Result.Go().ThenAsync(async () => await Task.FromResult(new IToResultTest())));
            AssertFailure(await Result.NotFoundError().ThenAsync(async () => { Assert.Fail(); return await Task.FromResult(new IToResultTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go(0).ThenAsync(async _ => await Task.FromResult(new ITypedToResultTest()))));
            AssertFailure(await Result<int>.NotFoundError().ThenAsync(async _ => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go(0).ThenAsync(async _ => await Task.FromResult(new IToResultIntTest()))));
            AssertFailure(await Result<int>.NotFoundError().ThenAsync(async _ => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go().ThenAsAsync<int>(async () => await Task.FromResult(new ITypedToResultTest()))));
            AssertFailure(await Result.NotFoundError().ThenAsAsync<int>(async () => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go().ThenAsAsync<int>(async () => await Task.FromResult(new IToResultIntTest()))));
            AssertFailure(await Result.NotFoundError().ThenAsAsync<int>(async () => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go(1f).ThenAsAsync<float, int>(async _ => await Task.FromResult(new ITypedToResultTest()))));
            AssertFailure(await Result<float>.NotFoundError().ThenAsAsync<float, int>(async _ => { Assert.Fail(); return await Task.FromResult(new ITypedToResultTest()); }));

            Assert.AreEqual(1, AssertSuccess(await Result.Go(1f).ThenAsAsync<float, int>(async _ => await Task.FromResult(new IToResultIntTest()))));
            AssertFailure(await Result<float>.NotFoundError().ThenAsAsync<float, int>(async _ => { Assert.Fail(); return await Task.FromResult(new IToResultIntTest()); }));
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

        //[Test]
        //public void Sync_Result_Then_With_Action_Success()
        //{
        //    var i = 0;
        //    var r = Result.Success;
        //    var r2 = r.Then(() => i = 1);
        //    Assert.AreEqual(1, i);
        //}

        //[Test]
        //public void Sync_Result_Then_With_Action_Failure()
        //{
        //    var i = 0;
        //    var r = Result.Fail(new BusinessException());
        //    var r2 = r.Then(() => i = 1);
        //    Assert.AreEqual(0, i);
        //}

        //[Test]
        //public void Sync_Result_Then_With_Func_Success()
        //{
        //    var r = Result.Success;
        //    var r2 = r.Then(() => Result.Fail("Test"));
        //    Assert.True(r2.IsFailure);
        //}

        //[Test]
        //public void Sync_Result_Then_With_Func_Failure()
        //{
        //    var r = Result.Fail(new BusinessException());
        //    var r2 = r.Then(() => Result.NotFoundError());
        //    Assert.True(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public void Sync_ResultT_Then_Action_Success()
        //{
        //    var i = 0;
        //    var r = Result<int>.Ok();
        //    var r2 = r.Then(() => { i = 1; });
        //    Assert.AreEqual(1, i);
        //}

        //[Test]
        //public void Sync_ResultT_Then_Action_Failure()
        //{
        //    var i = 0;
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = r.Then(() => { i = 1; });
        //    Assert.AreEqual(0, i);
        //}

        //[Test]
        //public void Sync_ResultT_Then_Func_Value_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = r.Then(() => 1);
        //    Assert.AreEqual(1, r2.Value);
        //}

        //[Test]
        //public void Sync_ResultT_Then_Func_Value_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = r.Then(() => 1);
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public void Sync_ResultT_Then_Func_Result_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = r.Then(() => Result.Fail("Test"));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public void Sync_ResultT_Then_Func_Result_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = r.Then(() => Result.NotFoundError());
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public void Sync_ResultT_Then_Func_Diff_Value_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = r.Then(() => true);
        //    Assert.AreEqual(true, r2.Value);
        //}

        //[Test]
        //public void Sync_ResultT_Then_Func_Diff_Value_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = r.Then(() => true);
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public void Sync_ResultT_Then_Func_Result_Value_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = r.Then(() => Result<int>.Fail("Test"));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public void Sync_ResultT_Then_Func_Result_Value_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = r.Then(() => Result<int>.Fail(new NotFoundException()));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public void Sync_ResultT_Then_FuncT_Result_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = r.Then(v => new Result<bool>(true));
        //    Assert.IsTrue(r2.IsSuccess);
        //}

        //[Test]
        //public void Sync_ResultT_Then_FuncT_Result_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = r.Then(v => new Result<bool>(true));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public void Sync_ResultT_Then_FuncT_ResultT_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = r.Then(v => true);
        //    Assert.IsTrue(r2.IsSuccess);
        //}

        //[Test]
        //public void Sync_ResultT_Then_FuncT_ResultT_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = r.Then(v => true);
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        ///* AsyncResult */

        //[Test]
        //public async Task AsyncResult_Result_Then_With_Action_Success()
        //{
        //    var i = 0;
        //    var r = Result.GoAsync(() => Task.CompletedTask);
        //    var r2 = await r.Then(() => i = 1);
        //    Assert.AreEqual(1, i);
        //}

        //[Test]
        //public async Task AsyncResult_Result_Then_With_Action_Failure()
        //{
        //    var i = 0;
        //    var r = Result.GoAsync(() => Task.FromResult(Result.Fail(new BusinessException())));
        //    var r2 = await r.Then(() => i = 1);
        //    Assert.AreEqual(0, i);
        //}

        //[Test]
        //public async Task AsyncResult_Result_Then_With_Func_Success()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result.Success));
        //    var r2 = await r.Then(() => Result.Fail("Test"));
        //    Assert.True(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncResult_Result_Then_With_Func_Failure()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result.Fail(new BusinessException())));
        //    var r2 = await r.Then(() => Result.NotFoundError());
        //    Assert.True(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Action_Success()
        //{
        //    var i = 0;
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Ok()));
        //    var r2 = await r.Then(() => { i = 1; });
        //    Assert.AreEqual(1, i);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Action_Failure()
        //{
        //    var i = 0;
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
        //    var r2 = await r.Then(() => { i = 1; });
        //    Assert.AreEqual(0, i);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Func_Value_Success()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Ok()));
        //    var r2 = await r.Then(() => 1);
        //    Assert.AreEqual(1, r2.Value);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Func_Value_Failure()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
        //    var r2 = await r.Then(() => 1);
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Func_Result_Success()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Ok()));
        //    var r2 = await r.Then(() => Result.Fail("Test"));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Func_Result_Failure()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
        //    var r2 = await r.Then(() => Result.NotFoundError());
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Func_Diff_Value_Success()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Ok()));
        //    var r2 = await r.Then(() => true);
        //    Assert.AreEqual(true, r2.Value);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Func_Diff_Value_Failure()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
        //    var r2 = await r.Then(() => true);
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Func_Result_Value_Success()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Ok()));
        //    var r2 = await r.Then(() => Result<int>.Fail("Test"));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_Func_Result_Value_Failure()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
        //    var r2 = await r.Then(() => Result<int>.Fail(new NotFoundException()));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_FuncT_Result_Success()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Ok()));
        //    var r2 = await r.Then(v => new Result<bool>(true));
        //    Assert.IsTrue(r2.IsSuccess);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_FuncT_Result_Failure()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
        //    var r2 = await r.Then(v => new Result<bool>(true));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_FuncT_ResultT_Success()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Ok()));
        //    var r2 = await r.Then(v => true);
        //    Assert.IsTrue(r2.IsSuccess);
        //}

        //[Test]
        //public async Task AsyncResult_ResultT_Then_FuncT_ResultT_Failure()
        //{
        //    var r = Result.GoAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
        //    var r2 = await r.Then(v => true);
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        ///* AsyncFunc */

        //[Test]
        //public async Task AsyncFunc_Result_Then_With_Action_Success()
        //{
        //    var i = 0;
        //    var r = Result.Success;
        //    var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
        //    Assert.AreEqual(1, i);
        //}

        //[Test]
        //public async Task AsyncFunc_Result_Then_With_Action_Failure()
        //{
        //    var i = 0;
        //    var r = Result.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
        //    Assert.AreEqual(0, i);
        //}

        //[Test]
        //public async Task AsyncFunc_Result_Then_With_Func_Success()
        //{
        //    var r = Result.Success;
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result.Fail("Test")));
        //    Assert.True(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncFunc_Result_Then_With_Func_Failure()
        //{
        //    var r = Result.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result.NotFoundError()));
        //    Assert.True(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Action_Success()
        //{
        //    var i = 0;
        //    var r = Result<int>.Ok();
        //    var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
        //    Assert.AreEqual(1, i);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Action_Failure()
        //{
        //    var i = 0;
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
        //    Assert.AreEqual(0, i);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Func_Value_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = await r.ThenAsync(() => Task.FromResult(1));
        //    Assert.AreEqual(1, r2.Value);
        //}

        //[Test]
        //public async Task AsyncFuncResultT_Then_Func_Value_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(1));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Func_Result_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result.Fail("Test")));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Func_Result_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result.NotFoundError()));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Func_Diff_Value_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = await r.ThenAsync(() => Task.FromResult(true));
        //    Assert.AreEqual(true, r2.Value);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Func_Diff_Value_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(true));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Func_Result_Value_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result<int>.Fail("Test")));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_Func_Result_Value_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result<int>.Fail(new NotFoundException())));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_FuncT_Result_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = await r.ThenAsync(v => Task.FromResult(new Result<bool>(true)));
        //    Assert.IsTrue(r2.IsSuccess);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_FuncT_Result_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(v => Task.FromResult(new Result<bool>(true)));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_FuncT_ResultT_Success()
        //{
        //    var r = Result<int>.Ok();
        //    var r2 = await r.ThenAsync(v => Task.FromResult(true));
        //    Assert.IsTrue(r2.IsSuccess);
        //}

        //[Test]
        //public async Task AsyncFunc_ResultT_Then_FuncT_ResultT_Failure()
        //{
        //    var r = Result<int>.Fail(new BusinessException());
        //    var r2 = await r.ThenAsync(v => Task.FromResult(true));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        ///* AsyncBoth */

        //[Test]
        //public async Task AsyncBoth_Result_Then_With_Action_Success()
        //{
        //    var i = 0;
        //    var r = Task.FromResult(Result.Success);
        //    var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
        //    Assert.AreEqual(1, i);
        //}

        //[Test]
        //public async Task AsyncBoth_Result_Then_With_Action_Failure()
        //{
        //    var i = 0;
        //    var r = Task.FromResult(Result.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
        //    Assert.AreEqual(0, i);
        //}

        //[Test]
        //public async Task AsyncBoth_Result_Then_With_Func_Success()
        //{
        //    var r = Task.FromResult(Result.Success);
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result.Fail("Test")));
        //    Assert.True(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncBoth_Result_Then_With_Func_Failure()
        //{
        //    var r = Task.FromResult(Result.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result.NotFoundError()));
        //    Assert.True(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Action_Success()
        //{
        //    var i = 0;
        //    var r = Task.FromResult(Result<int>.Ok());
        //    var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
        //    Assert.AreEqual(1, i);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Action_Failure()
        //{
        //    var i = 0;
        //    var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
        //    Assert.AreEqual(0, i);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Func_Value_Success()
        //{
        //    var r = Task.FromResult(Result<int>.Ok());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(1));
        //    Assert.AreEqual(1, r2.Value);
        //}

        //[Test]
        //public async Task AsyncBothResultT_Then_Func_Value_Failure()
        //{
        //    var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(() => Task.FromResult(1));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Func_Result_Success()
        //{
        //    var r = Task.FromResult(Result<int>.Ok());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result.Fail("Test")));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Func_Result_Failure()
        //{
        //    var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result.NotFoundError()));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Func_Diff_Value_Success()
        //{
        //    var r = Task.FromResult(Result<int>.Ok());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(true));
        //    Assert.AreEqual(true, r2.Value);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Func_Diff_Value_Failure()
        //{
        //    var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(() => Task.FromResult(true));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Func_Result_Value_Success()
        //{
        //    var r = Task.FromResult(Result<int>.Ok());
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result<int>.Fail("Test")));
        //    Assert.IsTrue(r2.IsFailure);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_Func_Result_Value_Failure()
        //{
        //    var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(() => Task.FromResult(Result<int>.Fail(new NotFoundException())));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_FuncT_Result_Success()
        //{
        //    var r = Task.FromResult(Result<int>.Ok());
        //    var r2 = await r.ThenAsync(v => Task.FromResult(new Result<bool>(true)));
        //    Assert.IsTrue(r2.IsSuccess);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_FuncT_Result_Failure()
        //{
        //    var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(v => Task.FromResult(new Result<bool>(true)));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_FuncT_ResultT_Success()
        //{
        //    var r = Task.FromResult(Result<int>.Ok());
        //    var r2 = await r.ThenAsync(v => Task.FromResult(true));
        //    Assert.IsTrue(r2.IsSuccess);
        //}

        //[Test]
        //public async Task AsyncBoth_ResultT_Then_FuncT_ResultT_Failure()
        //{
        //    var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
        //    var r2 = await r.ThenAsync(v => Task.FromResult(true));
        //    Assert.IsTrue(r2.IsFailure);
        //    Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        //}
    }
}