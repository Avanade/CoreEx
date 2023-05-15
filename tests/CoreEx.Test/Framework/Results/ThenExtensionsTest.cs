using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ThenExtensionsTest
    {
        [Test]
        public void Sync_Result_Then_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = r.Then(() => i = 1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Sync_Result_Then_With_Action_Failure()
        {
            var i = 0;
            var r = Result.Fail(new BusinessException());
            var r2 = r.Then(() => i = 1);
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Sync_Result_Then_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = r.Then(() => Result.Fail("Test"));
            Assert.True(r2.IsFailure);
        }

        [Test]
        public void Sync_Result_Then_With_Func_Failure()
        {
            var r = Result.Fail(new BusinessException());
            var r2 = r.Then(() => Result.NotFoundError());
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Sync_ResultT_Then_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = r.Then(() => { i = 1; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Sync_ResultT_Then_Action_Failure()
        {
            var i = 0;
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.Then(() => { i = 1; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Sync_ResultT_Then_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.Then(() => 1);
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public void Sync_ResultT_Then_Func_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.Then(() => 1);
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public void Sync_ResultT_Then_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.Then(() => Result.Fail("Test"));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public void Sync_ResultT_Then_Func_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.Then(() => Result.NotFoundError());
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Sync_ResultT_Then_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.Then(() => true);
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public void Sync_ResultT_Then_Func_Diff_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.Then(() => true);
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public void Sync_ResultT_Then_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.Then(() => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public void Sync_ResultT_Then_Func_Result_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.Then(() => Result<int>.Fail(new NotFoundException()));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Sync_ResultT_Then_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.Then(v => new Result<bool>(true));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_Then_FuncT_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.Then(v => new Result<bool>(true));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public void Sync_ResultT_Then_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.Then(v => true);
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_Then_FuncT_ResultT_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = r.Then(v => true);
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        /* AsyncResult */

        [Test]
        public async Task AsyncResult_Result_Then_With_Action_Success()
        {
            var i = 0;
            var r = Result.BeginAsync(() => Task.CompletedTask);
            var r2 = await r.Then(() => i = 1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncResult_Result_Then_With_Action_Failure()
        {
            var i = 0;
            var r = Result.BeginAsync(() => Task.FromResult(Result.Fail(new BusinessException())));
            var r2 = await r.Then(() => i = 1);
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncResult_Result_Then_With_Func_Success()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result.Success));
            var r2 = await r.Then(() => Result.Fail("Test"));
            Assert.True(r2.IsFailure);
        }

        [Test]
        public async Task AsyncResult_Result_Then_With_Func_Failure()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result.Fail(new BusinessException())));
            var r2 = await r.Then(() => Result.NotFoundError());
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Action_Success()
        {
            var i = 0;
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Ok()));
            var r2 = await r.Then(() => { i = 1; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Action_Failure()
        {
            var i = 0;
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
            var r2 = await r.Then(() => { i = 1; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Func_Value_Success()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Ok()));
            var r2 = await r.Then(() => 1);
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Func_Value_Failure()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
            var r2 = await r.Then(() => 1);
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Func_Result_Success()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Ok()));
            var r2 = await r.Then(() => Result.Fail("Test"));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Func_Result_Failure()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
            var r2 = await r.Then(() => Result.NotFoundError());
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Func_Diff_Value_Success()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Ok()));
            var r2 = await r.Then(() => true);
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Func_Diff_Value_Failure()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
            var r2 = await r.Then(() => true);
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Func_Result_Value_Success()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Ok()));
            var r2 = await r.Then(() => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_Func_Result_Value_Failure()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
            var r2 = await r.Then(() => Result<int>.Fail(new NotFoundException()));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_FuncT_Result_Success()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Ok()));
            var r2 = await r.Then(v => new Result<bool>(true));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_FuncT_Result_Failure()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
            var r2 = await r.Then(v => new Result<bool>(true));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_FuncT_ResultT_Success()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Ok()));
            var r2 = await r.Then(v => true);
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_Then_FuncT_ResultT_Failure()
        {
            var r = Result.BeginAsync(() => Task.FromResult(Result<int>.Fail(new BusinessException())));
            var r2 = await r.Then(v => true);
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        /* AsyncFunc */

        [Test]
        public async Task AsyncFunc_Result_Then_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncFunc_Result_Then_With_Action_Failure()
        {
            var i = 0;
            var r = Result.Fail(new BusinessException());
            var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncFunc_Result_Then_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = await r.ThenAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsFailure);
        }

        [Test]
        public async Task AsyncFunc_Result_Then_With_Func_Failure()
        {
            var r = Result.Fail(new BusinessException());
            var r2 = await r.ThenAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Action_Failure()
        {
            var i = 0;
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.ThenAsync(() => Task.FromResult(1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncFuncResultT_Then_Func_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.ThenAsync(() => Task.FromResult(1));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.ThenAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Func_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.ThenAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.ThenAsync(() => Task.FromResult(true));
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Func_Diff_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.ThenAsync(() => Task.FromResult(true));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.ThenAsync(() => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_Func_Result_Value_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.ThenAsync(() => Task.FromResult(Result<int>.Fail(new NotFoundException())));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.ThenAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_FuncT_Result_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.ThenAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.ThenAsync(v => Task.FromResult(true));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Then_FuncT_ResultT_Failure()
        {
            var r = Result<int>.Fail(new BusinessException());
            var r2 = await r.ThenAsync(v => Task.FromResult(true));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        /* AsyncBoth */

        [Test]
        public async Task AsyncBoth_Result_Then_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncBoth_Result_Then_With_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncBoth_Result_Then_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.ThenAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsFailure);
        }

        [Test]
        public async Task AsyncBoth_Result_Then_With_Func_Failure()
        {
            var r = Task.FromResult(Result.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.True(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Action_Failure()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(() => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.ThenAsync(() => Task.FromResult(1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncBothResultT_Then_Func_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(() => Task.FromResult(1));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.ThenAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Func_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(() => Task.FromResult(Result.NotFoundError()));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.ThenAsync(() => Task.FromResult(true));
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Func_Diff_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(() => Task.FromResult(true));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.ThenAsync(() => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_Func_Result_Value_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(() => Task.FromResult(Result<int>.Fail(new NotFoundException())));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.ThenAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_FuncT_Result_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.ThenAsync(v => Task.FromResult(true));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Then_FuncT_ResultT_Failure()
        {
            var r = Task.FromResult(Result<int>.Fail(new BusinessException()));
            var r2 = await r.ThenAsync(v => Task.FromResult(true));
            Assert.IsTrue(r2.IsFailure);
            Assert.That(r2.Error, Is.Not.Null.And.InstanceOf<BusinessException>());
        }
    }
}