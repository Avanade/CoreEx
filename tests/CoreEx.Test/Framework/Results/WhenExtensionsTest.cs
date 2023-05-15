using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class WhenExtensionsTest
    {
        [Test]
        public void Sync_Result_When_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = r.When(() => true, () => i = 1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Sync_Result_WhenNot_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = r.When(() => false, () => i = 1);
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Sync_Result_When_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = r.When(() => true, () => Result.Fail("Test"));
            Assert.True(r2.IsFailure);
        }

        [Test]
        public void Sync_Result_WhenNot_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = r.When(() => false, () => Result.Fail("Test"));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_When_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = r.When(_ => true, () => { i = 1; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Sync_ResultT_WhenNot_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = r.When(_ => false, () => { i = 1; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Sync_ResultT_When_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => true, () => 1);
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public void Sync_ResultT_WhenNot_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => false, () => 1);
            Assert.AreEqual(0, r2.Value);
        }

        [Test]
        public void Sync_ResultT_When_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => true, () => Result.Fail("Test"));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public void Sync_ResultT_WhenNot_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => false, () => Result.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_When_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => true, () => true);
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public void Sync_ResultT_WhenNot_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => false, () => true);
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public void Sync_ResultT_When_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => true, () => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public void Sync_ResultT_WhenNot_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => false, () => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public void Sync_ResultT_When_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => true, v => new Result<bool>(true));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public void Sync_ResultT_WhenNot_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => false, v => new Result<bool>(true));
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public void Sync_ResultT_When_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => true, v => true);
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public void Sync_ResultT_WhenNot_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = r.When(_ => false, v => true);
            Assert.IsFalse(r2.Value);
        }

        /* AsyncResult */

        [Test]
        public async Task AsyncResult_Result_When_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.When(() => true, () => i = 1);
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncResult_Result_WhenNot_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.When(() => false, () => i = 1);
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncResult_Result_When_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.When(() => true, () => Result.Fail("Test"));
            Assert.True(r2.IsFailure);
        }

        [Test]
        public async Task AsyncResult_Result_WhenNot_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.When(() => false, () => Result.Fail("Test"));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_When_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => true, () => { i = 1; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_WhenNot_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => false, () => { i = 1; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_When_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => true, () => 1);
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_WhenNot_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => false, () => 1);
            Assert.AreEqual(0, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_When_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => true, () => Result.Fail("Test"));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncResult_ResultT_WhenNot_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => false, () => Result.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_When_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => true, () => true);
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_WhenNot_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => false, () => true);
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_When_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => true, () => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncResult_ResultT_WhenNot_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => false, () => Result<int>.Fail("Test"));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncResult_ResultT_When_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => true, v => new Result<bool>(true));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_WhenNot_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => false, v => new Result<bool>(true));
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_When_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => true, v => true);
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncResult_ResultT_WhenNot_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.When(_ => false, v => true);
            Assert.IsFalse(r2.Value);
        }

        /* AsyncFunc */

        [Test]
        public async Task AsyncFunc_Result_When_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = await r.WhenAsync(() => true, () => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncFunc_Result_WhenNot_With_Action_Success()
        {
            var i = 0;
            var r = Result.Success;
            var r2 = await r.WhenAsync(() => false, () => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncFunc_Result_When_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = await r.WhenAsync(() => true, () => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsFailure);
        }

        [Test]
        public async Task AsyncFunc_Result_WhenNot_With_Func_Success()
        {
            var r = Result.Success;
            var r2 = await r.WhenAsync(() => false, () => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_When_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => true, () => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_WhenNot_Action_Success()
        {
            var i = 0;
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => false, () => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_When_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => true, () => Task.FromResult(1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_WhenNot_Func_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => false, () => Task.FromResult(1));
            Assert.AreEqual(0, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_When_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => true, () => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncFunc_ResultT_WhenNot_Func_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => false, () => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_When_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => true, () => Task.FromResult(true));
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_WhenNot_Func_Diff_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => false, () => Task.FromResult(true));
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_When_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => true, () => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncFunc_ResultT_WhenNot_Func_Result_Value_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => false, () => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncFunc_ResultT_When_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => true, v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_WhenNot_FuncT_Result_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => false, v => Task.FromResult(new Result<bool>(true)));
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_When_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => true, v => Task.FromResult(true));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncFunc_ResultT_WhenNot_FuncT_ResultT_Success()
        {
            var r = Result<int>.Ok();
            var r2 = await r.WhenAsync(_ => false, v => Task.FromResult(true));
            Assert.IsFalse(r2.Value);
        }

        /* AsyncBoth */

        [Test]
        public async Task AsyncBoth_Result_When_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.WhenAsync(() => true, () => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncBoth_Result_WhenNot_With_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.WhenAsync(() => false, () => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncBoth_Result_When_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.WhenAsync(() => true, () => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsFailure);
        }

        [Test]
        public async Task AsyncBoth_Result_WhenNot_With_Func_Success()
        {
            var r = Task.FromResult(Result.Success);
            var r2 = await r.WhenAsync(() => false, () => Task.FromResult(Result.Fail("Test")));
            Assert.True(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_When_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => true, () => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(1, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_WhenNot_Action_Success()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => false, () => { i = 1; return Task.CompletedTask; });
            Assert.AreEqual(0, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_When_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => true, () => Task.FromResult(1));
            Assert.AreEqual(1, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_WhenNot_Func_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => false, () => Task.FromResult(1));
            Assert.AreEqual(0, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_When_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => true, () => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncBoth_ResultT_WhenNot_Func_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => false, () => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_When_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => true, () => Task.FromResult(true));
            Assert.AreEqual(true, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_WhenNot_Func_Diff_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => false, () => Task.FromResult(true));
            Assert.AreEqual(false, r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_When_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => true, () => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsFailure);
        }

        [Test]
        public async Task AsyncBoth_ResultT_WhenNot_Func_Result_Value_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => false, () => Task.FromResult(Result<int>.Fail("Test")));
            Assert.IsTrue(r2.IsSuccess);
        }

        [Test]
        public async Task AsyncBoth_ResultT_When_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => true, v => Task.FromResult(new Result<bool>(true)));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_WhenNot_FuncT_Result_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => false, v => Task.FromResult(new Result<bool>(true)));
            Assert.IsFalse(r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_When_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => true, v => Task.FromResult(true));
            Assert.IsTrue(r2.Value);
        }

        [Test]
        public async Task AsyncBoth_ResultT_WhenNot_FuncT_ResultT_Success()
        {
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.WhenAsync(_ => false, v => Task.FromResult(true));
            Assert.IsFalse(r2.Value);
        }
    }
}