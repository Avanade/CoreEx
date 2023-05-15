using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class OnFailureExtensionsTest
    {
        [Test]
        public void Sync_Result_OnFailure_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = r.OnFailure(() => i = 1);
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Sync_Result_OnFailure_With_Action_Failure()
        {
            var i = 0;
            var r = Result.Fail(new BusinessException());
            var r2 = r.OnFailure(() => i = 1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Sync_Result_OnFailure_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = r.OnFailure(() => Result.Fail("Test"));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public void Sync_Result_OnFailure_With_Func_Failure()
        {
            var r = Result.Fail(new BusinessException());
            var r2 = r.OnFailure(() => Result.NotFoundError());
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public void Sync_ResultT_OnFailure_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = r.OnFailure(() => { i = 1; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Sync_ResultT_OnFailure_Action_Failure()
        {
            var i = 0;
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnFailure(() => { i = 1; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Sync_ResultT_OnFailure_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnFailure(() => 1);
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_OnFailure_Func_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnFailure(() => 1);
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnFailure_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnFailure(() => Result.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_OnFailure_Func_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnFailure(() => Result.NotFoundError());
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public void Sync_ResultT_OnFailure_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnFailure(() => true);
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnFailure_Func_Diff_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnFailure(() => true);
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnFailure_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnFailure(() => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_OnFailure_Func_Result_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnFailure(() => Result<int>.Fail(new NotFoundException()));
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public void Sync_ResultT_OnFailure_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnFailure(v => new Result<bool>(true));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_OnFailure_FuncT_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnFailure(v => new Result<bool>(true));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnFailure_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.OnFailure(v => true);
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public void Sync_ResultT_OnFailure_FuncT_ResultT_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.OnFailure(v => true);
            Assert.IsTrue(r2.Value);
        }

        /* AsyncResult */

        [Test]
        public async Task AsyncResult_Result_OnFailure_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.OnFailure(() => i = 1);
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncResult_Result_OnFailure_With_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.OnFailure(() => i = 1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncResult_Result_OnFailure_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.OnFailure(() => Result.Fail("Test"));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_Result_OnFailure_With_Func_Failure()
        {
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.OnFailure(() => Result.NotFoundError());
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailure(() => { i = 1; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailure(() => { i = 1; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailure(() => 1);
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Func_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailure(() => 1);
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailure(() => Result.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Func_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailure(() => Result.NotFoundError());
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailure(() => true);
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Func_Diff_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailure(() => true);
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailure(() => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_Func_Result_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailure(() => Result<int>.Fail(new NotFoundException()));
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailure(v => new Result<bool>(true));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_FuncT_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailure(v => new Result<bool>(true));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailure(v => true);
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_OnFailure_FuncT_ResultT_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailure(v => true);
            Assert.IsTrue(r2.Value);
        }

        /* AsyncFunc */

        [Test]
        public async Task AsyncFunc_Result_OnFailure_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = await r.OnFailureAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncFunc_Result_OnFailure_With_Action_Failure()
        {
            var i = 0;
            var r = Result.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncFunc_Result_OnFailure_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_Result_OnFailure_With_Func_Failure()
        {
            var r = Result.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = await r.OnFailureAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Action_Failure()
        {
            var i = 0;
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnFailureAsync(() => Task.FromResult(1));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Func_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Func_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnFailureAsync(() => Task.FromResult(true));
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Func_Diff_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(true));
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_Func_Result_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result<int>.Fail(new NotFoundException())));
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnFailureAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_FuncT_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.OnFailureAsync(v => Task.FromResult(true));
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_OnFailure_FuncT_ResultT_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.OnFailureAsync(v => Task.FromResult(true));
            Assert.IsTrue(r2.Value);
        }

        /* AsyncBoth */

        [Test]
        public async Task AsyncBoth_Result_OnFailure_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.OnFailureAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncBoth_Result_OnFailure_With_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncBoth_Result_OnFailure_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_Result_OnFailure_With_Func_Failure()
        {
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailureAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(1));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Func_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(() => Task.FromResult(1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Func_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(true));
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Func_Diff_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(() => Task.FromResult(true));
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_Func_Result_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(() => Task.FromResult(Result<int>.Fail(new NotFoundException())));
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<NotFoundException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailureAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_FuncT_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.OnFailureAsync(v => Task.FromResult(true));
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_OnFailure_FuncT_ResultT_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.OnFailureAsync(v => Task.FromResult(true));
            Assert.IsTrue(r2.Value);
        }
    }
}