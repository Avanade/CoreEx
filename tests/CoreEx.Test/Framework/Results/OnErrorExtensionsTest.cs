using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class OnErrorExtensionsTest
    {
        [Test]
        public void Sync_Result_OnError_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = r.OnError(() => i = 1);
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Sync_Result_OnError_With_Action_Failure()
        {
            var i = 0;
            var r = Result.Fail(new BusinessException());
            var r2 = r.OnError(() => i = 1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Sync_Result_OnError_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = r.OnError(() => Result.Fail("Test"));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public void Sync_Result_OnError_With_Func_Failure()
        {
            var r = Result.Fail(new BusinessException());
            var r2 = r.OnError(() => Result.NotFoundError());
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public void Sync_ResultT_OnError_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = r.OnError(() => { i = 1; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Sync_ResultT_OnError_Action_Failure()
        {
            var i = 0;
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnError(() => { i = 1; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Sync_ResultT_OnError_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnError(() => 1);
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_OnError_Func_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnError(() => 1);
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnError_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnError(() => Result.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_OnError_Func_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnError(() => Result.NotFoundError());
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public void Sync_ResultT_OnError_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnError(() => true);
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnError_Func_Diff_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnError(() => true);
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnError_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnError(() => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_OnError_Func_Result_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnError(() => Result<int>.Fail(new NotFoundException()));
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public void Sync_ResultT_OnError_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnError(v => new Result<bool>(true));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_OnError_FuncT_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnError(v => new Result<bool>(true));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnError_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnError(v => true);
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnError_FuncT_ResultT_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnError(v => true);
            Assert.IsTrue(r2.Value);
        }

        /* AsyncResult */

        [Test]
        public async Task AsyncResult_Result_OnError_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.OnError(() => i = 1);
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncResult_Result_OnError_With_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.OnError(() => i = 1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncResult_Result_OnError_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.OnError(() => Result.Fail("Test"));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_Result_OnError_With_Func_Failure()
        {
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.OnError(() => Result.NotFoundError());
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnError(() => { i = 1; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnError(() => { i = 1; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnError(() => 1);
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Func_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnError(() => 1);
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnError(() => Result.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Func_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnError(() => Result.NotFoundError());
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnError(() => true);
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Func_Diff_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnError(() => true);
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnError(() => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_Func_Result_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnError(() => Result<int>.Fail(new NotFoundException()));
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnError(v => new Result<bool>(true));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_FuncT_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnError(v => new Result<bool>(true));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnError(v => true);
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnError_FuncT_ResultT_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnError(v => true);
            Assert.IsTrue(r2.Value);
        }

        /* AsyncFunc */

        [Test]
        public async Task AsyncFunc_Result_OnError_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = await r.OnErrorAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncFunc_Result_OnError_With_Action_Failure()
        {
            var i = 0;
            var r = Result.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncFunc_Result_OnError_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_Result_OnError_With_Func_Failure()
        {
            var r = Result.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = await r.OnErrorAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Action_Failure()
        {
            var i = 0;
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnErrorAsync(() => Task.FromResult(1));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Func_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Func_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnErrorAsync(() => Task.FromResult(true));
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Func_Diff_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(true));
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_Func_Result_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result<int>.Fail(new NotFoundException())));
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnErrorAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_FuncT_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnErrorAsync(v => Task.FromResult(true));
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnError_FuncT_ResultT_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnErrorAsync(v => Task.FromResult(true));
            Assert.IsTrue(r2.Value);
        }

        /* AsyncBoth */

        [Test]
        public async Task AsyncBoth_Result_OnError_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.OnErrorAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncBoth_Result_OnError_With_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncBoth_Result_OnError_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_Result_OnError_With_Func_Failure()
        {
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnErrorAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(1));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Func_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(() => Task.FromResult(1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Func_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(true));
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Func_Diff_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(() => Task.FromResult(true));
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_Func_Result_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(() => Task.FromResult(Result<int>.Fail(new NotFoundException())));
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnErrorAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_FuncT_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnErrorAsync(v => Task.FromResult(true));
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnError_FuncT_ResultT_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnErrorAsync(v => Task.FromResult(true));
            Assert.IsTrue(r2.Value);
        }
    }
}