using CoreEx.Results;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ResultBeginTest
    {
        [Test]
        public void Begin_No_Args()
        {
            var r = Result.Begin();
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public void Begin_With_Action()
        {
            var r = Result.Begin(() => { });
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public void Begin_With_Result_Func()
        {
            var r = Result.Begin(() => Result.Fail("Test"));
            Assert.IsTrue(r.IsFailure);
        }

        [Test]
        public void Begin_Value_With_Action()
        {
            var r = Result.Begin<int>(() => { });
            Assert.AreEqual(Result<int>.None, r);
        }

        [Test]
        public void Begin_Value_Func()
        {
            var r = Result.Begin(() => 1);
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public void Begin_Value_Result_Func()
        {
            var r = Result.Begin(() => Result.Ok(1));
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public void Begin_Value_No_Args()
        {
            var r = Result.Begin<int>();
            Assert.AreEqual(Result<int>.None, r);
        }

        /* Begin_Async */

        [Test]
        public async Task BeginAsync_With_Action()
        {
            var r = await Result.BeginAsync(() => Task.CompletedTask);
            Assert.AreEqual(Result.Success, r);
        }

        [Test]
        public async Task BeginAsync_With_Result_Func()
        {
            var r = await Result.BeginAsync(() => Task.FromResult(Result.Fail("Test")));
            Assert.IsTrue(r.IsFailure);
        }

        [Test]
        public async Task BeginAsync_Value_With_Action()
        {
            var r = await Result.BeginAsync<int>(() => Task.CompletedTask);
            Assert.AreEqual(Result<int>.None, r);
        }

        [Test]
        public async Task BeginAsync_Value_Func()
        {
            var r = await Result.BeginAsync<int>(() => Task.FromResult(1));
            Assert.AreEqual(1, r.Value);
        }

        [Test]
        public async Task BeginAsync_Value_Result_Func()
        {
            var r = await Result.BeginAsync(() => Task.FromResult(Result.Ok(1)));
            Assert.AreEqual(1, r.Value);
        }
    }
}