using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class AnyExtensionsTest
    {
        [Test]
        public void Sync_Result_Any_Action()
        {
            var i = 0;
            Result.Success.Any(() => { i++; });
            Result.Fail("sad").Any(() => { i++; });
            Assert.AreEqual(2, i);
        }

        [Test]
        public void Sync_Result_Any_Func()
        {
            var i = 0;
            var r = Result.Success.Any(() => { i++; return Result.Fail("sad"); });
            Assert.IsTrue(r.IsFailure);
            r = r.Any(() => { i++; return Result.Ok(); });
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(2, i);
        }

        [Test]
        public void Sync_ResultT_Any_Action()
        {
            var i = 0;
            Result<int>.Ok().Any(_ => { i++; });
            Result<int>.Fail("sad").Any(_ => { i++; });
            Assert.AreEqual(2, i);
        }

        [Test]
        public void Sync_ResultT_Any_Func()
        {
            var i = 0;
            var r = Result<int>.Ok().Any(() => { i++; return Result<int>.Fail("sad"); });
            Assert.IsTrue(r.IsFailure);
            r = r.Any(() => { i++; return Result<int>.Ok(); });
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(2, i);
        }

        [Test]
        public void Sync_ResultT_Any_Func_With_Result()
        {
            var i = 0;
            var r = Result<int>.Ok().Any(_ => { i++; return Result<int>.Fail("sad"); });
            Assert.IsTrue(r.IsFailure);
            r = r.Any(_ => { i++; return Result<int>.Ok(); });
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(2, i);
        }

        /* AsyncResult */

        [Test]
        public async Task AsyncResult_Result_Any_Action()
        {
            var i = 0;
            var r = Task.FromResult(Result.Ok());
            await r.Any(() => { i++; });
            Result.Fail("sad").Any(() => { i++; });
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncResult_Result_Any_Func()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.Any(() => { i++; return Result.Fail("sad"); });
            Assert.IsTrue(r2.IsFailure);
            r = Task.FromResult(r2);
            r2 = await r.Any(() => { i++; return Result.Ok(); });
            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_Any_Action()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            await r.Any(_ => { i++; });
            Result<int>.Fail("sad").Any(_ => { i++; });
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_Any_Func()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.Any(() => { i++; return Result<int>.Fail("sad"); });
            Assert.IsTrue(r2.IsFailure);
            r = Task.FromResult(r2);
            r2 = await r.Any(() => { i++; return Result<int>.Ok(); });
            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncResult_ResultT_Any_Func_With_Result()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.Any(_ => { i++; return Result<int>.Fail("sad"); });
            Assert.IsTrue(r2.IsFailure);
            r = Task.FromResult(r2);
            r2 = await r.Any(_ => { i++; return Result<int>.Ok(); });
            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, i);
        }

        /* AsyncFunc */

        [Test]
        public async Task AsyncFunc_Result_Any_Action()
        {
            var i = 0;
            await Result.Success.AnyAsync(() => { i++; return Task.CompletedTask; });
            await Result.Fail("sad").AnyAsync(() => { i++; return Task.CompletedTask; });
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncFunc_Result_Any_Func()
        {
            var i = 0;
            var r = await Result.Success.AnyAsync(_ => { i++; return Task.FromResult(Result.Fail("sad")); });
            Assert.IsTrue(r.IsFailure);
            r = await r.AnyAsync(_ => { i++; return Task.FromResult(Result.Ok()); });
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Any_Action()
        {
            var i = 0;
            await Result<int>.Ok().AnyAsync(_ => { i++; return Task.CompletedTask; });
            await Result<int>.Fail("sad").AnyAsync(_ => { i++; return Task.CompletedTask; });
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Any_Func()
        {
            var i = 0;
            var r = await Result<int>.Ok().AnyAsync(() => { i++; return Task.FromResult(Result<int>.Fail("sad")); });
            Assert.IsTrue(r.IsFailure);
            r = await r.AnyAsync(() => { i++; return Task.FromResult(Result<int>.Ok()); });
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncFunc_ResultT_Any_Func_With_Result()
        {
            var i = 0;
            var r = await Result<int>.Ok().AnyAsync(_ => { i++; return Task.FromResult(Result<int>.Fail("sad")); });
            Assert.IsTrue(r.IsFailure);
            r = await r.AnyAsync(_ => { i++; return Task.FromResult(Result<int>.Ok()); });
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(2, i);
        }

        /* AsyncBoth */

        [Test]
        public async Task AsyncBoth_Result_Any_Action()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            await r.AnyAsync(() => { i++; return Task.CompletedTask; });
            r = Task.FromResult(Result.Fail("sad"));
            await r.AnyAsync(() => { i++; return Task.CompletedTask; });
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncBoth_Result_Any_Func()
        {
            var i = 0;
            var r = Task.FromResult(Result.Success);
            var r2 = await r.AnyAsync(_ => { i++; return Task.FromResult(Result.Fail("sad")); });
            Assert.IsTrue(r2.IsFailure);
            r = Task.FromResult(r2);
            r2 = await r.AnyAsync(_ => { i++; return Task.FromResult(Result.Ok()); });
            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Any_Action()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            await r.AnyAsync(_ => { i++; return Task.CompletedTask; });
            r = Task.FromResult(Result<int>.Fail("sad"));
            await r.AnyAsync(_ => { i++; return Task.CompletedTask; });
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Any_Func()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.AnyAsync(() => { i++; return Task.FromResult(Result<int>.Fail("sad")); });
            Assert.IsTrue(r2.IsFailure);
            r = Task.FromResult(r2);
            r2 = await r.AnyAsync(() => { i++; return Task.FromResult(Result<int>.Ok()); });
            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, i);
        }

        [Test]
        public async Task AsyncBoth_ResultT_Any_Func_With_Result()
        {
            var i = 0;
            var r = Task.FromResult(Result<int>.Ok());
            var r2 = await r.AnyAsync(_ => { i++; return Task.FromResult(Result<int>.Fail("sad")); });
            Assert.IsTrue(r2.IsFailure);
            r = Task.FromResult(r2);
            r2 = await r.AnyAsync(_ => { i++; return Task.FromResult(Result<int>.Ok()); });
            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, i);
        }
    }
}